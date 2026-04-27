using System.Text.Json;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class TeleprompterRecordingFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture)
{
    [Test]
    public Task TeleprompterReader_RecordingControls_RecordVideoAndAudioWithLiveMeter() =>
        RunPageAsync(async page =>
        {
            await page.AddInitScriptAsync(scriptPath: UiTestAssetPaths.GetRecordingFileHarnessScriptPath());
            await ReaderRouteDriver.OpenTeleprompterAsync(page, BrowserTestConstants.Routes.TeleprompterDemo);
            await page.EvaluateAsync(BrowserTestConstants.Media.EnableSyntheticRecordingEncoderScript);

            await Expect(page.GetByTestId(UiTestIds.Teleprompter.RecordingPanel)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.CameraToggle)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.RecordingModeSelect)).ToHaveAttributeAsync("value", "video-audio");
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.RecordingCameraSelect)).ToHaveAttributeAsync("value", BrowserTestConstants.Media.PrimaryCameraId);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.RecordingMicrophoneSelect)).ToHaveAttributeAsync("value", BrowserTestConstants.Media.PrimaryMicrophoneId);

            await page.GetByTestId(UiTestIds.Teleprompter.RecordingToggle).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.RecordingToggle)).ToHaveAttributeAsync(
                BrowserTestConstants.State.ActiveAttribute,
                BrowserTestConstants.State.ActiveValue);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.RecordingStatus)).ToContainTextAsync("Recording");
            await page.WaitForFunctionAsync(
                "([testId, minimumLevel]) => Number(document.querySelector(`[data-test=\"${testId}\"]`)?.dataset.level ?? '0') >= minimumLevel",
                new object[] { UiTestIds.Teleprompter.RecordingLevel, BrowserTestConstants.Media.LiveLevelThreshold },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await page.WaitForTimeoutAsync(900);

            await page.GetByTestId(UiTestIds.Teleprompter.RecordingToggle).ClickAsync();
            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.SavedRecordingReadyScript,
                null,
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            var savedRecording = await page.EvaluateAsync<JsonElement>(BrowserTestConstants.Media.GetSavedRecordingStateScript);
            await Assert.That(savedRecording.GetProperty("hasBlob").GetBoolean()).IsTrue();
            await Assert.That(savedRecording.GetProperty("sizeBytes").GetInt64() > 0).IsTrue();
            await Assert.That(savedRecording.GetProperty("mimeType").GetString()).Contains("video/");
        });

    [Test]
    public Task TeleprompterReader_AudioOnlyRecording_DoesNotRequireBackgroundCamera() =>
        RunPageAsync(async page =>
        {
            await page.AddInitScriptAsync(scriptPath: UiTestAssetPaths.GetRecordingFileHarnessScriptPath());
            await ReaderRouteDriver.OpenTeleprompterAsync(page, BrowserTestConstants.Routes.TeleprompterDemo);
            await page.EvaluateAsync(BrowserTestConstants.Media.EnableSyntheticRecordingEncoderScript);
            await TeleprompterCameraDriver.EnsureDisabledAsync(page);
            await page.EvaluateAsync(BrowserTestConstants.Media.ClearRequestLogScript);

            await SettingsSelectDriver.SelectByValueAsync(
                page,
                UiTestIds.Teleprompter.RecordingModeSelect,
                "audio");
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.RecordingCameraSelect)).ToHaveCountAsync(0);

            await page.GetByTestId(UiTestIds.Teleprompter.RecordingToggle).ClickAsync();
            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.HasAudioOnlyRequestScript,
                new object[] { BrowserTestConstants.Media.PrimaryMicrophoneId },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await Assert.That(await page.EvaluateAsync<int>(BrowserTestConstants.Media.GetActiveVideoTrackCountScript)).IsEqualTo(0);
            await page.WaitForTimeoutAsync(1500);

            await page.GetByTestId(UiTestIds.Teleprompter.RecordingToggle).ClickAsync();
            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.SavedRecordingReadyScript,
                null,
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
        });
}
