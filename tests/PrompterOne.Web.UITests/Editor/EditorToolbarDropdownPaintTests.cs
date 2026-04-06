using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;
using System.Threading.Tasks;

namespace PrompterOne.Web.UITests;
[System.Obsolete]

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
[NotInParallel(UiTestParallelization.EditorAuthoringConstraintKey)]
public sealed class EditorToolbarDropdownPaintTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture){
    private const double MenuProbeInsetPx = 24;
    private const string ScenarioName = "editor-toolbar-dropdown-paint";

    [Test]
    public Task EditorToolbar_DropdownMenus_RenderAboveEditorSurface_AndReceivePointerHits() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(ScenarioName);

            await page.GotoAsync(
                BrowserTestConstants.Routes.EditorDemo,
                new() { WaitUntil = WaitUntilState.NetworkIdle });
            await Expect(page.GetByTestId(UiTestIds.Editor.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await EditorMonacoDriver.WaitUntilReadyAsync(page);

            var scenarioIndex = 1;
            foreach (var scenario in EditorToolbarCoverageScenarios.MenuScenarios)
            {
                await page.GetByTestId(scenario.TriggerTestId).ClickAsync();
                var panel = page.GetByTestId(scenario.PanelTestId);
                await Expect(panel)
                    .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.FastVisibleTimeoutMs });

                var hitTest = await page.EvaluateAsync<DropdownHitTestResult>(
                    """
                    args => {
                        const menu = document.querySelector(`[data-testid="${args.panelTestId}"]`);
                        if (!menu) {
                            return null;
                        }

                        const rect = menu.getBoundingClientRect();
                        const probeX = rect.left + (rect.width / 2);
                        const probeY = Math.min(rect.top + args.probeInsetPx, rect.bottom - 4);
                        const hitElement = document.elementFromPoint(probeX, probeY);

                        return {
                            hitInsideMenu: !!hitElement && (hitElement === menu || menu.contains(hitElement)),
                            hitTestId: hitElement?.getAttribute('data-testid') ?? '',
                            hitTagName: hitElement?.tagName ?? '',
                            hitClassName: hitElement?.className ?? '',
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
                    BuildStepName(scenarioIndex, scenario.TriggerTestId));

                await Assert.That(hitTest).IsNotNull();
                await Assert.That(hitTest!.HitInsideMenu).IsTrue().Because($"Expected dropdown '{scenario.PanelTestId}' to paint above the editor surface and receive pointer hits, but the probe hit '{hitTest.HitTagName}' with data-testid '{hitTest.HitTestId}' and class '{hitTest.HitClassName}'. Menu size was {hitTest.MenuWidth:0.##}x{hitTest.MenuHeight:0.##} at probe ({hitTest.ProbeX:0.##}, {hitTest.ProbeY:0.##}).");

                scenarioIndex++;
            }
        });

    [Test]
    public Task EditorToolbar_FloatingDropdownMenus_RenderAboveEditorSurface_AndReceivePointerHits() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(ScenarioName);

            await page.GotoAsync(
                BrowserTestConstants.Routes.EditorDemo,
                new() { WaitUntil = WaitUntilState.NetworkIdle });
            await Expect(page.GetByTestId(UiTestIds.Editor.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await EditorMonacoDriver.SetSelectionByTextAsync(page, BrowserTestConstants.Editor.TransformativeMoment);
            await Expect(page.GetByTestId(UiTestIds.Editor.FloatingBar))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.FastVisibleTimeoutMs });

            var scenarioIndex = 100;
            foreach (var scenario in EditorToolbarCoverageScenarios.FloatingMenuScenarios)
            {
                await page.GetByTestId(scenario.TriggerTestId).ClickAsync();
                var panel = page.GetByTestId(scenario.PanelTestId);
                await Expect(panel)
                    .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.FastVisibleTimeoutMs });

                var hitTest = await page.EvaluateAsync<DropdownHitTestResult>(
                    """
                    args => {
                        const menu = document.querySelector(`[data-testid="${args.panelTestId}"]`);
                        if (!menu) {
                            return null;
                        }

                        const rect = menu.getBoundingClientRect();
                        const probeX = rect.left + (rect.width / 2);
                        const probeY = Math.min(rect.top + args.probeInsetPx, rect.bottom - 4);
                        const hitElement = document.elementFromPoint(probeX, probeY);

                        return {
                            hitInsideMenu: !!hitElement && (hitElement === menu || menu.contains(hitElement)),
                            hitTestId: hitElement?.getAttribute('data-testid') ?? '',
                            hitTagName: hitElement?.tagName ?? '',
                            hitClassName: hitElement?.className ?? '',
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
                    BuildStepName(scenarioIndex, scenario.TriggerTestId));

                await Assert.That(hitTest).IsNotNull();
                await Assert.That(hitTest!.HitInsideMenu).IsTrue().Because($"Expected floating dropdown '{scenario.PanelTestId}' to paint above the editor surface and receive pointer hits, but the probe hit '{hitTest.HitTagName}' with data-testid '{hitTest.HitTestId}' and class '{hitTest.HitClassName}'. Menu size was {hitTest.MenuWidth:0.##}x{hitTest.MenuHeight:0.##} at probe ({hitTest.ProbeX:0.##}, {hitTest.ProbeY:0.##}).");

                scenarioIndex++;
            }
        });

    private static string BuildStepName(int scenarioIndex, string triggerTestId) =>
        $"{scenarioIndex:D2}-{triggerTestId}";

    private sealed class DropdownHitTestResult
    {
        public bool HitInsideMenu { get; init; }

        public string HitTestId { get; init; } = string.Empty;

        public string HitTagName { get; init; } = string.Empty;

        public string HitClassName { get; init; } = string.Empty;

        public double MenuHeight { get; init; }

        public double MenuWidth { get; init; }

        public double ProbeX { get; init; }

        public double ProbeY { get; init; }
    }
}
