using System.Globalization;
using System.Text.RegularExpressions;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class TeleprompterSettingsFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture)
{
    private const string ReaderSpeedDialUpdatedLabel = "8/10";
    private const string ReaderSpeedDialUpdatedValue = "8";
    private const int ReaderSpeedDialUpdatedWpm = 402;
    private const int ReaderSpeedStepWpm = 10;
    private const int TeleprompterDemoBaseWpm = 140;
    private const string WordsPerMinuteSuffix = "WPM";
    private readonly record struct LayoutBounds(double X, double Y, double Width, double Height);

    [Test]
    public Task TeleprompterAndSettingsScreens_RespondToCoreControls() =>
        RunPageAsync(async page =>
        {
            await VerifyTeleprompterControlsAsync(page);
            var readerCameraWasOn = await VerifySettingsControlsAsync(page);
            await VerifyTeleprompterCameraAutostartAsync(page, readerCameraWasOn);
        });

    [Test]
    public Task Teleprompter_UsesStoredPrimaryCameraAsBackgroundLayer() =>
        RunPageAsync(async page =>
        {
            await BrowserRouteDriver.OpenPageAsync(
                page,
                BrowserTestConstants.Routes.Library,
                UiTestIds.Library.Page,
                nameof(Teleprompter_UsesStoredPrimaryCameraAsBackgroundLayer));
            var cameraDeviceId = await ResolveCameraDeviceIdAsync(page);
            await SeedStoredTeleprompterSceneAsync(page, cameraDeviceId);

            await ReaderRouteDriver.OpenTeleprompterAsync(page, BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page)).ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.CameraBackground)).ToHaveCountAsync(1);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.CameraBackground)).ToHaveAttributeAsync("data-camera-role", "primary");
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.CameraBackground)).ToHaveAttributeAsync("data-camera-device-id", cameraDeviceId);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.CameraBackground)).ToHaveCountAsync(1);
        });

    private static async Task VerifyTeleprompterControlsAsync(Microsoft.Playwright.IPage page)
    {
        await ReaderRouteDriver.OpenTeleprompterAsync(page, BrowserTestConstants.Routes.TeleprompterDemo);
        await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page)).ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
        await Expect(page.GetByTestId(UiTestIds.Teleprompter.EdgeSection)).ToContainTextAsync(BrowserTestConstants.TeleprompterFlow.OpeningBlock);
        await Expect(page.GetByTestId(UiTestIds.Teleprompter.CardText(0))).ToContainTextAsync(BrowserTestConstants.TeleprompterFlow.OpeningLine);
        await Expect(page.GetByTestId(UiTestIds.Teleprompter.CardText(0))).Not.ToContainTextAsync(BrowserTestConstants.TeleprompterFlow.CollapsedOpeningLine);
        await Expect(page.GetByTestId(UiTestIds.Teleprompter.CameraBackground)).ToHaveAttributeAsync(
            BrowserTestConstants.TeleprompterFlow.CameraAutostartAttribute,
            BrowserTestConstants.Regexes.CameraAutoStart);
        await Expect(page.GetByTestId(UiTestIds.Teleprompter.CameraBackground)).ToHaveCountAsync(1);
        await AssertTeleprompterChromeVisibilityAsync(page);

        var speedValue = page.GetByTestId(UiTestIds.Teleprompter.SpeedValue);
        var baselineSpeedText = await speedValue.TextContentAsync() ?? string.Empty;
        var baselineSpeedWpm = ParseWordsPerMinuteValue(baselineSpeedText);

        await page.GetByTestId(UiTestIds.Teleprompter.SpeedUp).ClickAsync();
        await Expect(speedValue).ToHaveTextAsync($"{baselineSpeedWpm + ReaderSpeedStepWpm} {WordsPerMinuteSuffix}");

        await SetRangeValueAsync(page.GetByTestId(UiTestIds.Teleprompter.SpeedDial), ReaderSpeedDialUpdatedValue);
        await Expect(speedValue).ToHaveTextAsync($"{ReaderSpeedDialUpdatedWpm} {WordsPerMinuteSuffix}");
        await Expect(page.GetByTestId(UiTestIds.Teleprompter.SpeedDial)).ToHaveValueAsync(ReaderSpeedDialUpdatedValue);
        await Expect(page.GetByTestId(UiTestIds.Teleprompter.SpeedDialValue)).ToHaveTextAsync(ReaderSpeedDialUpdatedLabel);

        await page.GetByTestId(UiTestIds.Teleprompter.SpeedCueDisplayMultiplier).ClickAsync();
        await Expect(speedValue)
            .ToHaveTextAsync(BuildSpeedMultiplierLabel(TeleprompterDemoBaseWpm, ReaderSpeedDialUpdatedWpm));
        await Expect(page.GetByTestId(UiTestIds.Teleprompter.SpeedCueDisplayMultiplier))
            .ToHaveAttributeAsync(BrowserTestConstants.State.ActiveAttribute, BrowserTestConstants.Teleprompter.ActiveStateValue);

        var cameraToggle = page.GetByTestId(UiTestIds.Teleprompter.CameraToggle);
        var cameraWasActive = await HasActiveStateAsync(cameraToggle);
        await cameraToggle.ClickAsync();

        if (cameraWasActive)
        {
            await Expect(cameraToggle).ToHaveAttributeAsync(
                BrowserTestConstants.State.ActiveAttribute,
                BrowserTestConstants.Teleprompter.InactiveStateValue);
        }
        else
        {
            await Expect(cameraToggle).ToHaveAttributeAsync(
                BrowserTestConstants.State.ActiveAttribute,
                BrowserTestConstants.Teleprompter.ActiveStateValue);
        }

        await page.GetByTestId(UiTestIds.Teleprompter.WidthSlider).EvaluateAsync(BrowserTestConstants.TeleprompterFlow.WidthInputScript);
        await Expect(page.GetByTestId(UiTestIds.Teleprompter.WidthValue)).ToHaveTextAsync(BrowserTestConstants.TeleprompterFlow.WidthAfterChange);

        var playToggle = page.GetByTestId(UiTestIds.Teleprompter.PlayToggle);
        await playToggle.ClickAsync();
        await Expect(playToggle).ToBeVisibleAsync();
        await Expect(playToggle.Locator(BrowserTestConstants.Teleprompter.PauseToggleIconSelector))
            .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ReaderPlaybackReadyTimeoutMs });
        await Expect(page.GetByTestId(UiTestIds.Teleprompter.TimeValue))
            .Not.ToHaveTextAsync(
                BrowserTestConstants.Regexes.ReaderTimeNotZero,
                new() { Timeout = BrowserTestConstants.Timing.ReaderPlaybackAdvanceTimeoutMs });
        await Expect(page.GetByTestId(UiTestIds.Teleprompter.ProgressFill))
            .Not.ToHaveAttributeAsync(
                "style",
                BrowserTestConstants.Regexes.NonZeroWidth,
                new() { Timeout = BrowserTestConstants.Timing.ReaderPlaybackAdvanceTimeoutMs });
        await page.GetByTestId(UiTestIds.Teleprompter.PreviousBlock).ClickAsync();
        await page.GetByTestId(UiTestIds.Teleprompter.NextBlock).ClickAsync();
        await page.GetByTestId(UiTestIds.Teleprompter.PreviousWord).ClickAsync();
        await page.GetByTestId(UiTestIds.Teleprompter.NextWord).ClickAsync();
    }

    private static async Task<bool> VerifySettingsControlsAsync(Microsoft.Playwright.IPage page)
    {
        await ReaderRouteDriver.OpenSettingsAsync(page);
        await page.GetByTestId(UiTestIds.Settings.NavCloud).ClickAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.CloudPanel)).ToBeVisibleAsync();
        await page.GetByTestId(UiTestIds.Settings.NavFiles).ClickAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.FilesPanel)).ToBeVisibleAsync();
        await SettingsCardDriver.EnsureExpandedAsync(page, UiTestIds.Settings.FilesScriptsCard);
        await page.GetByTestId(UiTestIds.Settings.FileAutoSave).ClickAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.FileAutoSave)).ToHaveAttributeAsync(
            BrowserTestConstants.State.EnabledAttribute,
            BrowserTestConstants.State.DisabledValue);
        await page.GetByTestId(UiTestIds.Settings.NavRecording).ClickAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.RecordingPanel)).ToBeVisibleAsync();
        await SettingsCardDriver.EnsureExpandedAsync(page, UiTestIds.Settings.RecordingGeneralCard);
        await Expect(page.GetByTestId(UiTestIds.Settings.RecordingAutoRecord)).ToBeVisibleAsync();

        var readerCameraWasOn = await VerifyCameraSettingsAsync(page);
        await VerifyMicrophoneSettingsAsync(page);
        await VerifyAiAndInfoSettingsAsync(page);

        return readerCameraWasOn;
    }

    private static async Task<bool> VerifyCameraSettingsAsync(Microsoft.Playwright.IPage page)
    {
        await page.GetByTestId(UiTestIds.Settings.NavCameras).ClickAsync();
        var camerasPanel = page.GetByTestId(UiTestIds.Settings.CamerasPanel);
        await Expect(camerasPanel).ToBeVisibleAsync();
        var requestMediaButton = camerasPanel.GetByTestId(UiTestIds.Settings.RequestMedia);
        await requestMediaButton.ScrollIntoViewIfNeededAsync();
        await Expect(requestMediaButton).ToBeVisibleAsync();
        await requestMediaButton.ClickAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.CameraResolution)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.CameraMirrorToggle)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.CameraPreviewCard)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.CameraPreviewVideo)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.CameraPreviewLabel)).ToHaveTextAsync(BrowserTestConstants.Media.PrimaryCameraLabel);
        await Expect(page.GetByTestId(UiTestIds.Settings.CameraDevice(BrowserTestConstants.Media.PrimaryCameraId))).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.CameraDevice(BrowserTestConstants.Media.SecondaryCameraId))).ToBeVisibleAsync();
        await SettingsSelectDriver.SelectByValueAsync(
            page,
            UiTestIds.Settings.CameraResolution,
            BrowserTestConstants.Streaming.ResolutionHd720);

        await ToggleSettingsButtonAsync(page.GetByTestId(UiTestIds.Settings.CameraMirrorToggle));
        await page.GetByTestId(UiTestIds.Settings.CameraDeviceAction(BrowserTestConstants.Media.SecondaryCameraId)).ClickAsync();
        var secondaryPrimaryAction = page.GetByTestId(UiTestIds.Settings.CameraPrimaryAction(BrowserTestConstants.Media.SecondaryCameraId));
        await secondaryPrimaryAction.ClickAsync();
        await Expect(secondaryPrimaryAction).ToBeDisabledAsync();

        return await ToggleReaderCameraAsync(page);
    }

    private static async Task VerifyMicrophoneSettingsAsync(Microsoft.Playwright.IPage page)
    {
        await page.GetByTestId(UiTestIds.Settings.NavMics).ClickAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.MicsPanel)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.MicDevice(BrowserTestConstants.Media.PrimaryMicrophoneId))).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.MicLevel)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.MicPreviewCard)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.MicPreviewMeter)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.NoiseSuppression)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.EchoCancellation)).ToBeVisibleAsync();
        await page.GetByTestId(UiTestIds.Settings.MicLevel).EvaluateAsync(BrowserTestConstants.SettingsFlow.MicLevelInputScript);
        await Expect(page.GetByTestId(UiTestIds.Settings.MicLevel)).ToHaveValueAsync(BrowserTestConstants.SettingsFlow.MicLevelValue);
        await page.GetByTestId(UiTestIds.Settings.NoiseSuppression).ClickAsync();
    }

    private static async Task VerifyAiAndInfoSettingsAsync(Microsoft.Playwright.IPage page)
    {
        var aiNavItem = page.GetByTestId(UiTestIds.Settings.NavAi);
        var appearanceNavItem = page.GetByTestId(UiTestIds.Settings.NavAppearance);
        var aiBeforeSelection = await GetRequiredBoundingBoxAsync(aiNavItem);
        var appearanceBeforeSelection = await GetRequiredBoundingBoxAsync(appearanceNavItem);

        await aiNavItem.ClickAsync();
        await Expect(aiNavItem).ToHaveAttributeAsync(
            BrowserTestConstants.State.ActiveAttribute,
            BrowserTestConstants.State.ActiveValue);
        await Expect(page.GetByTestId(UiTestIds.Settings.AiPanel)).ToBeVisibleAsync();

        var aiAfterSelection = await GetRequiredBoundingBoxAsync(aiNavItem);
        var appearanceAfterSelection = await GetRequiredBoundingBoxAsync(appearanceNavItem);
        await AssertDimensionStable(aiBeforeSelection.Width, aiAfterSelection.Width);
        await AssertDimensionStable(aiBeforeSelection.Height, aiAfterSelection.Height);
        await AssertDimensionStable(appearanceBeforeSelection.Y, appearanceAfterSelection.Y);

        var openAiProvider = page.GetByTestId(UiTestIds.Settings.AiProvider(BrowserTestConstants.SettingsFlow.OpenAiProviderId));
        await openAiProvider.ClickAsync();
        await Expect(openAiProvider).ToHaveAttributeAsync(
            BrowserTestConstants.State.ExpandedAttribute,
            BrowserTestConstants.State.OpenValue);
        await Expect(page.GetByTestId(UiTestIds.Settings.AiProviderSave(BrowserTestConstants.SettingsFlow.OpenAiProviderId))).ToBeVisibleAsync();

        await page.GetByTestId(UiTestIds.Settings.NavAppearance).ClickAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.AppearancePanel)).ToBeVisibleAsync();
        await SettingsCardDriver.EnsureExpandedAsync(page, UiTestIds.Settings.AppearanceThemeCard);
        await page.GetByTestId(UiTestIds.Settings.ThemeOption(BrowserTestConstants.SettingsFlow.LightTheme)).ClickAsync();
        await Expect(page.Locator("html")).ToHaveAttributeAsync(
            BrowserTestConstants.SettingsFlow.HtmlThemeAttribute,
            BrowserTestConstants.SettingsFlow.LightTheme);

        await page.GetByTestId(UiTestIds.Settings.NavFeedback).ClickAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.FeedbackPanel)).ToBeVisibleAsync();
        var feedbackCard = page.GetByTestId(UiTestIds.Settings.FeedbackCard);
        await feedbackCard.ClickAsync();
        await Expect(feedbackCard).ToHaveAttributeAsync(
            BrowserTestConstants.State.ExpandedAttribute,
            BrowserTestConstants.State.OpenValue);
        await page.GetByTestId(UiTestIds.Settings.FeedbackOpen).ClickAsync();
        await Expect(page.GetByTestId(UiTestIds.Feedback.Dialog)).ToBeVisibleAsync();
        await page.GetByTestId(UiTestIds.Feedback.Cancel).ClickAsync();
        await Expect(page.GetByTestId(UiTestIds.Feedback.Dialog)).ToBeHiddenAsync();

        await page.GetByTestId(UiTestIds.Settings.NavAbout).ClickAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.AboutPanel)).ToBeVisibleAsync();
        await SettingsCardDriver.EnsureExpandedAsync(page, UiTestIds.Settings.AboutAppCard);
        await SettingsCardDriver.EnsureExpandedAsync(page, UiTestIds.Settings.AboutCompanyCard);
        await SettingsCardDriver.EnsureExpandedAsync(page, UiTestIds.Settings.AboutResourcesCard);
        await Expect(page.GetByTestId(UiTestIds.Settings.AboutAppCard)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.AboutCompanyCard)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.AboutVersion)).ToHaveTextAsync(BrowserTestConstants.Regexes.SettingsAboutVersion);
        await Expect(page.GetByTestId(UiTestIds.Settings.AboutCompanyWebsite)).ToHaveAttributeAsync("href", AboutLinks.ManagedCodeWebsiteUrl);
        await Expect(page.GetByTestId(UiTestIds.Settings.AboutCompanyGitHub)).ToHaveAttributeAsync("href", AboutLinks.ManagedCodeGitHubUrl);
        await Expect(page.GetByTestId(UiTestIds.Settings.AboutProductGitHub)).ToHaveAttributeAsync("href", AboutLinks.ProductRepositoryUrl);
        await Expect(page.GetByTestId(UiTestIds.Settings.AboutTpsGitHub)).ToHaveAttributeAsync("href", AboutLinks.TpsRepositoryUrl);
        await Expect(page.GetByTestId(UiTestIds.Settings.AboutRepositoryLink)).ToHaveAttributeAsync("href", AboutLinks.ProductRepositoryUrl);
        await Expect(page.GetByTestId(UiTestIds.Settings.AboutReleasesLink)).ToHaveAttributeAsync("href", AboutLinks.ProductReleasesUrl);
        await Expect(page.GetByTestId(UiTestIds.Settings.AboutIssuesLink)).ToHaveAttributeAsync("href", AboutLinks.ProductIssuesUrl);
    }

    private static async Task<bool> ToggleReaderCameraAsync(Microsoft.Playwright.IPage page)
    {
        var readerCameraToggle = page.GetByTestId(UiTestIds.Settings.ReaderCameraToggle);
        await readerCameraToggle.ScrollIntoViewIfNeededAsync();
        await Expect(readerCameraToggle).ToBeVisibleAsync();
        var cameraToggleWasOn = await HasEnabledStateAsync(readerCameraToggle);
        await readerCameraToggle.ClickAsync();

        if (cameraToggleWasOn)
        {
            await Expect(readerCameraToggle).ToHaveAttributeAsync(
                BrowserTestConstants.State.EnabledAttribute,
                BrowserTestConstants.State.DisabledValue);
        }
        else
        {
            await Expect(readerCameraToggle).ToHaveAttributeAsync(
                BrowserTestConstants.State.EnabledAttribute,
                BrowserTestConstants.State.EnabledValue);
        }

        return cameraToggleWasOn;
    }

    private static async Task<bool> HasActiveStateAsync(Microsoft.Playwright.ILocator locator)
    {
        var state = await locator.GetAttributeAsync(BrowserTestConstants.State.ActiveAttribute);
        return string.Equals(state, BrowserTestConstants.Teleprompter.ActiveStateValue, StringComparison.Ordinal);
    }

    private static async Task AssertTeleprompterChromeVisibilityAsync(Microsoft.Playwright.IPage page)
    {
        var controlsOpacity = await GetOpacityAsync(page.GetByTestId(UiTestIds.Teleprompter.Controls));
        var slidersOpacity = await GetOpacityAsync(page.GetByTestId(UiTestIds.Teleprompter.Sliders));
        var edgeInfoOpacity = await GetOpacityAsync(page.GetByTestId(UiTestIds.Teleprompter.EdgeInfo));

        await Assert.That(controlsOpacity >= BrowserTestConstants.TeleprompterFlow.ControlsMinimumOpacity).IsTrue();
        await Assert.That(slidersOpacity >= BrowserTestConstants.TeleprompterFlow.SlidersMinimumOpacity).IsTrue();
        await Assert.That(edgeInfoOpacity >= BrowserTestConstants.TeleprompterFlow.EdgeInfoMinimumOpacity).IsTrue();
    }

    private static async Task AssertDimensionStable(double before, double after)
    {
        await Assert.That(Math.Abs(before - after)).IsBetween(0, BrowserTestConstants.SettingsFlow.NavItemLayoutTolerancePx);
    }

    private static Task<double> GetOpacityAsync(Microsoft.Playwright.ILocator locator) =>
        locator.EvaluateAsync<double>(
            "element => Number.parseFloat(window.getComputedStyle(element).opacity)");

    private static Task SetRangeValueAsync(Microsoft.Playwright.ILocator locator, string value) =>
        locator.EvaluateAsync(
            """
            (element, nextValue) => {
                element.value = nextValue;
                element.dispatchEvent(new Event("input", { bubbles: true }));
                element.dispatchEvent(new Event("change", { bubbles: true }));
            }
            """,
            value);

    private static async Task<LayoutBounds> GetRequiredBoundingBoxAsync(Microsoft.Playwright.ILocator locator) =>
        await locator.EvaluateAsync<LayoutBounds>(
            """
            element => {
                const rect = element.getBoundingClientRect();
                return {
                    x: rect.x,
                    y: rect.y,
                    width: rect.width,
                    height: rect.height
                };
            }
            """);

    private static async Task VerifyTeleprompterCameraAutostartAsync(Microsoft.Playwright.IPage page, bool readerCameraWasOn)
    {
        await ReaderRouteDriver.OpenTeleprompterAsync(page, BrowserTestConstants.Routes.TeleprompterDemo);
        await Expect(page.GetByTestId(UiTestIds.Teleprompter.CameraBackground)).ToHaveAttributeAsync(
            "data-camera-autostart",
            readerCameraWasOn ? new Regex("false") : new Regex("true"));

        if (readerCameraWasOn)
        {
            return;
        }

        await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.ReaderCameraInitDelayMs);
        var hasVideoTrack = await page.GetByTestId(UiTestIds.Teleprompter.CameraBackground).EvaluateAsync<bool>(
            "element => !!element.srcObject && element.srcObject.getVideoTracks().length > 0");
        await Assert.That(hasVideoTrack).IsTrue();
    }

    private static async Task<string> ResolveCameraDeviceIdAsync(Microsoft.Playwright.IPage page) =>
        await page.EvaluateAsync<string>(
            """
            async () => {
                try {
                    const stream = await navigator.mediaDevices.getUserMedia({ video: true, audio: false });
                    stream.getTracks().forEach(track => track.stop());
                } catch {
                }

                const devices = await navigator.mediaDevices.enumerateDevices();
                return devices.find(device => device.kind === 'videoinput')?.deviceId ?? 'default';
            }
            """);

    private static Task SeedStoredTeleprompterSceneAsync(Microsoft.Playwright.IPage page, string cameraDeviceId) =>
        page.EvaluateAsync(
            BrowserTestConstants.TeleprompterFlow.SeedStoredSceneScript,
            new { cameraDeviceId });

    private static async Task ToggleSettingsButtonAsync(Microsoft.Playwright.ILocator locator)
    {
        var wasOn = await HasEnabledStateAsync(locator);
        await locator.ClickAsync();

        if (wasOn)
        {
            await Expect(locator).ToHaveAttributeAsync(
                BrowserTestConstants.State.EnabledAttribute,
                BrowserTestConstants.State.DisabledValue);
        }
        else
        {
            await Expect(locator).ToHaveAttributeAsync(
                BrowserTestConstants.State.EnabledAttribute,
                BrowserTestConstants.State.EnabledValue);
        }
    }

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

    private static string BuildSpeedMultiplierLabel(int baseWpm, int speedWpm) =>
        string.Concat(
            "x",
            (speedWpm / (double)baseWpm).ToString("0.##", CultureInfo.InvariantCulture));

    private static async Task<bool> HasEnabledStateAsync(Microsoft.Playwright.ILocator locator)
    {
        var state = await locator.GetAttributeAsync(BrowserTestConstants.State.EnabledAttribute);
        return string.Equals(state, BrowserTestConstants.State.EnabledValue, StringComparison.Ordinal);
    }
}
