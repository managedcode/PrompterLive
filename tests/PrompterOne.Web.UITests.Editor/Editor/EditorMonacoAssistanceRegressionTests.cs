using ManagedCode.Tps;

namespace PrompterOne.Web.UITests;

[System.Obsolete]

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorMonacoAssistanceRegressionTests(StandaloneAppFixture fixture)
{
    private const int TitleLineNumber = 1;
    private const int SegmentLineNumber = 2;
    private const int InlineLineNumber = 4;
    private const int CompletionStartColumn = 2;
    private const int CompletionSlashColumn = 3;
    private const int HoverInsideTokenOffset = 2;
    private const string TitleLine = "# System Design and Software Architecture for Vibe Coders";
    private const string SegmentLine = "## [Episode 2 - How Systems Talk to Each Other|140WPM|Professional|Speaker:Alex|0:00-0:30]";
    private const string InlineGuideLine = "[edit_point:high] [pause:1500ms] [pronunciation:KAM-uhl]teleprompter[/pronunciation]";
    private const string SimpleSegmentLine = "## Segment Title";
    private const string SimpleBlockLine = "### Block Title";
    private const string TokenizationLine = @"Escaped slash \/ pipe \| star \* slash \\ should stay literal [180WPM] [normal]steady[/normal] *calm* **loud**";
    private const string MetadataDocument = """
        # System Design and Software Architecture for Vibe Coders
        ## [Episode 2 - How Systems Talk to Each Other|140WPM|Professional|Speaker:Alex|0:00-0:30]
        ### [Connection Patterns|145WPM|Warm]
        [edit_point:high] [pause:1500ms] [pronunciation:KAM-uhl]teleprompter[/pronunciation]
        """;
    private const string SimpleHeaderDocument = """
        ## Segment Title
        ### Block Title
        Escaped slash \/ pipe \| star \* slash \\ should stay literal [180WPM] [normal]steady[/normal] *calm* **loud**
        """;
    private const string PronunciationCompletionLabel = "[pronunciation:guide]text[/pronunciation]";
    private const string SegmentCompletionLabel = "## [Segment Name|Speaker:Host|140WPM|neutral|0:00-0:30]";
    private const string TimedPauseCompletionLabel = "[pause:2s]";
    private const string MarkdownBoldCompletionLabel = "**text**";
    private const string MarkdownItalicCompletionLabel = "*text*";
    private const string MillisecondPauseCompletionLabel = "[pause:1000ms]";
    private const string LowEditPointCompletionLabel = "[edit_point:low]";
    private const string MediumEditPointCompletionLabel = "[edit_point:medium]";
    private const string XslowCompletionLabel = "[xslow]text[/xslow]";
    private const string LoudCompletionLabel = "[loud]text[/loud]";
    private const string SarcasmCompletionLabel = "[sarcasm]text[/sarcasm]";
    private const string NeutralCompletionLabel = "[neutral]text[/neutral]";
    private const string EnergyCompletionLabel = "[energy:8]text[/energy]";
    private const string MelodyCompletionLabel = "[melody:4]text[/melody]";
    private const string LegatoCompletionLabel = "[legato]text[/legato]";
    private const string StaccatoCompletionLabel = "[staccato]text[/staccato]";
    private const string ArchetypeSegmentCompletionLabel = "## [Segment Name|Speaker:Host|Archetype:Coach|neutral|0:00-0:30]";
    private const string ArchetypeLine = "## [Story Arc|Archetype:Storyteller|Warm|0:10-0:45]";
    private const string ArchetypeDocument = """
        # System Design and Software Architecture for Vibe Coders
        ## [Story Arc|Archetype:Storyteller|Warm|0:10-0:45]
        ### [Punchline|Archetype:Entertainer|Excited]
        [breath]
        """;
    private static readonly string[] ExpectedArchetypeSegmentCompletionLabels = TpsSpec.Archetypes
        .Select(archetype => BuildArchetypeSegmentCompletionLabel(ToDisplayLabel(archetype)))
        .ToArray();
    private static readonly string[] ExpectedVendoredWrapperLabels =
        TpsSpec.RelativeSpeedTags.Select(BuildWrapCompletionLabel)
            .Concat(TpsSpec.VolumeLevels.Select(BuildWrapCompletionLabel))
            .Concat(TpsSpec.DeliveryModes.Select(BuildWrapCompletionLabel))
            .Concat(TpsSpec.ArticulationStyles.Select(BuildWrapCompletionLabel))
            .Concat(TpsSpec.Emotions.Select(BuildWrapCompletionLabel))
            .ToArray();

    public static IEnumerable<string> ArchetypeSegmentCompletionLabels => ExpectedArchetypeSegmentCompletionLabels;

    public static IEnumerable<string> VendoredWrapperLabels => ExpectedVendoredWrapperLabels;

    [Test]
    public async Task EditorScreen_CompletionsExposeDetailedPayloadsForStructuredTpsSuggestions()
    {
        var page = await OpenEditorAsync();

        try
        {
            await EditorMonacoDriver.SetTextAsync(page, "[");
            var completions = await EditorMonacoDriver.GetCompletionsAsync(page, TitleLineNumber, CompletionStartColumn);

            var pronunciationCompletion = FindCompletion(completions, PronunciationCompletionLabel);
            var segmentCompletion = FindCompletion(completions, SegmentCompletionLabel);
            var markdownBoldCompletion = FindCompletion(completions, MarkdownBoldCompletionLabel);
            var markdownItalicCompletion = FindCompletion(completions, MarkdownItalicCompletionLabel);
            var millisecondPauseCompletion = FindCompletion(completions, MillisecondPauseCompletionLabel);
            var mediumEditPointCompletion = FindCompletion(completions, MediumEditPointCompletionLabel);
            var lowEditPointCompletion = FindCompletion(completions, LowEditPointCompletionLabel);

            await Assert.That(pronunciationCompletion.Detail).IsEqualTo("Simple pronunciation guide");
            await Assert.That(pronunciationCompletion.Documentation).Contains("readable pronunciation guide");
            await Assert.That(pronunciationCompletion.InsertText).IsEqualTo("[pronunciation:${1:KAM-uhl}]${2:camel}[/pronunciation]");
            await Assert.That(segmentCompletion.Detail).IsEqualTo("Segment header");
            await Assert.That(segmentCompletion.Documentation).Contains("structured TPS segment header");
            await Assert.That(segmentCompletion.InsertText).IsEqualTo("## [${1:Segment Name}|Speaker:${2:Host}|${3:140}WPM|${4:neutral}|${5:0:00-0:30}]");
            await Assert.That(markdownBoldCompletion.Detail).IsEqualTo("Markdown bold");
            await Assert.That(markdownBoldCompletion.InsertText).IsEqualTo("**${1:text}**");
            await Assert.That(markdownItalicCompletion.Detail).IsEqualTo("Markdown italic");
            await Assert.That(markdownItalicCompletion.InsertText).IsEqualTo("*${1:text}*");
            await Assert.That(millisecondPauseCompletion.Detail).IsEqualTo("Timed pause (ms)");
            await Assert.That(millisecondPauseCompletion.Documentation).Contains("milliseconds");
            await Assert.That(mediumEditPointCompletion.Detail).IsEqualTo("Priority edit point");
            await Assert.That(mediumEditPointCompletion.Documentation).Contains("medium priority");
            await Assert.That(lowEditPointCompletion.Detail).IsEqualTo("Priority edit point");
            await Assert.That(lowEditPointCompletion.Documentation).Contains("low priority");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_CompletionsExposeNewSdkAuthoringTokensForArchetypesAndVoiceShape()
    {
        var page = await OpenEditorAsync();

        try
        {
            await EditorMonacoDriver.SetTextAsync(page, "[");
            var completions = await EditorMonacoDriver.GetCompletionsAsync(page, TitleLineNumber, CompletionStartColumn);

            var energyCompletion = FindCompletion(completions, EnergyCompletionLabel);
            var melodyCompletion = FindCompletion(completions, MelodyCompletionLabel);
            var legatoCompletion = FindCompletion(completions, LegatoCompletionLabel);
            var staccatoCompletion = FindCompletion(completions, StaccatoCompletionLabel);
            var archetypeSegmentCompletion = FindCompletion(completions, ArchetypeSegmentCompletionLabel);

            await Assert.That(energyCompletion.Detail).IsEqualTo("Energy contour");
            await Assert.That(energyCompletion.Documentation).Contains("1 to 10");
            await Assert.That(energyCompletion.InsertText).IsEqualTo("[energy:${1:8}]${2:text}[/energy]");
            await Assert.That(melodyCompletion.Detail).IsEqualTo("Melody contour");
            await Assert.That(melodyCompletion.Documentation).Contains("pitch variation");
            await Assert.That(melodyCompletion.InsertText).IsEqualTo("[melody:${1:4}]${2:text}[/melody]");
            await Assert.That(legatoCompletion.Documentation).Contains("legato");
            await Assert.That(staccatoCompletion.Documentation).Contains("staccato");
            await Assert.That(archetypeSegmentCompletion.Detail).IsEqualTo("Segment header (archetype)");
            await Assert.That(archetypeSegmentCompletion.InsertText).Contains("Archetype:${3:Coach}");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_CompletionsPrioritizeSpeedAndImpactWrappersBeforeGeneralizedEmotion()
    {
        var page = await OpenEditorAsync();

        try
        {
            await EditorMonacoDriver.SetTextAsync(page, "[");
            var completions = await EditorMonacoDriver.GetCompletionsAsync(page, TitleLineNumber, CompletionStartColumn);

            var neutralIndex = IndexOfCompletion(completions, NeutralCompletionLabel);

            await Assert.That(IndexOfCompletion(completions, XslowCompletionLabel)).IsLessThan(neutralIndex);
            await Assert.That(IndexOfCompletion(completions, LoudCompletionLabel)).IsLessThan(neutralIndex);
            await Assert.That(IndexOfCompletion(completions, SarcasmCompletionLabel)).IsLessThan(neutralIndex);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    [MethodDataSource(nameof(ArchetypeSegmentCompletionLabels))]
    public async Task EditorScreen_CompletionsExposeEveryArchetypeSegmentCompletion(string completionLabel)
    {
        var page = await OpenEditorAsync();

        try
        {
            await EditorMonacoDriver.SetTextAsync(page, "[");
            var completions = await EditorMonacoDriver.GetCompletionsAsync(page, TitleLineNumber, CompletionStartColumn);

            _ = FindCompletion(completions, completionLabel);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    [MethodDataSource(nameof(VendoredWrapperLabels))]
    public async Task EditorScreen_CompletionsExposeVendoredEmotionVoiceAndDeliveryWrapper(string expectedLabel)
    {
        var page = await OpenEditorAsync();

        try
        {
            await EditorMonacoDriver.SetTextAsync(page, "[");
            var completions = await EditorMonacoDriver.GetCompletionsAsync(page, TitleLineNumber, CompletionStartColumn);

            _ = FindCompletion(completions, expectedLabel);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_CompletionsStayContextAwareForPauseInsertion()
    {
        var page = await OpenEditorAsync();

        try
        {
            await EditorMonacoDriver.SetTextAsync(page, " /");
            var completions = await EditorMonacoDriver.GetCompletionsAsync(page, TitleLineNumber, CompletionSlashColumn);

            var shortPause = FindCompletion(completions, "/");
            var mediumPause = FindCompletion(completions, "//");
            var timedPause = FindCompletion(completions, TimedPauseCompletionLabel);

            await Assert.That(shortPause.Detail).IsEqualTo("Short pause");
            await Assert.That(shortPause.InsertText).IsEqualTo(" / ");
            await Assert.That(mediumPause.Detail).IsEqualTo("Medium pause");
            await Assert.That(mediumPause.InsertText).IsEqualTo(" //");
            await Assert.That(timedPause.Detail).IsEqualTo("Timed pause");
            await Assert.That(timedPause.Documentation).Contains("seconds or milliseconds");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_HoverExplainsTitleAndStructuredHeaderMetadata()
    {
        var page = await OpenEditorAsync();

        try
        {
            await EditorMonacoDriver.SetTextAsync(page, MetadataDocument);

            var titleHover = await EditorMonacoDriver.GetHoverAsync(page, TitleLineNumber, FindColumn(TitleLine, "#"));
            var wpmHover = await EditorMonacoDriver.GetHoverAsync(page, SegmentLineNumber, FindColumn(SegmentLine, "140WPM"));
            var emotionHover = await EditorMonacoDriver.GetHoverAsync(page, SegmentLineNumber, FindColumn(SegmentLine, "Professional"));
            var speakerHover = await EditorMonacoDriver.GetHoverAsync(page, SegmentLineNumber, FindColumn(SegmentLine, "Speaker:Alex"));
            var timingHover = await EditorMonacoDriver.GetHoverAsync(page, SegmentLineNumber, FindColumn(SegmentLine, "0:00-0:30"));

            await Assert.That(titleHover).IsNotNull();
            await Assert.That(wpmHover).IsNotNull();
            await Assert.That(emotionHover).IsNotNull();
            await Assert.That(speakerHover).IsNotNull();
            await Assert.That(timingHover).IsNotNull();
            await Assert.That(titleHover!.Contents).Contains(content => content.Contains("Document title", StringComparison.Ordinal));
            await Assert.That(wpmHover!.Contents).Contains(content => content.Contains("Pace", StringComparison.Ordinal) &&
                content.Contains("target speaking pace", StringComparison.Ordinal));
            await Assert.That(emotionHover!.Contents).Contains(content => content.Contains("Emotion", StringComparison.Ordinal) &&
                content.Contains("delivery mood", StringComparison.Ordinal));
            await Assert.That(speakerHover!.Contents).Contains(content => content.Contains("Speaker assignment", StringComparison.Ordinal) &&
                content.Contains("Speaker:Name", StringComparison.Ordinal));
            await Assert.That(speakerHover.Contents).DoesNotContain(content => content.Contains("Header metadata", StringComparison.Ordinal));
            await Assert.That(string.Join("\n", speakerHover.Contents).Length).IsLessThanOrEqualTo(180);
            await Assert.That(timingHover!.Contents).Contains(content => content.Contains("Timing", StringComparison.Ordinal) &&
                content.Contains("planned time window", StringComparison.Ordinal));
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_HoverExplainsEditPointPriorityAndTimedPauseTags()
    {
        var page = await OpenEditorAsync();

        try
        {
            await EditorMonacoDriver.SetTextAsync(page, MetadataDocument);

            var editPointHover = await EditorMonacoDriver.GetHoverAsync(page, InlineLineNumber, FindColumn(InlineGuideLine, "[edit_point:high]"));
            var timedPauseHover = await EditorMonacoDriver.GetHoverAsync(page, InlineLineNumber, FindColumn(InlineGuideLine, "[pause:1500ms]"));

            await Assert.That(editPointHover).IsNotNull();
            await Assert.That(timedPauseHover).IsNotNull();
            await Assert.That(editPointHover!.Contents).Contains(content => content.Contains("Edit point", StringComparison.Ordinal) &&
                content.Contains("high", StringComparison.Ordinal));
            await Assert.That(timedPauseHover!.Contents).Contains(content => content.Contains("Timed pause", StringComparison.Ordinal) &&
                content.Contains("1500ms", StringComparison.Ordinal));
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_HoverExplainsArchetypeProfilesFromVendoredSdk()
    {
        var page = await OpenEditorAsync();

        try
        {
            await EditorMonacoDriver.SetTextAsync(page, ArchetypeDocument);

            var archetypeHover = await EditorMonacoDriver.GetHoverAsync(page, SegmentLineNumber, FindColumn(ArchetypeLine, "Archetype:Storyteller"));

            await Assert.That(archetypeHover).IsNotNull();
            await Assert.That(archetypeHover!.Contents).Contains(content => content.Contains("Archetype", StringComparison.Ordinal));
            await Assert.That(archetypeHover.Contents).Contains(content => content.Contains("Storyteller performance preset", StringComparison.Ordinal));
            await Assert.That(archetypeHover.Contents).Contains(content => content.Contains("pace, energy, melody", StringComparison.Ordinal));
            await Assert.That(string.Join("\n", archetypeHover.Contents).Length).IsLessThanOrEqualTo(180);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_TokenizesSimpleHeadersEscapesAndInlineWpmBadges()
    {
        var page = await OpenEditorAsync();

        try
        {
            await EditorMonacoDriver.SetTextAsync(page, SimpleHeaderDocument);

            var simpleSegmentTokens = await EditorMonacoDriver.TokenizeLineAsync(page, TitleLineNumber);
            var simpleBlockTokens = await EditorMonacoDriver.TokenizeLineAsync(page, SegmentLineNumber);
            var edgeCaseTokens = await EditorMonacoDriver.TokenizeLineAsync(page, 3);

            await Assert.That(simpleSegmentTokens.LineText).IsEqualTo(SimpleSegmentLine);
            await Assert.That(simpleBlockTokens.LineText).IsEqualTo(SimpleBlockLine);
            await Assert.That(edgeCaseTokens.LineText).IsEqualTo(TokenizationLine);
            await Assert.That(simpleSegmentTokens.Tokens).Contains(token => token.Type.Contains("header.segment.hash", StringComparison.Ordinal));
            await Assert.That(simpleBlockTokens.Tokens).Contains(token => token.Type.Contains("header.block.hash", StringComparison.Ordinal));
            await Assert.That(edgeCaseTokens.Tokens).Contains(token => token.Type.Contains("escape.sequence", StringComparison.Ordinal));
            await Assert.That(edgeCaseTokens.Tokens).Contains(token => token.Type.Contains("wpm.badge", StringComparison.Ordinal));
            await Assert.That(edgeCaseTokens.Tokens).Contains(token => token.Type.Contains("cue.open", StringComparison.Ordinal));
            await Assert.That(edgeCaseTokens.Tokens).Contains(token => token.Type.Contains("cue.close", StringComparison.Ordinal));
            await Assert.That(edgeCaseTokens.Tokens).Contains(token => token.Type.Contains("markdown.italic", StringComparison.Ordinal));
            await Assert.That(edgeCaseTokens.Tokens).Contains(token => token.Type.Contains("markdown.bold", StringComparison.Ordinal));
            await Assert.That(edgeCaseTokens.Tokens).DoesNotContain(token => token.Type.Contains("pause.short", StringComparison.Ordinal));
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_HoverExplainsMarkdownItalicSyntax()
    {
        var page = await OpenEditorAsync();

        try
        {
            await EditorMonacoDriver.SetTextAsync(page, SimpleHeaderDocument);

            var italicHover = await EditorMonacoDriver.GetHoverAsync(page, 3, FindColumn(TokenizationLine, "*calm*"));

            await Assert.That(italicHover).IsNotNull();
            await Assert.That(italicHover!.Contents).Contains(content => content.Contains("Markdown italic", StringComparison.Ordinal));
            await Assert.That(italicHover.Contents).Contains(content => content.Contains("*text*", StringComparison.Ordinal));
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private async Task<Microsoft.Playwright.IPage> OpenEditorAsync()
    {
        var page = await fixture.NewPageAsync(additionalContext: true);
        await EditorIsolatedDraftDriver.CreateDraftAsync(page, "# Monaco Assistance");
        return page;
    }

    private static EditorMonacoCompletionItem FindCompletion(EditorMonacoCompletionList completions, string label)
    {
        var completion = completions.Suggestions.SingleOrDefault(suggestion => string.Equals(suggestion.Label, label, StringComparison.Ordinal));
        return completion
            ?? throw new InvalidOperationException($"Unable to locate Monaco completion '{label}'.");
    }

    private static int IndexOfCompletion(EditorMonacoCompletionList completions, string label)
    {
        for (var index = 0; index < completions.Suggestions.Count; index++)
        {
            if (string.Equals(completions.Suggestions[index].Label, label, StringComparison.Ordinal))
            {
                return index;
            }
        }

        throw new InvalidOperationException($"Unable to locate Monaco completion '{label}'.");
    }

    private static int FindColumn(string line, string fragment)
    {
        var index = line.IndexOf(fragment, StringComparison.Ordinal);
        if (index < 0)
        {
            throw new InvalidOperationException($"Unable to locate \"{fragment}\" inside the Monaco regression probe line.");
        }

        return index + HoverInsideTokenOffset;
    }

    private static string BuildArchetypeSegmentCompletionLabel(string displayName) =>
        $"## [Segment Name|Speaker:Host|Archetype:{displayName}|neutral|0:00-0:30]";

    private static string BuildWrapCompletionLabel(string tagName) =>
        $"[{tagName}]text[/{tagName}]";

    private static string ToDisplayLabel(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : char.ToUpperInvariant(value[0]) + value[1..];
}
