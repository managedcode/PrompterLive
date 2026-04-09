using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorMetadataTitleFlowTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture)
{
    private const string MetadataTitleScenario = "editor-metadata-title";
    private const string MetadataTitleStep = "01-rename-script-title";

    [Test]
    public Task EditorScreen_MetadataTitleEditUpdatesHeader_AndKeepsFrontMatterOutOfVisibleSource() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(MetadataTitleScenario);

            await EditorIsolatedDraftDriver.CreateSeededDraftAsync(page, BrowserTestConstants.Scripts.DemoId);

            var titleInput = page.GetByTestId(UiTestIds.Editor.Title);
            var authorInput = page.GetByTestId(UiTestIds.Editor.Author);
            var sourceInput = EditorMonacoDriver.SourceInput(page);

            await Expect(titleInput).ToHaveValueAsync(BrowserTestConstants.Scripts.ProductLaunchTitle);
            await titleInput.FillAsync(BrowserTestConstants.Editor.RetitledScript);
            await authorInput.ClickAsync();

            await Expect(page.GetByTestId(UiTestIds.Header.Title))
                .ToHaveTextAsync(BrowserTestConstants.Editor.RetitledScript);
            await Expect(titleInput).ToHaveValueAsync(BrowserTestConstants.Editor.RetitledScript);

            var visibleSource = await sourceInput.InputValueAsync();
            await Assert.That(visibleSource).DoesNotContain(BrowserTestConstants.Editor.TitleFieldPrefix);

            await UiScenarioArtifacts.CapturePageAsync(page, MetadataTitleScenario, MetadataTitleStep);
        });
}
