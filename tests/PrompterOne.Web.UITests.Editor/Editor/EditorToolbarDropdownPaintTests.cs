using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[System.Obsolete]

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorToolbarDropdownPaintTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture)
{
    private const double MenuProbeInsetPx = 24;
    private const string ScenarioName = "editor-toolbar-dropdown-paint";

    public static IEnumerable<DropdownPaintScenario> ToolbarDropdownPaintScenarios =>
        EditorToolbarCoverageScenarios.MenuScenarios
            .Select((scenario, index) => new DropdownPaintScenario(index + 1, scenario.TriggerTestId, scenario.PanelTestId));

    public static IEnumerable<DropdownPaintScenario> FloatingDropdownPaintScenarios =>
        EditorToolbarCoverageScenarios.FloatingMenuScenarios
            .Select((scenario, index) => new DropdownPaintScenario(index + 100, scenario.TriggerTestId, scenario.PanelTestId));

    [Test]
    [MethodDataSource(nameof(ToolbarDropdownPaintScenarios))]
    public Task EditorToolbar_DropdownMenu_RendersAboveEditorSurface_AndReceivesPointerHits(DropdownPaintScenario scenario) =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(ScenarioName);

            await EditorRouteDriver.OpenReadyAsync(
                page,
                BrowserTestConstants.Routes.EditorDemo,
                $"{scenario.TriggerTestId}-editor-dropdown-paint");

            await AssertDropdownPaintAsync(page, scenario);
        });

    [Test]
    [MethodDataSource(nameof(FloatingDropdownPaintScenarios))]
    public Task EditorToolbar_FloatingDropdownMenu_RendersAboveEditorSurface_AndReceivesPointerHits(DropdownPaintScenario scenario) =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(ScenarioName);

            await EditorRouteDriver.OpenReadyAsync(
                page,
                BrowserTestConstants.Routes.EditorDemo,
                $"{scenario.TriggerTestId}-editor-floating-dropdown-paint");
            await EditorMonacoDriver.SetSelectionByTextAsync(page, BrowserTestConstants.Editor.TransformativeMoment);
            await Expect(page.GetByTestId(UiTestIds.Editor.FloatingBar))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.FastVisibleTimeoutMs });

            await AssertDropdownPaintAsync(page, scenario);
        });

    private static string BuildStepName(int scenarioIndex, string triggerTestId) =>
        $"{scenarioIndex:D2}-{triggerTestId}";

    private static async Task AssertDropdownPaintAsync(Microsoft.Playwright.IPage page, DropdownPaintScenario scenario)
    {
        await page.GetByTestId(scenario.TriggerTestId).ClickAsync();
        var panel = page.GetByTestId(scenario.PanelTestId);
        await Expect(panel)
            .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.FastVisibleTimeoutMs });

        var hitTest = await page.EvaluateAsync<DropdownHitTestResult>(
            """
            args => {
                const menu = document.querySelector(`[data-test="${args.panelTestId}"]`);
                if (!menu) {
                    return null;
                }

                const rect = menu.getBoundingClientRect();
                const probeX = rect.left + (rect.width / 2);
                const probeY = Math.min(rect.top + args.probeInsetPx, rect.bottom - 4);
                const hitElement = document.elementFromPoint(probeX, probeY);

                return {
                    hitInsideMenu: !!hitElement && (hitElement === menu || menu.contains(hitElement)),
                    hitTestId: hitElement?.getAttribute('data-test') ?? '',
                    hitTagName: hitElement?.tagName ?? '',
                    menuHeight: rect.height,
                    menuWidth: rect.width,
                    probeX,
                    probeY
                };
            }
            """,
            new
            {
                panelTestId = scenario.PanelTestId,
                probeInsetPx = MenuProbeInsetPx
            });

        await UiScenarioArtifacts.CaptureLocatorAsync(
            panel,
            ScenarioName,
            BuildStepName(scenario.ScenarioIndex, scenario.TriggerTestId));

        await Assert.That(hitTest).IsNotNull();
        await Assert.That(hitTest!.HitInsideMenu).IsTrue().Because($"Expected dropdown '{scenario.PanelTestId}' to paint above the editor surface and receive pointer hits, but the probe hit '{hitTest.HitTagName}' with data-test '{hitTest.HitTestId}'. Menu size was {hitTest.MenuWidth:0.##}x{hitTest.MenuHeight:0.##} at probe ({hitTest.ProbeX:0.##}, {hitTest.ProbeY:0.##}).");
    }

    private sealed class DropdownHitTestResult
    {
        public bool HitInsideMenu { get; init; }

        public string HitTestId { get; init; } = string.Empty;

        public string HitTagName { get; init; } = string.Empty;

        public double MenuHeight { get; init; }

        public double MenuWidth { get; init; }

        public double ProbeX { get; init; }

        public double ProbeY { get; init; }
    }

    public sealed record DropdownPaintScenario(
        int ScenarioIndex,
        string TriggerTestId,
        string PanelTestId);
}
