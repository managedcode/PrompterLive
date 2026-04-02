using System.Globalization;
using System.Text.Json;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Settings.Models;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.App.UITests;

public sealed class GoLiveShellSessionFlowTests(StandaloneAppFixture fixture) : IClassFixture<StandaloneAppFixture>
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
    public async Task GoLivePage_StartStream_LeavesPersistentWidgetAndReturnsToActiveSession()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await GoLiveFlowTests.SeedGoLiveSceneForReuseAsync(page);
            await page.EvaluateAsync(BrowserTestConstants.GoLive.EnableObsStudioScript);
            await page.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await page.GetByTestId(UiTestIds.GoLive.SourceCameraSelect(BrowserTestConstants.GoLive.SecondSourceId)).ClickAsync();
            await page.GetByTestId(UiTestIds.GoLive.StartStream).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.GoLive.ActiveSourceLabel)).ToContainTextAsync(BrowserTestConstants.GoLive.SideCameraLabel);

            await page.GetByTestId(UiTestIds.GoLive.Back).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.Library));
            await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.LiveWidget)).ToContainTextAsync(BrowserTestConstants.GoLive.SideCameraLabel);

            await CaptureScreenshotAsync(page, BrowserTestConstants.GoLive.WidgetReturnScreenshotPath);
            await page.GetByTestId(UiTestIds.Header.LiveWidget).ClickAsync();

            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.GoLiveDemo));
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.GoLive.ActiveSourceLabel)).ToContainTextAsync(BrowserTestConstants.GoLive.SideCameraLabel);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task GoLivePage_StartRecording_MarksHeaderIndicatorAsRecordingOutsideStudioRoute()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await GoLiveFlowTests.SeedGoLiveSceneForReuseAsync(page);
            await page.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await page.GetByTestId(UiTestIds.GoLive.StartRecording).ClickAsync();

            await page.WaitForFunctionAsync(
                BrowserTestConstants.GoLive.RecordingRuntimeActiveScript,
                BrowserTestConstants.GoLive.RuntimeSessionId,
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await Expect(page.GetByTestId(UiTestIds.Header.GoLive)).ToHaveCountAsync(0);
            await page.GetByTestId(UiTestIds.GoLive.OpenSettings).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.Settings));
            await Expect(page.GetByTestId(UiTestIds.Header.GoLive))
                .ToHaveAttributeAsync("data-live-state", BrowserTestConstants.GoLive.RecordingStateValue);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task GoLivePage_RecordingState_PropagatesAcrossSharedTabsAndReturnsToIdleAfterStop()
    {
        UiScenarioArtifacts.ResetScenario(BrowserTestConstants.GoLive.CrossTabIndicatorScenario);

        var pages = await _fixture.NewSharedPagesAsync(BrowserTestConstants.GoLive.SharedContextPageCount);
        var primaryPage = pages[0];
        var secondaryPage = pages[1];

        try
        {
            await GoLiveFlowTests.SeedGoLiveSceneForReuseAsync(primaryPage);

            await secondaryPage.GotoAsync(BrowserTestConstants.Routes.Settings);
            await Expect(secondaryPage.GetByTestId(UiTestIds.Settings.Page)).ToBeVisibleAsync();
            await Expect(secondaryPage.GetByTestId(UiTestIds.Header.GoLive))
                .ToHaveAttributeAsync("data-live-state", BrowserTestConstants.GoLive.IdleStateValue);

            await primaryPage.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await Expect(primaryPage.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();

            await primaryPage.GetByTestId(UiTestIds.GoLive.StartRecording).ClickAsync();
            await primaryPage.WaitForFunctionAsync(
                BrowserTestConstants.GoLive.RecordingRuntimeActiveScript,
                BrowserTestConstants.GoLive.RuntimeSessionId,
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await Expect(secondaryPage.GetByTestId(UiTestIds.Header.GoLive))
                .ToHaveAttributeAsync("data-live-state", BrowserTestConstants.GoLive.RecordingStateValue);

            await UiScenarioArtifacts.CapturePageAsync(
                secondaryPage,
                BrowserTestConstants.GoLive.CrossTabIndicatorScenario,
                BrowserTestConstants.GoLive.CrossTabIndicatorActiveStep);

            await primaryPage.GetByTestId(UiTestIds.GoLive.StartRecording).ClickAsync();
            await primaryPage.WaitForFunctionAsync(
                BrowserTestConstants.GoLive.RecordingRuntimeInactiveScript,
                BrowserTestConstants.GoLive.RuntimeSessionId,
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await Expect(secondaryPage.GetByTestId(UiTestIds.Header.GoLive))
                .ToHaveAttributeAsync("data-live-state", BrowserTestConstants.GoLive.IdleStateValue);

            await UiScenarioArtifacts.CapturePageAsync(
                secondaryPage,
                BrowserTestConstants.GoLive.CrossTabIndicatorScenario,
                BrowserTestConstants.GoLive.CrossTabIndicatorIdleStep);
        }
        finally
        {
            await primaryPage.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task GoLivePage_StartRecording_UsesSelectedProgramSourceAndShowsRecordingMetadata()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await GoLiveFlowTests.SeedGoLiveSceneForReuseAsync(page);
            await page.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.GoLive.SourceCameraSelect(BrowserTestConstants.GoLive.SecondSourceId)).ClickAsync();
            await page.GetByTestId(UiTestIds.GoLive.StartRecording).ClickAsync();

            await page.WaitForFunctionAsync(
                BrowserTestConstants.GoLive.RecordingRuntimeUsesProgramSourceScript,
                new object[] { BrowserTestConstants.GoLive.RuntimeSessionId, BrowserTestConstants.GoLive.SecondSourceId },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.WaitForFunctionAsync(
                BrowserTestConstants.GoLive.RecordingRuntimeMetadataReadyScript,
                BrowserTestConstants.GoLive.RuntimeSessionId,
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            var runtimeState = await page.EvaluateAsync<JsonElement>(
                BrowserTestConstants.GoLive.GetRuntimeStateScript,
                BrowserTestConstants.GoLive.RuntimeSessionId);

            var programState = runtimeState.GetProperty("program");
            var recordingState = runtimeState.GetProperty("recording");

            Assert.Equal(BrowserTestConstants.GoLive.SecondSourceId, programState.GetProperty("primarySourceId").GetString());
            Assert.True(programState.GetProperty("width").GetInt32() > 0);
            Assert.True(programState.GetProperty("height").GetInt32() > 0);
            Assert.False(string.IsNullOrWhiteSpace(recordingState.GetProperty("fileName").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(recordingState.GetProperty("mimeType").GetString()));
            Assert.True(recordingState.GetProperty("sizeBytes").GetInt64() > 0);
            Assert.True(recordingState.GetProperty("videoBitrateKbps").GetInt32() > 0);

            var bitrateMetric = page.GetByTestId(UiTestIds.GoLive.StatusMetric(GoLiveMetricIds.StatusBitrate));
            var outputMetric = page.GetByTestId(UiTestIds.GoLive.StatusMetric(GoLiveMetricIds.StatusOutput));
            var recordingMetric = page.GetByTestId(UiTestIds.GoLive.RuntimeMetric(GoLiveMetricIds.RuntimeRecording));
            var runtimeMetric = page.GetByTestId(UiTestIds.GoLive.RuntimeMetric(GoLiveMetricIds.RuntimeEngine));

            await Expect(bitrateMetric).ToContainTextAsync(
                SettingsPagePreferences.Default.RecordingVideoBitrateKbps.ToString(CultureInfo.InvariantCulture));
            await Expect(outputMetric).ToContainTextAsync(BrowserTestConstants.GoLive.OutputWidthLabel);

            var recordingMetricText = await recordingMetric.TextContentAsync();
            Assert.False(string.IsNullOrWhiteSpace(recordingMetricText));
            Assert.Contains(BrowserTestConstants.GoLive.ByteSuffix, recordingMetricText, StringComparison.Ordinal);

            var runtimeMetricText = await runtimeMetric.TextContentAsync();
            Assert.False(string.IsNullOrWhiteSpace(runtimeMetricText));
            Assert.Contains(
                recordingState.GetProperty("mimeType").GetString()!.Contains(BrowserTestConstants.GoLive.Mp4MimeFragment, StringComparison.OrdinalIgnoreCase)
                    ? BrowserTestConstants.GoLive.Mp4ContainerLabel
                    : BrowserTestConstants.GoLive.WebmContainerLabel,
                runtimeMetricText,
                StringComparison.Ordinal);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task GoLivePage_StartRecording_FilePickerSave_ProducesDecodableProgramVideoAndAudio()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.AddInitScriptAsync(scriptPath: GetRecordingFileHarnessScriptPath());
            await GoLiveFlowTests.SeedGoLiveSceneForReuseAsync(page);
            await GoLiveFlowTests.SeedGoLivePrimaryMicrophoneAsync(page);
            await page.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.GoLive.SourceCameraSelect(BrowserTestConstants.GoLive.SecondSourceId)).ClickAsync();
            await page.GetByTestId(UiTestIds.GoLive.StartRecording).ClickAsync();

            await page.WaitForFunctionAsync(
                BrowserTestConstants.GoLive.RecordingRuntimeMetadataReadyScript,
                BrowserTestConstants.GoLive.RuntimeSessionId,
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.GetByTestId(UiTestIds.GoLive.StartRecording).ClickAsync();
            await page.WaitForFunctionAsync(
                BrowserTestConstants.GoLive.RecordingRuntimeInactiveScript,
                BrowserTestConstants.GoLive.RuntimeSessionId,
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.SavedRecordingReadyScript,
                null,
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            var savedRecording = await page.EvaluateAsync<JsonElement>(BrowserTestConstants.Media.GetSavedRecordingStateScript);
            var savedAnalysis = await page.EvaluateAsync<JsonElement>(BrowserTestConstants.Media.AnalyzeSavedRecordingScript);

            Assert.True(savedRecording.GetProperty("pickerCallCount").GetInt32() >= 1);
            Assert.True(savedRecording.GetProperty("sizeBytes").GetInt64() > 0);
            Assert.True(savedAnalysis.GetProperty("width").GetInt32() > 0);
            Assert.True(savedAnalysis.GetProperty("height").GetInt32() > 0);
            Assert.True(savedAnalysis.GetProperty("hasAudioTrack").GetBoolean());
            Assert.True(savedAnalysis.GetProperty("hasAudibleAudio").GetBoolean());
            Assert.True(savedAnalysis.GetProperty("hasVisibleVideo").GetBoolean());
            Assert.True(savedAnalysis.GetProperty("nonBlackPixelCount").GetInt32() >= BrowserTestConstants.Media.MinimumVisiblePixelCount);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task GoLivePage_AudioTab_ShowsLiveMicrophoneProgramAndRecordingLevels()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await GoLiveFlowTests.SeedGoLiveSceneForReuseAsync(page);
            await page.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.GoLive.AudioTab).ClickAsync();

            var micChannel = page.GetByTestId(UiTestIds.GoLive.AudioChannel(BrowserTestConstants.GoLive.MicChannelId));
            var programChannel = page.GetByTestId(UiTestIds.GoLive.AudioChannel(BrowserTestConstants.GoLive.ProgramChannelId));
            var recordingChannel = page.GetByTestId(UiTestIds.GoLive.AudioChannel(BrowserTestConstants.GoLive.RecordingChannelId));

            await Expect(micChannel)
                .ToHaveAttributeAsync(BrowserTestConstants.GoLive.LiveStateAttributeName, BrowserTestConstants.GoLive.ActiveStateValue);

            await page.GetByTestId(UiTestIds.GoLive.StartRecording).ClickAsync();
            await page.WaitForFunctionAsync(
                BrowserTestConstants.GoLive.RecordingRuntimeAudioLevelsReadyScript,
                new object[] { BrowserTestConstants.GoLive.RuntimeSessionId, BrowserTestConstants.GoLive.MinimumActiveLevelPercent },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            var runtimeState = await page.EvaluateAsync<JsonElement>(
                BrowserTestConstants.GoLive.GetRuntimeStateScript,
                BrowserTestConstants.GoLive.RuntimeSessionId);

            var audioState = runtimeState.GetProperty("audio");
            Assert.True(audioState.GetProperty("programLevelPercent").GetInt32() >= BrowserTestConstants.GoLive.MinimumActiveLevelPercent);
            Assert.True(audioState.GetProperty("recordingLevelPercent").GetInt32() >= BrowserTestConstants.GoLive.MinimumActiveLevelPercent);

            await Expect(programChannel)
                .ToHaveAttributeAsync(BrowserTestConstants.GoLive.LiveStateAttributeName, BrowserTestConstants.GoLive.ActiveStateValue);
            await Expect(recordingChannel)
                .ToHaveAttributeAsync(BrowserTestConstants.GoLive.LiveStateAttributeName, BrowserTestConstants.GoLive.ActiveStateValue);

            await page.GetByTestId(UiTestIds.GoLive.StartRecording).ClickAsync();
            await page.WaitForFunctionAsync(
                BrowserTestConstants.GoLive.RecordingRuntimeInactiveScript,
                BrowserTestConstants.GoLive.RuntimeSessionId,
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await Expect(programChannel)
                .ToHaveAttributeAsync(BrowserTestConstants.GoLive.LiveStateAttributeName, BrowserTestConstants.GoLive.IdleStateValue);
            await Expect(programChannel)
                .ToHaveAttributeAsync(BrowserTestConstants.GoLive.LiveLevelAttributeName, BrowserTestConstants.GoLive.ZeroLevelValue);
            await Expect(recordingChannel)
                .ToHaveAttributeAsync(BrowserTestConstants.GoLive.LiveStateAttributeName, BrowserTestConstants.GoLive.IdleStateValue);
            await Expect(recordingChannel)
                .ToHaveAttributeAsync(BrowserTestConstants.GoLive.LiveLevelAttributeName, BrowserTestConstants.GoLive.ZeroLevelValue);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static async Task CaptureScreenshotAsync(Microsoft.Playwright.IPage page, string relativePath)
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await page.ScreenshotAsync(new() { Path = fullPath, FullPage = true });
    }

    private static string GetRecordingFileHarnessScriptPath() =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../tests/PrompterOne.App.UITests/Media/recording-file-harness.js"));
}
