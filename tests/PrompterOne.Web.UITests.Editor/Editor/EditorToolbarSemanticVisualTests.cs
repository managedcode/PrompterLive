using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[System.Obsolete]

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
[NotInParallel(UiTestParallelization.EditorAuthoringConstraintKey)]
public sealed class EditorToolbarSemanticVisualTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
    public async Task EditorScreen_FloatingToolbarUsesTwoSemanticDotsAndDistinctTriggerColors()
    {
        UiScenarioArtifacts.ResetScenario(BrowserTestConstants.EditorFlow.ToolbarSemanticScenario);

        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync(new()
            {
                Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs
            });

            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await EditorMonacoDriver.SetSelectionByTextAsync(page, BrowserTestConstants.Editor.DemoFloatingSelectionTarget);

            var floatingBar = page.GetByTestId(UiTestIds.Editor.FloatingBar);
            await Expect(floatingBar).ToBeVisibleAsync();
            await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.FloatingToolbarSettleDelayMs);
            await UiScenarioArtifacts.CaptureLocatorAsync(
                floatingBar,
                BrowserTestConstants.EditorFlow.ToolbarSemanticScenario,
                BrowserTestConstants.EditorFlow.ToolbarSemanticStep);

            var metrics = await floatingBar.EvaluateAsync<FloatingToolbarSemanticMetrics>(
                """
                (element, args) => {
                    const selectChannels = value => (value?.match(/\d+(\.\d+)?/g) ?? []).slice(0, 3).map(Number);
                    const distance = (a, b) => a.length < 3 || b.length < 3
                        ? 0
                        : Math.abs(a[0] - b[0]) + Math.abs(a[1] - b[1]) + Math.abs(a[2] - b[2]);
                    const colorDistance = (firstNode, secondNode, propertyName = 'color') =>
                        distance(
                            selectChannels(window.getComputedStyle(firstNode ?? document.body)[propertyName]),
                            selectChannels(window.getComputedStyle(secondNode ?? document.body)[propertyName]));
                    const byTestId = testId => document.querySelector(`[data-test="${testId}"]`);
                    const readIconShape = iconTestId =>
                        byTestId(iconTestId)?.firstElementChild?.getAttribute(args.iconShapeAttributeName) ?? '';

                    const loudButton = byTestId(args.loudTestId);
                    const voiceButton = byTestId(args.floatingVoiceTestId);
                    const emotionButton = byTestId(args.floatingEmotionTestId);
                    const topVoiceButton = byTestId(args.topVoiceTestId);
                    const topEmotionButton = byTestId(args.topEmotionTestId);
                    const topPauseButton = byTestId(args.topPauseTestId);
                    const floatingPauseButton = byTestId(args.floatingPauseTestId);
                    const topSpeedButton = byTestId(args.topSpeedTestId);
                    const floatingSpeedButton = byTestId(args.floatingSpeedTestId);
                    const topInsertButton = byTestId(args.topInsertTestId);
                    const floatingInsertButton = byTestId(args.floatingInsertTestId);
                    const loudShape = readIconShape(args.loudLeadingIconTestId);
                    const floatingVoiceShape = readIconShape(args.floatingVoiceLeadingIconTestId);
                    const floatingEmotionShape = readIconShape(args.floatingEmotionLeadingIconTestId);

                    return {
                        floatingDotCount: [floatingVoiceShape, floatingEmotionShape]
                            .filter(shape => shape === args.dotShapeValue).length,
                        loudUsesDot: loudShape === args.dotShapeValue,
                        loudUsesSvg: loudShape === args.glyphShapeValue,
                        voiceColorDistance: colorDistance(topVoiceButton, voiceButton),
                        emotionColorDistance: colorDistance(topEmotionButton, emotionButton),
                        topGroupColorDistance: colorDistance(topVoiceButton, topEmotionButton),
                        floatingGroupColorDistance: colorDistance(voiceButton, emotionButton),
                        pauseColorDistance: colorDistance(topPauseButton, floatingPauseButton),
                        speedColorDistance: colorDistance(topSpeedButton, floatingSpeedButton),
                        insertColorDistance: colorDistance(topInsertButton, floatingInsertButton)
                    };
                }
                """,
                new
                {
                    loudTestId = UiTestIds.Editor.FloatingVoiceLoud,
                    loudLeadingIconTestId = UiTestIds.Editor.ToolbarActionLeadingIcon(UiTestIds.Editor.FloatingVoiceLoud),
                    floatingVoiceLeadingIconTestId = UiTestIds.Editor.ToolbarActionLeadingIcon(UiTestIds.Editor.FloatingVoice),
                    floatingEmotionLeadingIconTestId = UiTestIds.Editor.ToolbarActionLeadingIcon(UiTestIds.Editor.FloatingEmotion),
                    floatingEmotionTestId = UiTestIds.Editor.FloatingEmotion,
                    floatingInsertTestId = UiTestIds.Editor.FloatingInsert,
                    floatingPauseTestId = UiTestIds.Editor.FloatingPauseTrigger,
                    floatingSpeedTestId = UiTestIds.Editor.FloatingSpeedTrigger,
                    floatingVoiceTestId = UiTestIds.Editor.FloatingVoice,
                    iconShapeAttributeName = UiDataAttributes.Editor.IconShape,
                    dotShapeValue = UiDataAttributes.Editor.IconShapeDot,
                    glyphShapeValue = UiDataAttributes.Editor.IconShapeGlyph,
                    topEmotionTestId = UiTestIds.Editor.EmotionTrigger,
                    topInsertTestId = UiTestIds.Editor.InsertTrigger,
                    topPauseTestId = UiTestIds.Editor.PauseTrigger,
                    topSpeedTestId = UiTestIds.Editor.SpeedTrigger,
                    topVoiceTestId = UiTestIds.Editor.ColorTrigger
                });

            await Assert.That(metrics.FloatingDotCount).IsEqualTo(BrowserTestConstants.EditorFlow.FloatingSemanticDotCount);
            await Assert.That(metrics.LoudUsesDot).IsFalse();
            await Assert.That(metrics.LoudUsesSvg).IsTrue();
            await Assert.That(metrics.VoiceColorDistance >= BrowserTestConstants.EditorFlow.MinimumSemanticColorDistance).IsTrue();
            await Assert.That(metrics.EmotionColorDistance >= BrowserTestConstants.EditorFlow.MinimumSemanticColorDistance).IsTrue();
            await Assert.That(metrics.TopGroupColorDistance >= BrowserTestConstants.EditorFlow.MinimumSemanticGroupColorDistance).IsTrue();
            await Assert.That(metrics.FloatingGroupColorDistance >= BrowserTestConstants.EditorFlow.MinimumSemanticGroupColorDistance).IsTrue();
            await Assert.That(metrics.PauseColorDistance >= BrowserTestConstants.EditorFlow.MinimumSemanticColorDistance).IsTrue();
            await Assert.That(metrics.SpeedColorDistance >= BrowserTestConstants.EditorFlow.MinimumSemanticColorDistance).IsTrue();
            await Assert.That(metrics.InsertColorDistance >= BrowserTestConstants.EditorFlow.MinimumSemanticColorDistance).IsTrue();
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_VoiceDropdown_UsesEvenRowRhythm_AndClearSurfaceBorder()
    {
        UiScenarioArtifacts.ResetScenario(BrowserTestConstants.EditorFlow.ToolbarSurfaceScenario);

        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync(new()
            {
                Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs
            });

            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await page.GetByTestId(UiTestIds.Editor.ColorTrigger).ClickAsync();

            var voiceMenu = page.GetByTestId(UiTestIds.Editor.MenuColor);
            await Expect(voiceMenu).ToBeVisibleAsync();
            await UiScenarioArtifacts.CaptureLocatorAsync(
                voiceMenu,
                BrowserTestConstants.EditorFlow.ToolbarSurfaceScenario,
                BrowserTestConstants.EditorFlow.ToolbarSurfaceStep);

            var metrics = await voiceMenu.EvaluateAsync<DropdownSurfaceMetrics>(
                """
                (element, args) => {
                    const byTestId = testId => document.querySelector(`[data-test="${testId}"]`);
                    const parseColor = value => (value?.match(/\d+(\.\d+)?/g) ?? []).map(Number);
                    const readAlpha = value => {
                        const channels = parseColor(value);
                        return channels.length >= 4 ? channels[3] : 1;
                    };
                    const distance = (left, right) =>
                        left.length < 3 || right.length < 3
                            ? 0
                            : Math.abs(left[0] - right[0]) + Math.abs(left[1] - right[1]) + Math.abs(left[2] - right[2]);
                    const heights = args.itemTestIds
                        .map(byTestId)
                        .filter(Boolean)
                        .map(item => item.getBoundingClientRect().height);
                    const styles = window.getComputedStyle(element);
                    const clearLeading = byTestId(args.clearLeadingTestId)?.textContent?.replace(/\s+/g, ' ').trim() ?? '';
                    const clearLabel = byTestId(args.clearLabelTestId)?.textContent?.replace(/\s+/g, ' ').trim() ?? '';
                    const clearMeta = byTestId(args.clearMetaTestId)?.textContent?.replace(/\s+/g, ' ').trim() ?? '';
                    const clearParts = [clearLeading, clearLabel, clearMeta].filter(Boolean);

                    return {
                        menuWidth: element.getBoundingClientRect().width,
                        maxRowHeight: heights.length === 0 ? 0 : Math.max(...heights),
                        rowHeightDelta: heights.length === 0 ? 0 : Math.max(...heights) - Math.min(...heights),
                        borderAlpha: readAlpha(styles.borderTopColor),
                        borderContrast: distance(parseColor(styles.borderTopColor), parseColor(styles.backgroundColor)),
                        clearLabel: clearParts.join(' ')
                    };
                }
                """,
                new
                {
                    itemTestIds = new[]
                    {
                        UiTestIds.Editor.ColorLoud,
                        UiTestIds.Editor.ColorSoft,
                        UiTestIds.Editor.ColorWhisper,
                        UiTestIds.Editor.ColorStress,
                        UiTestIds.Editor.ColorGuide
                    },
                    clearLeadingTestId = UiTestIds.Editor.ToolbarActionLeading(UiTestIds.Editor.ColorClear),
                    clearLabelTestId = UiTestIds.Editor.ToolbarActionLabel(UiTestIds.Editor.ColorClear),
                    clearMetaTestId = UiTestIds.Editor.ToolbarActionMeta(UiTestIds.Editor.ColorClear)
                });

            await Assert.That(metrics.MenuWidth >= BrowserTestConstants.EditorFlow.MinimumDropdownSurfaceWidthPx).IsTrue();
            await Assert.That(metrics.MaxRowHeight <= BrowserTestConstants.EditorFlow.MaximumDropdownCompactRowHeightPx).IsTrue();
            await Assert.That(metrics.RowHeightDelta <= BrowserTestConstants.EditorFlow.MaximumDropdownRowHeightDeltaPx).IsTrue();
            await Assert.That(metrics.BorderAlpha >= BrowserTestConstants.EditorFlow.MinimumDropdownSurfaceBorderAlpha).IsTrue();
            await Assert.That(metrics.BorderContrast > 0).IsTrue();
            await Assert.That(metrics.ClearLabel).IsEqualTo(BrowserTestConstants.EditorFlow.VoiceClearLabel);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_EmotionDropdown_UsesCompactMenuRows()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync(new()
            {
                Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs
            });

            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await page.GetByTestId(UiTestIds.Editor.EmotionTrigger).ClickAsync();

            var emotionMenu = page.GetByTestId(UiTestIds.Editor.MenuEmotion);
            await Expect(emotionMenu).ToBeVisibleAsync();

            var metrics = await emotionMenu.EvaluateAsync<DropdownCompactMetrics>(
                """
                (element, args) => {
                    const heights = args.itemTestIds
                        .map(testId => document.querySelector(`[data-test="${testId}"]`))
                        .filter(Boolean)
                        .map(item => item.getBoundingClientRect().height);

                    return {
                        maxRowHeight: heights.length === 0 ? 0 : Math.max(...heights),
                        rowHeightDelta: heights.length === 0 ? 0 : Math.max(...heights) - Math.min(...heights)
                    };
                }
                """,
                new
                {
                    itemTestIds = new[]
                    {
                        "editor-emotion-warm",
                        "editor-emotion-concerned",
                        "editor-emotion-focused",
                        UiTestIds.Editor.EmotionMotivational,
                        "editor-emotion-neutral"
                    }
                });

            await Assert.That(metrics.MaxRowHeight <= BrowserTestConstants.EditorFlow.MaximumDropdownCompactRowHeightPx).IsTrue();
            await Assert.That(metrics.RowHeightDelta <= BrowserTestConstants.EditorFlow.MaximumDropdownRowHeightDeltaPx).IsTrue();
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_DropdownRows_LeftAlignMetaClusters_InToolbarAndFloatingMenus()
    {
        UiScenarioArtifacts.ResetScenario(BrowserTestConstants.EditorFlow.ToolbarDropdownAlignmentScenario);

        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync(new()
            {
                Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs
            });

            await EditorMonacoDriver.WaitUntilReadyAsync(page);

            await page.GetByTestId(UiTestIds.Editor.ColorTrigger).ClickAsync();
            var voiceMenu = page.GetByTestId(UiTestIds.Editor.MenuColor);
            await Expect(voiceMenu).ToBeVisibleAsync();
            await UiScenarioArtifacts.CaptureLocatorAsync(
                voiceMenu,
                BrowserTestConstants.EditorFlow.ToolbarDropdownAlignmentScenario,
                BrowserTestConstants.EditorFlow.ToolbarDropdownAlignmentTopStep);

            var topMetrics = await ReadDropdownClusterMetricsAsync(page, UiTestIds.Editor.ColorEnergy);
            await Assert.That(topMetrics.LabelMetaGap <= BrowserTestConstants.EditorFlow.MaximumDropdownInlineMetaGapPx).IsTrue().Because($"Expected the top toolbar voice dropdown to keep label and meta in one left-aligned cluster, but the gap was {topMetrics.LabelMetaGap:0.##}px.");

            await page.GetByTestId(UiTestIds.Editor.ColorTrigger).ClickAsync();
            await EditorMonacoDriver.SetSelectionByTextAsync(page, BrowserTestConstants.Editor.TransformativeMoment);
            await Expect(page.GetByTestId(UiTestIds.Editor.FloatingBar)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.Editor.FloatingVoice).ClickAsync();
            var floatingMenu = page.GetByTestId(UiTestIds.Editor.FloatingVoiceMenu);
            await Expect(floatingMenu).ToBeVisibleAsync();
            await UiScenarioArtifacts.CaptureLocatorAsync(
                floatingMenu,
                BrowserTestConstants.EditorFlow.ToolbarDropdownAlignmentScenario,
                BrowserTestConstants.EditorFlow.ToolbarDropdownAlignmentFloatingStep);

            var floatingMetrics = await ReadDropdownClusterMetricsAsync(page, UiTestIds.Editor.FloatingVoiceEnergy);
            await Assert.That(floatingMetrics.LabelMetaGap <= BrowserTestConstants.EditorFlow.MaximumDropdownInlineMetaGapPx).IsTrue().Because($"Expected the floating voice dropdown to keep label and meta in one left-aligned cluster, but the gap was {floatingMetrics.LabelMetaGap:0.##}px.");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static Task<DropdownAlignmentMetrics> ReadDropdownClusterMetricsAsync(IPage page, string itemTestId) =>
        page.EvaluateAsync<DropdownAlignmentMetrics>(
            """
            (args) => {
                const label = document.querySelector(`[data-test="${args.labelTestId}"]`);
                const meta = document.querySelector(`[data-test="${args.metaTestId}"]`);

                if (!label || !meta) {
                    return {
                        labelMetaGap: Number.POSITIVE_INFINITY
                    };
                }

                const labelRect = label.getBoundingClientRect();
                const metaRect = meta.getBoundingClientRect();

                return {
                    labelMetaGap: Math.max(0, metaRect.left - labelRect.right)
                };
            }
            """,
            new
            {
                labelTestId = UiTestIds.Editor.ToolbarActionLabel(itemTestId),
                metaTestId = UiTestIds.Editor.ToolbarActionMeta(itemTestId)
            });

    private readonly record struct FloatingToolbarSemanticMetrics(
        int FloatingDotCount,
        bool LoudUsesDot,
        bool LoudUsesSvg,
        double VoiceColorDistance,
        double EmotionColorDistance,
        double TopGroupColorDistance,
        double FloatingGroupColorDistance,
        double PauseColorDistance,
        double SpeedColorDistance,
        double InsertColorDistance);

    private readonly record struct DropdownSurfaceMetrics(
        double MenuWidth,
        double MaxRowHeight,
        double RowHeightDelta,
        double BorderAlpha,
        double BorderContrast,
        string ClearLabel);

    private readonly record struct DropdownCompactMetrics(
        double MaxRowHeight,
        double RowHeightDelta);

    private readonly record struct DropdownAlignmentMetrics(
        double LabelMetaGap);
}
