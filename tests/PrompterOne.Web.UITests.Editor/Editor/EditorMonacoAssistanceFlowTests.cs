namespace PrompterOne.Web.UITests;

[System.Obsolete]

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorMonacoAssistanceFlowTests(StandaloneAppFixture fixture)
{
    private const int TitleLineNumber = 1;
    private const int SegmentLineNumber = 2;
    private const int BlockLineNumber = 3;
    private const int InlineLineNumber = 4;
    private const int CompletionInvokeLineNumber = 1;
    private const int CompletionInvokeColumn = 2;
    private const int HoverInsideTokenOffset = 2;
    private const string TitleLine = "# System Design and Software Architecture for Vibe Coders";
    private const string SegmentLine = "## [Episode 2 - How Systems Talk to Each Other|140WPM|Professional|Speaker:Alex]";
    private const string BlockLine = "### Connection Patterns";
    private const string InlineLine = "[breath] [edit_point:medium] *carefully* **really** [phonetic:ˈkæməl]camel[/phonetic] [pronunciation:KAM-uhl]teleprompter[/pronunciation] [stress:de-VE-lop-ment]development[/stress] / // [pause:1000ms] [pause:2s]";
    private const string AssistanceDocument = """
        # System Design and Software Architecture for Vibe Coders
        ## [Episode 2 - How Systems Talk to Each Other|140WPM|Professional|Speaker:Alex]
        ### Connection Patterns
        [breath] [edit_point:medium] *carefully* **really** [phonetic:ˈkæməl]camel[/phonetic] [pronunciation:KAM-uhl]teleprompter[/pronunciation] [stress:de-VE-lop-ment]development[/stress] / // [pause:1000ms] [pause:2s]
        """;
    private static readonly string[] ExpectedCompletionLabels =
    [
        "# Title",
        "## [Segment Name|Speaker:Host|140WPM|neutral|0:00-0:30]",
        "### [Block Name|Speaker:Host|140WPM|focused]",
        "*text*",
        "**text**",
        "[breath]",
        "[edit_point:high]",
        "[edit_point:medium]",
        "[edit_point:low]",
        "[pause:1000ms]",
        "[phonetic:guide]text[/phonetic]",
        "[pronunciation:guide]text[/pronunciation]",
        "[stress:guide]text[/stress]",
        "[normal]text[/normal]",
        "/",
        "//"
    ];

    public static IEnumerable<string> CompletionLabels => ExpectedCompletionLabels;

    [Test]
    public async Task EditorScreen_TokenizesSupportedTpsSyntaxWithMonacoLanguage()
    {
        var page = await fixture.NewPageAsync(additionalContext: true);

        try
        {
            await EditorIsolatedDraftDriver.CreateDraftAsync(page, AssistanceDocument);

            var titleTokens = await EditorMonacoDriver.TokenizeLineAsync(page, TitleLineNumber);
            var segmentTokens = await EditorMonacoDriver.TokenizeLineAsync(page, SegmentLineNumber);
            var blockTokens = await EditorMonacoDriver.TokenizeLineAsync(page, BlockLineNumber);
            var inlineTokens = await EditorMonacoDriver.TokenizeLineAsync(page, InlineLineNumber);

            await Assert.That(titleTokens.LineText).IsEqualTo(TitleLine);
            await Assert.That(segmentTokens.LineText).IsEqualTo(SegmentLine);
            await Assert.That(blockTokens.LineText).IsEqualTo(BlockLine);
            await Assert.That(inlineTokens.LineText).IsEqualTo(InlineLine);
            await Assert.That(titleTokens.Tokens).Contains(token => token.Type.Contains("header.title.hash", StringComparison.Ordinal));
            await Assert.That(titleTokens.Tokens).Contains(token => token.Type.Contains("header.title.body", StringComparison.Ordinal));
            await Assert.That(segmentTokens.Tokens).Contains(token => token.Type.Contains("header.segment.hash", StringComparison.Ordinal));
            await Assert.That(segmentTokens.Tokens).Contains(token => token.Type.Contains("header.segment.body", StringComparison.Ordinal));
            await Assert.That(blockTokens.Tokens).Contains(token => token.Type.Contains("header.block.hash", StringComparison.Ordinal));
            await Assert.That(blockTokens.Tokens).Contains(token => token.Type.Contains("header.block.body", StringComparison.Ordinal));
            await Assert.That(inlineTokens.Tokens).Contains(token => token.Type.Contains("cue.breath", StringComparison.Ordinal));
            await Assert.That(inlineTokens.Tokens).Contains(token => token.Type.Contains("cue.editpoint", StringComparison.Ordinal));
            await Assert.That(inlineTokens.Tokens).Contains(token => token.Type.Contains("markdown.italic", StringComparison.Ordinal));
            await Assert.That(inlineTokens.Tokens).Contains(token => token.Type.Contains("markdown.bold", StringComparison.Ordinal));
            await Assert.That(inlineTokens.Tokens).Contains(token => token.Type.Contains("cue.pronunciation", StringComparison.Ordinal));
            await Assert.That(inlineTokens.Tokens).Contains(token => token.Type.Contains("pause.short", StringComparison.Ordinal));
            await Assert.That(inlineTokens.Tokens).Contains(token => token.Type.Contains("pause.long", StringComparison.Ordinal));
            await Assert.That(inlineTokens.Tokens).Contains(token => token.Type.Contains("pause.timed", StringComparison.Ordinal));
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    [MethodDataSource(nameof(CompletionLabels))]
    public async Task EditorScreen_ProvidesMonacoTpsCompletionLabel(string expectedLabel)
    {
        var page = await fixture.NewPageAsync(additionalContext: true);

        try
        {
            await EditorIsolatedDraftDriver.CreateDraftAsync(page, "[", waitForPersistedRoute: false);

            var completions = await EditorMonacoDriver.GetCompletionsAsync(page, CompletionInvokeLineNumber, CompletionInvokeColumn);

            await Assert.That(completions.Suggestions).IsNotEmpty();
            await Assert.That(completions.Suggestions).Contains(suggestion => string.Equals(suggestion.Label, expectedLabel, StringComparison.Ordinal));
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_ProvidesMonacoHoverHelpForTpsAuthoring()
    {
        var page = await fixture.NewPageAsync(additionalContext: true);

        try
        {
            await EditorIsolatedDraftDriver.CreateDraftAsync(page, AssistanceDocument);

            var breathHover = await EditorMonacoDriver.GetHoverAsync(page, InlineLineNumber, FindColumn(InlineLine, "[breath]"));
            var speakerHover = await EditorMonacoDriver.GetHoverAsync(page, SegmentLineNumber, FindColumn(SegmentLine, "Speaker:Alex"));
            var pauseHover = await EditorMonacoDriver.GetHoverAsync(page, InlineLineNumber, FindColumn(InlineLine, "//"));
            var guideHover = await EditorMonacoDriver.GetHoverAsync(page, InlineLineNumber, FindColumn(InlineLine, "development"));
            var markdownHover = await EditorMonacoDriver.GetHoverAsync(page, InlineLineNumber, FindColumn(InlineLine, "**really**"));

            await Assert.That(breathHover).IsNotNull();
            await Assert.That(speakerHover).IsNotNull();
            await Assert.That(pauseHover).IsNotNull();
            await Assert.That(guideHover).IsNotNull();
            await Assert.That(markdownHover).IsNotNull();
            await Assert.That(breathHover!.Contents).Contains(content => content.Contains("Breath mark", StringComparison.Ordinal) &&
                content.Contains("natural breath point", StringComparison.OrdinalIgnoreCase));
            await Assert.That(speakerHover!.Contents).Contains(content => content.Contains("Speaker assignment", StringComparison.OrdinalIgnoreCase) &&
                content.Contains("multi-speaker scripts", StringComparison.OrdinalIgnoreCase));
            await Assert.That(pauseHover!.Contents).Contains(content => content.Contains("Medium pause", StringComparison.Ordinal) &&
                content.Contains("600ms", StringComparison.Ordinal));
            await Assert.That(guideHover!.Contents).Contains(content => content.Contains("Syllable guide", StringComparison.Ordinal) &&
                content.Contains("de-VE-lop-ment", StringComparison.Ordinal));
            await Assert.That(markdownHover!.Contents).Contains(content => content.Contains("Markdown bold", StringComparison.Ordinal) &&
                content.Contains("**text**", StringComparison.Ordinal));
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static int FindColumn(string line, string fragment)
    {
        var index = line.IndexOf(fragment, StringComparison.Ordinal);
        if (index < 0)
        {
            throw new InvalidOperationException($"Unable to locate \"{fragment}\" inside the Monaco assistance probe line.");
        }

        return index + HoverInsideTokenOffset;
    }
}
