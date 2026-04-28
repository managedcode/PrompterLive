using System.Text.RegularExpressions;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Services.Editor;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorStyledViewFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture)
{
    [Test]
    public Task EditorScreen_StyledEditorViewUsesMonacoAndWritesBackToSource() =>
        RunPageAsync(async page =>
        {
            await EditorIsolatedDraftDriver.CreateSeededDraftAsync(page, BrowserTestConstants.Scripts.DemoId);

            await EditorMonacoDriver.WaitUntilReadyAsync(page);

            await Expect(page.GetByTestId(UiTestIds.Editor.SourceTab)).ToHaveTextAsync("Raw");
            await Expect(page.GetByTestId(UiTestIds.Editor.WorkspaceEditorTab)).ToHaveTextAsync("Editor");
            await Expect(page.GetByTestId(UiTestIds.Editor.WorkspaceEditorTab)).ToHaveClassAsync(new Regex("active"));
            await Expect(page.GetByTestId(UiTestIds.Editor.MainPanel))
                .ToHaveAttributeAsync("data-authoring-mode", UiTestIds.Editor.StyledAuthoringMode);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceStage))
                .ToHaveAttributeAsync(EditorMonacoRuntimeContract.EditorEngineAttributeName, EditorMonacoRuntimeContract.EditorEngineAttributeValue);
            await Assert.That(await page.GetByTestId(UiTestIds.Editor.LegacyRenderedView).CountAsync()).IsEqualTo(0);
            await Assert.That(await page.Locator(".editor-rendered-card").CountAsync()).IsEqualTo(0);

            var styledState = await EditorMonacoDriver.GetStateAsync(page);
            await Assert.That(styledState.AuthoringMode).IsEqualTo(UiTestIds.Editor.StyledAuthoringMode);
            await Assert.That(styledState.Engine).IsEqualTo(EditorMonacoRuntimeContract.EditorEngineAttributeValue);
            await Assert.That(styledState.Text).Contains(BrowserTestConstants.Editor.RenderedOpeningProbe);
            await Assert.That(styledState.DecorationClasses.Any(item => item.Contains("po-tag", StringComparison.Ordinal))).IsTrue();
            await Assert.That(styledState.DecorationClasses.Any(item => item.Contains("po-object-chip", StringComparison.Ordinal))).IsTrue();
            var tagFontSize = await page.Locator(".po-tag").First.EvaluateAsync<string>("element => getComputedStyle(element).fontSize");
            await Assert.That(tagFontSize).IsEqualTo("0px");
            await Expect(page.Locator(".po-object-chip").First).ToBeVisibleAsync();
            await EditorMonacoDriver.SetTextAsync(
                page,
                $"""
                ## [Demo Segment|Speaker:Alex|140WPM|neutral]
                ### [Demo Block|140WPM|neutral]
                [edit_point:high] {BrowserTestConstants.Editor.RenderedOpeningProbe} / [highlight]important[/highlight] //
                """);
            styledState = await EditorMonacoDriver.GetStateAsync(page);
            await Assert.That(styledState.DecorationClasses.Any(item => item.Contains("po-object-cut", StringComparison.Ordinal))).IsTrue();
            await Assert.That(styledState.DecorationClasses.Any(item => item.Contains("po-object-pause-short", StringComparison.Ordinal))).IsTrue();
            await Assert.That(styledState.DecorationClasses.Any(item => item.Contains("po-object-highlight", StringComparison.Ordinal))).IsTrue();
            var headerLines = styledState.Text.Split('\n');
            var speakerHeaderLineIndex = Array.FindIndex(
                headerLines,
                line => line.Contains("Speaker:", StringComparison.Ordinal));
            await Assert.That(speakerHeaderLineIndex).IsGreaterThanOrEqualTo(0);
            var speakerHover = await EditorMonacoDriver.GetHoverAsync(
                page,
                speakerHeaderLineIndex + 1,
                headerLines[speakerHeaderLineIndex].IndexOf("Speaker:", StringComparison.Ordinal) + 2);
            await Assert.That(speakerHover).IsNotNull();
            await Assert.That(speakerHover!.Contents).Contains(content => content.Contains("Speaker assignment", StringComparison.Ordinal));
            await Assert.That(speakerHover.Contents).DoesNotContain(content => content.Contains("Header metadata", StringComparison.Ordinal));

            await EditorMonacoDriver.ReplaceTextAsync(
                page,
                text => text.Replace(
                    BrowserTestConstants.Editor.RenderedOpeningProbe,
                    BrowserTestConstants.Editor.RenderedOpeningRewrite,
                    StringComparison.Ordinal));

            await page.GetByTestId(UiTestIds.Editor.SourceTab).ClickAsync();
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            var sourceInput = EditorMonacoDriver.SourceInput(page);
            await Expect(sourceInput)
                .ToHaveValueAsync(
                    new Regex(Regex.Escape(BrowserTestConstants.Editor.RenderedOpeningRewrite)),
                    new() { Timeout = BrowserTestConstants.Timing.EditorMutationTimeoutMs });

            var sourceValue = await sourceInput.InputValueAsync();
            await Assert.That(sourceValue).Contains("## [Demo Segment|Speaker:Alex|140WPM|neutral]");
            await Assert.That(sourceValue).Contains("### [Demo Block|140WPM|neutral]");
        });
}
