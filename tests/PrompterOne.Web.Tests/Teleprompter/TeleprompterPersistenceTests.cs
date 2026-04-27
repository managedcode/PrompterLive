using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Storage;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class TeleprompterPersistenceTests : BunitContext
{
    private static readonly TimeSpan PersistenceAssertionTimeout = TimeSpan.FromSeconds(10);
    private const int MinimumReaderSpeedWpm = 60;
    private const int ReaderSpeedStepWpm = 10;
    private const string WordsPerMinuteSuffix = "WPM";
    private const int PersistedFocalPointPercent = 42;
    private const int PersistedFontSize = 40;
    private const int PersistedTextWidthPercent = 79;
    private const string PersistedTextWidthLabel = "79%";
    private const string EnabledCameraAttribute = "true";
    private const string HorizontalMirrorTransform = "scaleX(-1)";
    private const string VerticalMirrorTransform = "scaleY(-1)";
    private const int UpdatedFocalPointPercent = 37;
    private const int UpdatedFontSize = 52;
    private const int UpdatedTextWidthPercent = 82;
    private const string UpdatedTextWidthLabel = "82%";
    private const double ReaderFontBaselinePixels = 36d;
    private const double PersistedFontScale = PersistedFontSize / ReaderFontBaselinePixels;
    private const double UpdatedFontScale = UpdatedFontSize / ReaderFontBaselinePixels;
    private const double PersistedTextWidthRatio = PersistedTextWidthPercent / 100d;
    private const double UpdatedTextWidthRatio = UpdatedTextWidthPercent / 100d;
    private const string DisabledCameraAttribute = "false";
    private const string JustifyAlignmentValue = "justify";
    private const string InvertedOrientationTransform = "rotate(180deg)";
    private const string InvertedOrientationValue = "inverted";
    private const string LandscapeOrientationValue = "landscape";
    private const string PortraitCounterClockwiseOrientationTransform = "rotate(270deg)";
    private const string PortraitCounterClockwiseOrientationValue = "portrait-270";
    private const string PortraitOrientationTransform = "rotate(90deg)";
    private const string PortraitOrientationValue = "portrait";
    private const string RightAlignmentValue = "right";

    [Test]
    public void TeleprompterPage_RestoresPersistedReaderLayoutSettings()
    {
        var harness = TestHarnessFactory.Create(this);
        harness.JsRuntime.SavedValues[BrowserAppSettingsKeys.ReaderSettings] = new ReaderSettings(
            FontScale: PersistedFontScale,
            TextWidth: PersistedTextWidthRatio,
            MirrorText: true,
            MirrorVertical: true,
            TextAlignment: ReaderTextAlignment.Right,
            TextOrientation: ReaderTextOrientation.Portrait,
            FocalPointPercent: PersistedFocalPointPercent,
            SpeedCueDisplayMode: ReaderSpeedCueDisplayMode.Multiplier);

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.TeleprompterDemo);
        var cut = Render<TeleprompterPage>();

        cut.WaitForAssertion(() =>
        {
            var expectedBaseSpeedWpm = harness.Session.State.ScriptData?.TargetWpm ?? 0;
            Assert.Equal(
                PersistedTextWidthLabel,
                cut.FindByTestId(UiTestIds.Teleprompter.WidthValue).TextContent.Trim());
            Assert.Equal(
                PersistedFontSize.ToString(CultureInfo.InvariantCulture),
                cut.FindByTestId(UiTestIds.Teleprompter.FontValue).TextContent.Trim());
            Assert.Equal(
                BuildSpeedMultiplierLabel(expectedBaseSpeedWpm, expectedBaseSpeedWpm),
                cut.FindByTestId(UiTestIds.Teleprompter.SpeedValue).TextContent.Trim());
            Assert.Equal(
                PersistedFontSize.ToString(CultureInfo.InvariantCulture),
                cut.FindByTestId(UiTestIds.Teleprompter.FontSlider).GetAttribute("value"));
            Assert.Equal(
                PersistedTextWidthPercent.ToString(CultureInfo.InvariantCulture),
                cut.FindByTestId(UiTestIds.Teleprompter.WidthSlider).GetAttribute("value"));
            Assert.Equal(
                $"top:{PersistedFocalPointPercent}%;",
                cut.FindByTestId(UiTestIds.Teleprompter.FocalGuide).GetAttribute("style"));
            var clusterWrapStyle = cut.FindByTestId(UiTestIds.Teleprompter.ClusterWrap).GetAttribute("style") ?? string.Empty;
            var cameraStyle = cut.FindByTestId(UiTestIds.Teleprompter.CameraBackground).GetAttribute("style") ?? string.Empty;
            Assert.Equal(
                PortraitOrientationValue,
                cut.FindByTestId(UiTestIds.Teleprompter.ClusterWrap).GetAttribute("data-reader-orientation"));
            Assert.Equal(
                RightAlignmentValue,
                cut.FindByTestId(UiTestIds.Teleprompter.ClusterWrap).GetAttribute("data-reader-text-alignment"));
            Assert.Equal("false", cut.FindByTestId(UiTestIds.Teleprompter.SpeedCueDisplayWpm).GetAttribute("data-active"));
            Assert.Equal("true", cut.FindByTestId(UiTestIds.Teleprompter.SpeedCueDisplayMultiplier).GetAttribute("data-active"));
            Assert.Contains(PortraitOrientationTransform, clusterWrapStyle, StringComparison.Ordinal);
            Assert.Contains(HorizontalMirrorTransform, clusterWrapStyle, StringComparison.Ordinal);
            Assert.Contains(VerticalMirrorTransform, clusterWrapStyle, StringComparison.Ordinal);
            Assert.Contains(PortraitOrientationTransform, cameraStyle, StringComparison.Ordinal);
            Assert.Contains(HorizontalMirrorTransform, cameraStyle, StringComparison.Ordinal);
            Assert.Contains(VerticalMirrorTransform, cameraStyle, StringComparison.Ordinal);
        }, PersistenceAssertionTimeout);
    }

    [Test]
    public async Task TeleprompterPage_PersistsReaderLayoutAndCameraPreferenceChanges()
    {
        var harness = TestHarnessFactory.Create(this);
        var initialShowCameraScene = harness.Session.State.ReaderSettings.ShowCameraScene;
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.TeleprompterDemo);
        var cut = Render<TeleprompterPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Teleprompter.WidthSlider, cut.Markup, StringComparison.Ordinal));
        var baselineSpeedWpm = ParseWordsPerMinuteValue(cut.FindByTestId(UiTestIds.Teleprompter.SpeedValue).TextContent.Trim());
        var expectedUpdatedSpeedWpm = baselineSpeedWpm + ReaderSpeedStepWpm;

        await cut.FindByTestId(UiTestIds.Teleprompter.FontSlider).InputAsync(UpdatedFontSize);
        await cut.FindByTestId(UiTestIds.Teleprompter.WidthSlider).InputAsync(UpdatedTextWidthPercent);
        await cut.FindByTestId(UiTestIds.Teleprompter.FocalSlider).InputAsync(UpdatedFocalPointPercent);
        await cut.FindByTestId(UiTestIds.Teleprompter.SpeedUp).ClickAsync();
        await cut.FindByTestId(UiTestIds.Teleprompter.MirrorHorizontalToggle).ClickAsync();
        await cut.FindByTestId(UiTestIds.Teleprompter.MirrorVerticalToggle).ClickAsync();
        await cut.FindByTestId(UiTestIds.Teleprompter.AlignmentJustify).ClickAsync();
        await cut.FindByTestId(UiTestIds.Teleprompter.OrientationToggle).ClickAsync();
        await cut.FindByTestId(UiTestIds.Teleprompter.SpeedCueDisplayMultiplier).ClickAsync();
        await cut.FindByTestId(UiTestIds.Teleprompter.AutoLoopToggle).ClickAsync();
        await cut.FindByTestId(UiTestIds.Teleprompter.CameraToggle).ClickAsync();

        cut.WaitForAssertion(() =>
        {
            var savedSettings = harness.JsRuntime.GetSavedValue<ReaderSettings>(BrowserAppSettingsKeys.ReaderSettings);
            var expectedShowCameraScene = !initialShowCameraScene;
            var expectedCameraAttribute = expectedShowCameraScene
                ? EnabledCameraAttribute
                : DisabledCameraAttribute;

            Assert.Equal(UpdatedFontSize, int.Parse(cut.FindByTestId(UiTestIds.Teleprompter.FontValue).TextContent.Trim(), CultureInfo.InvariantCulture));
            Assert.Equal(UpdatedTextWidthLabel, cut.FindByTestId(UiTestIds.Teleprompter.WidthValue).TextContent.Trim());
            Assert.Equal(
                BuildSpeedMultiplierLabel(
                    harness.Session.State.ScriptData?.TargetWpm ?? expectedUpdatedSpeedWpm,
                    expectedUpdatedSpeedWpm),
                cut.FindByTestId(UiTestIds.Teleprompter.SpeedValue).TextContent.Trim());
            Assert.Equal($"top:{UpdatedFocalPointPercent}%;", cut.FindByTestId(UiTestIds.Teleprompter.FocalGuide).GetAttribute("style"));
            var clusterWrapStyle = cut.FindByTestId(UiTestIds.Teleprompter.ClusterWrap).GetAttribute("style") ?? string.Empty;
            var cameraStyle = cut.FindByTestId(UiTestIds.Teleprompter.CameraBackground).GetAttribute("style") ?? string.Empty;
            Assert.Equal(
                PortraitOrientationValue,
                cut.FindByTestId(UiTestIds.Teleprompter.ClusterWrap).GetAttribute("data-reader-orientation"));
            Assert.Equal(
                JustifyAlignmentValue,
                cut.FindByTestId(UiTestIds.Teleprompter.ClusterWrap).GetAttribute("data-reader-text-alignment"));
            Assert.Contains(PortraitOrientationTransform, clusterWrapStyle, StringComparison.Ordinal);
            Assert.Contains(HorizontalMirrorTransform, clusterWrapStyle, StringComparison.Ordinal);
            Assert.Contains(VerticalMirrorTransform, clusterWrapStyle, StringComparison.Ordinal);
            Assert.Contains(PortraitOrientationTransform, cameraStyle, StringComparison.Ordinal);
            Assert.Contains(HorizontalMirrorTransform, cameraStyle, StringComparison.Ordinal);
            Assert.Contains(VerticalMirrorTransform, cameraStyle, StringComparison.Ordinal);

            Assert.Equal(UpdatedFontScale, savedSettings.FontScale, 2);
            Assert.Equal(UpdatedTextWidthRatio, savedSettings.TextWidth, 4);
            Assert.Equal(UpdatedFocalPointPercent, savedSettings.FocalPointPercent);
            Assert.Equal(expectedUpdatedSpeedWpm, savedSettings.ScrollSpeed, 2);
            Assert.True(savedSettings.MirrorText);
            Assert.True(savedSettings.MirrorVertical);
            Assert.Equal(ReaderTextAlignment.Justify, savedSettings.TextAlignment);
            Assert.Equal(ReaderTextOrientation.Portrait, savedSettings.TextOrientation);
            Assert.Equal(ReaderSpeedCueDisplayMode.Multiplier, savedSettings.SpeedCueDisplayMode);
            Assert.False(savedSettings.AutoLoop);
            Assert.Equal(expectedShowCameraScene, savedSettings.ShowCameraScene);
            Assert.Equal("false", cut.FindByTestId(UiTestIds.Teleprompter.AutoLoopToggle).GetAttribute("aria-pressed"));
            Assert.Equal(expectedCameraAttribute, cut.FindByTestId(UiTestIds.Teleprompter.CameraBackground).GetAttribute("data-camera-autostart"));
            Assert.True(harness.Session.State.ReaderSettings.MirrorText);
            Assert.True(harness.Session.State.ReaderSettings.MirrorVertical);
            Assert.Equal(ReaderTextAlignment.Justify, harness.Session.State.ReaderSettings.TextAlignment);
            Assert.Equal(ReaderTextOrientation.Portrait, harness.Session.State.ReaderSettings.TextOrientation);
            Assert.Equal(ReaderSpeedCueDisplayMode.Multiplier, harness.Session.State.ReaderSettings.SpeedCueDisplayMode);
            Assert.False(harness.Session.State.ReaderSettings.AutoLoop);
            Assert.Equal(expectedUpdatedSpeedWpm, harness.Session.State.ReaderSettings.ScrollSpeed, 2);
            Assert.Equal(expectedShowCameraScene, harness.Session.State.ReaderSettings.ShowCameraScene);
        }, PersistenceAssertionTimeout);
    }

    [Test]
    public async Task TeleprompterPage_CyclesAndPersistsAllReaderOrientations()
    {
        var harness = TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.TeleprompterDemo);
        var cut = Render<TeleprompterPage>();

        cut.WaitForAssertion(() =>
            Assert.Equal(
                LandscapeOrientationValue,
                cut.FindByTestId(UiTestIds.Teleprompter.ClusterWrap).GetAttribute("data-reader-orientation")));

        await cut.FindByTestId(UiTestIds.Teleprompter.OrientationToggle).ClickAsync();
        cut.WaitForAssertion(() =>
            AssertReaderOrientation(
                cut,
                harness,
                ReaderTextOrientation.Portrait,
                PortraitOrientationValue,
                PortraitOrientationTransform));

        await cut.FindByTestId(UiTestIds.Teleprompter.OrientationToggle).ClickAsync();
        cut.WaitForAssertion(() =>
            AssertReaderOrientation(
                cut,
                harness,
                ReaderTextOrientation.Inverted,
                InvertedOrientationValue,
                InvertedOrientationTransform));

        await cut.FindByTestId(UiTestIds.Teleprompter.OrientationToggle).ClickAsync();
        cut.WaitForAssertion(() =>
            AssertReaderOrientation(
                cut,
                harness,
                ReaderTextOrientation.PortraitCounterClockwise,
                PortraitCounterClockwiseOrientationValue,
                PortraitCounterClockwiseOrientationTransform));

        await cut.FindByTestId(UiTestIds.Teleprompter.OrientationToggle).ClickAsync();
        cut.WaitForAssertion(() =>
        {
            var savedSettings = harness.JsRuntime.GetSavedValue<ReaderSettings>(BrowserAppSettingsKeys.ReaderSettings);
            var clusterWrapStyle = cut.FindByTestId(UiTestIds.Teleprompter.ClusterWrap).GetAttribute("style") ?? string.Empty;

            Assert.Equal(
                LandscapeOrientationValue,
                cut.FindByTestId(UiTestIds.Teleprompter.ClusterWrap).GetAttribute("data-reader-orientation"));
            Assert.DoesNotContain(PortraitOrientationTransform, clusterWrapStyle, StringComparison.Ordinal);
            Assert.DoesNotContain(InvertedOrientationTransform, clusterWrapStyle, StringComparison.Ordinal);
            Assert.DoesNotContain(PortraitCounterClockwiseOrientationTransform, clusterWrapStyle, StringComparison.Ordinal);
            Assert.Equal(ReaderTextOrientation.Landscape, savedSettings.TextOrientation);
            Assert.Equal(ReaderTextOrientation.Landscape, harness.Session.State.ReaderSettings.TextOrientation);
        }, PersistenceAssertionTimeout);
    }

    [Test]
    public void TeleprompterPage_SpeedDown_ClampsAtSixtyWordsPerMinute()
    {
        var harness = TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.TeleprompterDemo);
        var cut = Render<TeleprompterPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Teleprompter.SpeedValue, cut.Markup, StringComparison.Ordinal));
        var baselineSpeedWpm = ParseWordsPerMinuteValue(cut.FindByTestId(UiTestIds.Teleprompter.SpeedValue).TextContent.Trim());
        var speedReductionClicks = Math.Max(0, (baselineSpeedWpm - MinimumReaderSpeedWpm) / ReaderSpeedStepWpm);

        for (var clickIndex = 0; clickIndex < speedReductionClicks + 1; clickIndex++)
        {
            cut.FindByTestId(UiTestIds.Teleprompter.SpeedDown).Click();
        }

        cut.WaitForAssertion(() =>
        {
            var savedSettings = harness.JsRuntime.GetSavedValue<ReaderSettings>(BrowserAppSettingsKeys.ReaderSettings);

            Assert.Equal(BuildWordsPerMinuteLabel(MinimumReaderSpeedWpm), cut.FindByTestId(UiTestIds.Teleprompter.SpeedValue).TextContent.Trim());
            Assert.Equal(MinimumReaderSpeedWpm, savedSettings.ScrollSpeed, 2);
            Assert.Equal(MinimumReaderSpeedWpm, harness.Session.State.ReaderSettings.ScrollSpeed, 2);
        }, PersistenceAssertionTimeout);
    }

    private static string BuildWordsPerMinuteLabel(int speedWpm) =>
        $"{speedWpm} {WordsPerMinuteSuffix}";

    private static string BuildSpeedMultiplierLabel(int baseWpm, int speedWpm) =>
        string.Concat(
            "x",
            (speedWpm / (double)Math.Max(MinimumReaderSpeedWpm, baseWpm)).ToString(
                "0.##",
                CultureInfo.InvariantCulture));

    private static int ParseWordsPerMinuteValue(string speedText)
    {
        var tokens = speedText
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0 || !int.TryParse(tokens[0], out var parsedWpm))
        {
            throw new InvalidOperationException($"Unable to parse teleprompter speed value from '{speedText}'.");
        }

        return parsedWpm;
    }

    private static void AssertReaderOrientation(
        IRenderedComponent<TeleprompterPage> cut,
        AppHarness harness,
        ReaderTextOrientation expectedOrientation,
        string expectedDataAttribute,
        string expectedTransform)
    {
        var savedSettings = harness.JsRuntime.GetSavedValue<ReaderSettings>(BrowserAppSettingsKeys.ReaderSettings);
        var clusterWrapStyle = cut.FindByTestId(UiTestIds.Teleprompter.ClusterWrap).GetAttribute("style") ?? string.Empty;
        var cameraStyle = cut.FindByTestId(UiTestIds.Teleprompter.CameraBackground).GetAttribute("style") ?? string.Empty;

        Assert.Equal(
            expectedDataAttribute,
            cut.FindByTestId(UiTestIds.Teleprompter.ClusterWrap).GetAttribute("data-reader-orientation"));
        Assert.Contains(expectedTransform, clusterWrapStyle, StringComparison.Ordinal);
        Assert.Contains(expectedTransform, cameraStyle, StringComparison.Ordinal);
        Assert.Equal(expectedOrientation, savedSettings.TextOrientation);
        Assert.Equal(expectedOrientation, harness.Session.State.ReaderSettings.TextOrientation);
    }
}
