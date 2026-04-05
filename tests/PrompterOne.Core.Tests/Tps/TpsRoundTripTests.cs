using ManagedCode.Tps;
using PrompterOne.Core.Models.CompiledScript;
using PrompterOne.Core.Services;
using PrompterOne.Core.Services.Rsvp;

namespace PrompterOne.Core.Tests;

public sealed class TpsRoundTripTests
{
    private const int MaximumSupportedWpm = 220;
    private const int MinimumSupportedWpm = 80;

    [Fact]
    public async Task ParseAndExportAsync_RetainsCanonicalMetadataAndFlexibleHeaders()
    {
        var documentReader = CreateDocumentReader();
        var exporter = new TpsExporter();
        const string source = """
        ---
        title: "File Export Test"
        duration: "10:00"
        base_wpm: 150
        speed_offsets:
          xslow: -30
          slow: -10
          fast: 10
          xfast: 35
        ---

        ## [Test Segment|Warm|Speaker:Alex|150WPM|0:30-1:10]

        Intro line.

        ### [Body|Speaker:Jordan|professional|160WPM]

        Content to export.
        """;

        var document = await documentReader.ReadAsync(source);
        var exported = await exporter.ExportAsync(document);
        var reparsed = await documentReader.ReadAsync(exported);

        Assert.Equal("File Export Test", reparsed.Metadata["title"]);
        Assert.Equal("10:00", reparsed.Metadata["duration"]);
        Assert.Equal("-30", reparsed.Metadata["speed_offsets.xslow"]);
        Assert.Equal("35", reparsed.Metadata["speed_offsets.xfast"]);

        var segment = Assert.Single(reparsed.Segments);
        Assert.Equal("Test Segment", segment.Name);
        Assert.Equal(150, segment.TargetWPM);
        Assert.Equal("warm", segment.Emotion);
        Assert.Equal("Alex", segment.Speaker);
        Assert.Equal("0:30-1:10", segment.Timing);

        var block = Assert.Single(segment.Blocks);
        Assert.Equal("Body", block.Name);
        Assert.Equal(160, block.TargetWPM);
        Assert.Equal("professional", block.Emotion);
        Assert.Equal("Jordan", block.Speaker);
    }

    [Fact]
    public void OrpCalculator_SplitsLongWordAtExpectedFocusPoint()
    {
        var calculator = new RsvpOrpCalculator();

        var split = calculator.SplitWordAtORP("teleprompter");

        Assert.Equal("tele", split.PreORP);
        Assert.Equal("p", split.OrpChar);
        Assert.Equal("rompter", split.PostORP);
    }

    [Fact]
    public async Task CompileAsync_PreservesPronunciationAndInlineSpeedResolution()
    {
        var compiled = await CompileAsync(
            """
            ---
            title: "Inline speed"
            base_wpm: 140
            ---

            ## [Call to Action|motivational|Speaker:Alex]

            ### [Closing Block|energetic]

            [180WPM]Join us in building the [pronunciation:TELE-promp-ter]teleprompter[/pronunciation] future.[/180WPM] [slow]carefully[/slow]
            """);

        var words = FlattenWords(compiled);
        var teleprompter = words.Single(word => string.Equals(word.CleanText, "teleprompter", StringComparison.Ordinal));
        var carefully = words.Single(word => string.Equals(word.CleanText, "carefully", StringComparison.Ordinal));

        Assert.Equal(180, teleprompter.Metadata.SpeedOverride);
        Assert.Equal("TELE-promp-ter", teleprompter.Metadata.PronunciationGuide);
        Assert.Equal("Alex", teleprompter.Metadata.Speaker);
        Assert.Equal(0.8f, carefully.Metadata.SpeedMultiplier);
    }

    [Fact]
    public async Task CompileAsync_PreservesRelativeSpeedWhenClosingTagIsFollowedByPunctuation()
    {
        var compiled = await CompileAsync(
            """
            ---
            title: "Speed punctuation"
            base_wpm: 140
            speed_offsets:
              fast: 10
            ---

            ## [Signal|neutral]

            ### [Reader Block]

            [fast]flight[/fast].
            """);

        var flight = FlattenWords(compiled).Single(word => string.Equals(word.CleanText, "flight.", StringComparison.Ordinal));

        Assert.Equal(1.1f, flight.Metadata.SpeedMultiplier);
    }

    [Fact]
    public async Task ParseAsync_UsesLastHeaderParameterAndFallsBackFromInvalidValues()
    {
        var documentReader = CreateDocumentReader();
        const string source = """
        ---
        title: "Header precedence"
        base_wpm: 140
        ---

        ## [Signal|warm|150WPM|Speaker:Jordan|mystery|brokenWPM|Speaker:Alex]

        ### [Body|happy|Speaker:Sam|160WPM|focused|Speaker:Casey]

        Copy.
        """;

        var document = await documentReader.ReadAsync(source);
        var segment = Assert.Single(document.Segments);
        var block = Assert.Single(segment.Blocks);

        Assert.Equal(150, segment.TargetWPM);
        Assert.Equal("warm", segment.Emotion);
        Assert.Equal("Alex", segment.Speaker);

        Assert.Equal(160, block.TargetWPM);
        Assert.Equal("focused", block.Emotion);
        Assert.Equal("Casey", block.Speaker);
    }

    [Fact]
    public async Task ParseAsync_UsesArchetypeRecommendedWpmWhenHeaderOmitsExplicitSpeed()
    {
        var documentReader = CreateDocumentReader();
        var compiler = new ScriptCompiler();
        const string source = """
        ---
        title: "Archetype defaults"
        base_wpm: 140
        ---

        ## [Coach Intro|Archetype:Coach|focused|Speaker:Alex]

        ### [Warmup Prompt|Archetype:Educator]

        Welcome everyone.
        """;

        var document = await documentReader.ReadAsync(source);
        var compiled = await compiler.CompileAsync(document);
        var segment = Assert.Single(document.Segments);
        var block = Assert.Single(segment.Blocks);
        var compiledSegment = Assert.Single(compiled.Segments);
        var compiledBlock = Assert.Single(compiledSegment.Blocks, candidate => string.Equals(candidate.Archetype, "educator", StringComparison.Ordinal));

        Assert.Equal("coach", segment.Archetype);
        Assert.Equal(TpsSpec.ArchetypeRecommendedWpm["coach"], segment.TargetWPM);
        Assert.Equal("educator", block.Archetype);
        Assert.Null(block.TargetWPM);
        Assert.Equal(TpsSpec.ArchetypeRecommendedWpm["educator"], compiledBlock.TargetWPM);
    }

    [Fact]
    public async Task CompileAsync_TracksVolumeDeliveryStressBreathAndHighlight()
    {
        var compiled = await CompileAsync(
            """
            ---
            title: "Reader cues"
            base_wpm: 140
            ---

            ## [Signal|focused|Speaker:Alex]

            ### [Body|professional]

            [loud][building][highlight]moment[/highlight][/building][/loud] announce[stress]me[/stress]nt [stress:de-VE-lop-ment]development[/stress] [breath]
            """);

        var words = FlattenWords(compiled);
        var moment = words.Single(word => string.Equals(word.CleanText, "moment", StringComparison.Ordinal));
        var announcement = words.Single(word => string.Equals(word.CleanText, "announcement", StringComparison.Ordinal));
        var development = words.Single(word => string.Equals(word.CleanText, "development", StringComparison.Ordinal));
        var breath = words.Single(word => word.Metadata.IsBreath);

        Assert.Equal("loud", moment.Metadata.VolumeLevel);
        Assert.Equal("building", moment.Metadata.DeliveryMode);
        Assert.True(moment.Metadata.IsHighlight);
        Assert.Equal("Alex", moment.Metadata.Speaker);

        Assert.Equal("me", announcement.Metadata.StressText);
        Assert.Equal("de-VE-lop-ment", development.Metadata.StressGuide);

        Assert.True(breath.Metadata.IsBreath);
        Assert.Equal(TimeSpan.Zero, breath.DisplayDuration);
    }

    [Fact]
    public async Task CompileAsync_TracksArchetypeArticulationEnergyAndMelodyMetadata()
    {
        var compiled = await CompileAsync(
            """
            ---
            title: "Archetype styling"
            base_wpm: 140
            ---

            ## [Coach Intro|Archetype:Coach|focused|Speaker:Alex]

            ### [Warmup Prompt|Archetype:Educator]

            [legato][energy:8]steady[/energy][/legato] [staccato][melody:4]rhythm[/melody][/staccato]
            """);

        var segment = Assert.Single(compiled.Segments);
        var block = Assert.Single(segment.Blocks, candidate => string.Equals(candidate.Archetype, "educator", StringComparison.Ordinal));
        var words = FlattenWords(compiled);
        var steady = words.Single(word => string.Equals(word.CleanText, "steady", StringComparison.Ordinal));
        var rhythm = words.Single(word => string.Equals(word.CleanText, "rhythm", StringComparison.Ordinal));

        Assert.Equal("coach", segment.Archetype);
        Assert.Equal("educator", block.Archetype);
        Assert.Equal("legato", steady.Metadata.ArticulationStyle);
        Assert.Equal(8, steady.Metadata.EnergyLevel);
        Assert.Equal("staccato", rhythm.Metadata.ArticulationStyle);
        Assert.Equal(4, rhythm.Metadata.MelodyLevel);
    }

    [Fact]
    public void ParseTps_PreservesExplicitBlockWordsWhenSdkEmitsImplicitLeadBlock()
    {
        var scriptDataFactory = CreateScriptDataFactory();
        const string source = """
        ---
        title: "Implicit lead block"
        base_wpm: 140
        ---

        ## [Coach Intro|Archetype:Coach|focused|Speaker:Alex]

        Opening setup for the segment.

        ### [Warmup Prompt|Archetype:Educator]

        [legato][energy:8]steady[/energy][/legato] [staccato][melody:4]rhythm[/melody][/staccato]
        """;

        var script = scriptDataFactory.Build(source);
        var segment = Assert.Single(script.Segments ?? []);
        var block = Assert.Single(segment.Blocks ?? [], candidate => string.Equals(candidate.Archetype, "educator", StringComparison.Ordinal));
        var blockWords = block.Phrases!
            .SelectMany(phrase => phrase.Words ?? [])
            .Select(word => word.Text)
            .ToArray();

        Assert.Contains("Opening setup for the segment.", segment.Content, StringComparison.Ordinal);
        Assert.Contains("steady", blockWords);
        Assert.Contains("rhythm", blockWords);
        Assert.DoesNotContain("Opening", blockWords);
    }

    [Fact]
    public async Task CompileAsync_LeavesUnknownTagsLiteralAndTreatsLegacyColorTagsAsText()
    {
        var compiled = await CompileAsync(
            """
            ---
            title: "Literal unknown tags"
            base_wpm: 140
            ---

            ## [Signal|neutral]

            ### [Body]

            Neutral [green]welcome[/green] [custom]tag[/custom]
            """);

        var words = FlattenWords(compiled)
            .Where(word => !word.Metadata.IsPause && !word.Metadata.IsBreath)
            .Select(word => word.CleanText)
            .ToArray();

        Assert.Contains("[green]welcome[/green]", words);
        Assert.Contains("[custom]tag[/custom]", words);
    }

    [Fact]
    public async Task ParseAsync_IgnoresLegacyOffsetAliasesAndUsesCanonicalDefaults()
    {
        var documentReader = CreateDocumentReader();
        const string source = """
        ---
        title: "Legacy offsets"
        base_wpm: 140
        xslow_offset: -99
        ---

        ## [Signal|neutral]

        ### [Body]

        [xslow]alpha[/xslow]
        """;

        var document = await documentReader.ReadAsync(source);
        var compiled = await new ScriptCompiler().CompileAsync(document);
        var alpha = FlattenWords(compiled).Single(word => string.Equals(word.CleanText, "alpha", StringComparison.Ordinal));

        Assert.DoesNotContain("xslow_offset", document.Metadata.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(0.6f, alpha.Metadata.SpeedMultiplier);
    }

    [Fact]
    public async Task ParseAsync_ClampsBaseWpmToCanonicalRuntimeBounds()
    {
        var documentReader = CreateDocumentReader();
        var scriptDataFactory = CreateScriptDataFactory();
        const string source = """
        ---
        title: "Clamped base WPM"
        base_wpm: 500
        ---

        ## [Signal|focused]

        ### [Body]

        Ready now.
        """;

        var document = await documentReader.ReadAsync(source);
        var data = scriptDataFactory.Build(source);
        var segment = Assert.Single(document.Segments);

        Assert.Equal(MaximumSupportedWpm, segment.TargetWPM);
        Assert.Equal(MaximumSupportedWpm, data.TargetWpm);
    }

    [Fact]
    public async Task ParseAsync_IgnoresOutOfRangeHeaderWpmAndFallsBackToClampedBaseWpm()
    {
        var documentReader = CreateDocumentReader();
        const string source = """
        ---
        title: "Header fallback"
        base_wpm: 60
        ---

        ## [Signal|warm|300WPM]

        ### [Body|400WPM]

        Copy.
        """;

        var document = await documentReader.ReadAsync(source);
        var segment = Assert.Single(document.Segments);
        var block = Assert.Single(segment.Blocks);

        Assert.Equal(MinimumSupportedWpm, segment.TargetWPM);
        Assert.Null(block.TargetWPM);
    }

    [Fact]
    public async Task CompileAsync_AttachesStandalonePunctuationTokensToAdjacentWords()
    {
        var compiled = await CompileAsync(
            """
            ---
            title: "Attached punctuation"
            base_wpm: 140
            ---

            ## [Signal|focused]

            ### [Reader Block]

            [emphasis]No payment data was exposed[/emphasis], / containment - restored.
            """);

        var words = FlattenWords(compiled)
            .Where(word => word.Metadata.IsPause is false && !string.IsNullOrWhiteSpace(word.CleanText))
            .Select(word => word.CleanText)
            .ToArray();

        Assert.Contains("exposed,", words);
        Assert.Contains("containment -", words);
        Assert.DoesNotContain(",", words);
        Assert.DoesNotContain("-", words);
    }

    private static async Task<CompiledScript> CompileAsync(string source)
    {
        var documentReader = CreateDocumentReader();
        var compiler = new ScriptCompiler();
        var document = await documentReader.ReadAsync(source);
        return await compiler.CompileAsync(document);
    }

    private static TpsDocumentReader CreateDocumentReader() => new();

    private static TpsScriptDataFactory CreateScriptDataFactory() => new();

    private static IReadOnlyList<CompiledWord> FlattenWords(CompiledScript compiled)
    {
        return compiled.Segments
            .SelectMany(segment => segment.Blocks.Count > 0
                ? segment.Blocks.SelectMany(block => block.Words)
                : segment.Words)
            .ToArray();
    }
}
