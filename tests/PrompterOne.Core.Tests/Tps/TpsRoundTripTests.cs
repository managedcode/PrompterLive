using PrompterOne.Core.Services;
using PrompterOne.Core.Services.Rsvp;

namespace PrompterOne.Core.Tests;

public sealed class TpsRoundTripTests
{
    [Fact]
    public async Task ParseAndExportAsync_RetainsMetadataAndSegmentShape()
    {
        var parser = new TpsParser();
        var exporter = new TpsExporter();
        const string source = """
        ---
        title: "File Export Test"
        base_wpm: 150
        ---

        ## [Test Segment|150WPM|warm]

        ### [Body|150WPM]

        Content to export.
        """;

        var document = await parser.ParseAsync(source);
        var exported = await exporter.ExportAsync(document);
        var reparsed = await parser.ParseAsync(exported);

        Assert.Equal("File Export Test", reparsed.Metadata["title"]);
        Assert.Equal("150", reparsed.Metadata["base_wpm"]);
        Assert.Single(reparsed.Segments);
        Assert.Equal("Test Segment", reparsed.Segments[0].Name);
        Assert.Contains(reparsed.Segments[0].Blocks, block => block.Name == "Body");
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
    public async Task CompileAsync_PreservesInlineWpmScopesAcrossNestedPronunciationTags()
    {
        var parser = new TpsParser();
        var compiler = new ScriptCompiler();
        const string source = """
        ---
        title: "Inline speed"
        base_wpm: 140
        ---

        ## [Call to Action|140WPM|motivational]

        ### [Closing Block|140WPM|energetic]

        [180WPM]Join us in building the future of [pronunciation:TELE-promp-ter]teleprompter[/pronunciation] technology.[/180WPM]
        """;

        var document = await parser.ParseAsync(source);
        var compiled = await compiler.CompileAsync(document);
        var compiledWord = compiled.Segments
            .SelectMany(segment => segment.Blocks)
            .SelectMany(block => block.Words)
            .Single(word => string.Equals(word.CleanText, "teleprompter", StringComparison.Ordinal));

        Assert.Equal(180, compiledWord.Metadata.SpeedOverride);
        Assert.Equal("TELE-promp-ter", compiledWord.Metadata.PronunciationGuide);
    }

    [Fact]
    public async Task CompileAsync_AppliesNestedFrontMatterSpeedOffsetsAndNormalReset()
    {
        var parser = new TpsParser();
        var compiler = new ScriptCompiler();
        const string source = """
        ---
        title: "Custom speed offsets"
        base_wpm: 140
        speed_offsets:
          xslow: -30
          slow: -10
          fast: 10
          xfast: 35
        ---

        ## [Offsets|140WPM|neutral]

        ### [Reader Block|140WPM]

        [xslow]alpha[/xslow] [slow]bravo [normal]charm[/normal] delta[/slow] [fast]eagle[/fast] [xfast]fable[/xfast]
        """;

        var document = await parser.ParseAsync(source);
        var compiled = await compiler.CompileAsync(document);
        var words = compiled.Segments
            .SelectMany(segment => segment.Blocks)
            .SelectMany(block => block.Words)
            .Where(word => !string.IsNullOrWhiteSpace(word.CleanText))
            .ToDictionary(word => word.CleanText, StringComparer.Ordinal);

        Assert.Equal("-30", document.Metadata["speed_offsets.xslow"]);
        Assert.Equal("-10", document.Metadata["speed_offsets.slow"]);
        Assert.Equal("10", document.Metadata["speed_offsets.fast"]);
        Assert.Equal("35", document.Metadata["speed_offsets.xfast"]);

        Assert.Equal(0.7f, words["alpha"].Metadata.SpeedMultiplier);
        Assert.Equal(0.9f, words["bravo"].Metadata.SpeedMultiplier);
        Assert.Null(words["charm"].Metadata.SpeedMultiplier);
        Assert.Equal(0.9f, words["delta"].Metadata.SpeedMultiplier);
        Assert.Equal(1.1f, words["eagle"].Metadata.SpeedMultiplier);
        Assert.Equal(1.35f, words["fable"].Metadata.SpeedMultiplier);

        Assert.True(words["alpha"].DisplayDuration > words["bravo"].DisplayDuration);
        Assert.True(words["bravo"].DisplayDuration > words["charm"].DisplayDuration);
        Assert.Equal(words["bravo"].DisplayDuration, words["delta"].DisplayDuration);
        Assert.True(words["charm"].DisplayDuration > words["eagle"].DisplayDuration);
        Assert.True(words["eagle"].DisplayDuration > words["fable"].DisplayDuration);
    }

    [Fact]
    public async Task CompileAsync_TracksInlineEmotionScopesSeparatelyFromInheritedSectionTone()
    {
        var parser = new TpsParser();
        var compiler = new ScriptCompiler();
        const string source = """
        ---
        title: "Inline emotion"
        base_wpm: 140
        ---

        ## [Signal|140WPM|focused]

        ### [Reader Block|140WPM]

        Neutral [warm]welcome[/warm] [urgent]act[/urgent]
        """;

        var document = await parser.ParseAsync(source);
        var compiled = await compiler.CompileAsync(document);
        var words = compiled.Segments
            .SelectMany(segment => segment.Blocks)
            .SelectMany(block => block.Words)
            .Where(word => !string.IsNullOrWhiteSpace(word.CleanText))
            .ToDictionary(word => word.CleanText, StringComparer.Ordinal);

        Assert.Equal("focused", words["Neutral"].Metadata.EmotionHint);
        Assert.Null(words["Neutral"].Metadata.InlineEmotionHint);

        Assert.Equal("warm", words["welcome"].Metadata.EmotionHint);
        Assert.Equal("warm", words["welcome"].Metadata.InlineEmotionHint);

        Assert.Equal("urgent", words["act"].Metadata.EmotionHint);
        Assert.Equal("urgent", words["act"].Metadata.InlineEmotionHint);
    }

    [Fact]
    public async Task CompileAsync_AttachesStandalonePunctuationTokensToAdjacentWords()
    {
        var parser = new TpsParser();
        var compiler = new ScriptCompiler();
        const string source = """
        ---
        title: "Attached punctuation"
        base_wpm: 140
        ---

        ## [Signal|140WPM|focused]

        ### [Reader Block|140WPM]

        [emphasis]No payment data was exposed[/emphasis], / containment - restored.
        """;

        var document = await parser.ParseAsync(source);
        var compiled = await compiler.CompileAsync(document);
        var words = compiled.Segments
            .SelectMany(segment => segment.Blocks)
            .SelectMany(block => block.Words)
            .Where(word => word.Metadata?.IsPause != true && !string.IsNullOrWhiteSpace(word.CleanText))
            .Select(word => word.CleanText)
            .ToArray();

        Assert.Contains("exposed,", words);
        Assert.Contains("containment -", words);
        Assert.DoesNotContain(",", words);
        Assert.DoesNotContain("-", words);
    }
}
