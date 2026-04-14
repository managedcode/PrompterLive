using System.Globalization;
using System.Text.Json;
using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Settings.Models;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class GoLiveShellSessionFlowTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
    public async Task GoLivePage_StartStream_LeavesPersistentWidgetAndReturnsToActiveSession()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);
        var compactViewport = new ResponsiveViewport(
            BrowserTestConstants.AppShellFlow.LiveWidgetViewportName,
            BrowserTestConstants.ResponsiveLayout.IphoneMediumWidth,
            BrowserTestConstants.ResponsiveLayout.IphoneMediumHeight);

        try
        {
            await page.SetViewportSizeAsync(compactViewport.Width, compactViewport.Height);
            await GoLiveFlowTests.SeedGoLiveSceneForReuseAsync(page);
            await GoLiveFlowTests.SeedGoLiveOperationalSettingsAsync(page);
            await StudioRouteDriver.OpenGoLiveAsync(page);
            await page.EvaluateAsync(BrowserTestConstants.GoLive.InstallVdoNinjaHarnessScript);
            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.GoLive.SourceCameraSelect(BrowserTestConstants.GoLive.SecondSourceId)),
                noWaitAfter: true);
            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.GoLive.StartStream),
                noWaitAfter: true);
            await page.WaitForFunctionAsync(
                BrowserTestConstants.GoLive.VdoNinjaHarnessReadyScript,
                null,
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.GoLive.ActiveSourceLabel)).ToContainTextAsync(BrowserTestConstants.GoLive.SideCameraLabel);

            await StudioRouteDriver.NavigateBackToLibraryAsync(page);
            await Expect(page.GetByTestId(UiTestIds.Header.LiveWidget)).ToContainTextAsync(BrowserTestConstants.GoLive.SideCameraLabel);
            await Expect(page.GetByTestId(UiTestIds.Header.LiveWidgetDetail)).ToContainTextAsync(BrowserTestConstants.Media.PrimaryMicrophoneLabel);
            await Expect(page.GetByTestId(UiTestIds.Header.LiveWidgetDetail))
                .Not.ToContainTextAsync(BrowserTestConstants.Scripts.IntroSubtitle);
            await ResponsiveLayoutAssertions.AssertVisibleWithinViewportAsync(
                page,
                page.GetByTestId(UiTestIds.Header.LiveWidget),
                UiTestIds.Header.LiveWidget,
                BrowserTestConstants.AppShellFlow.LiveWidgetScenario,
                compactViewport);
            await ResponsiveLayoutAssertions.AssertVisibleWithinViewportAsync(
                page,
                page.GetByTestId(UiTestIds.Header.LiveWidgetPreview),
                UiTestIds.Header.LiveWidgetPreview,
                BrowserTestConstants.AppShellFlow.LiveWidgetScenario,
                compactViewport);

            var initialTimerLabel = (await page.GetByTestId(UiTestIds.Header.LiveWidgetTimer).TextContentAsync())?.Trim() ?? string.Empty;
            var updatedTimerLabel = await WaitForTextChangeAsync(page, UiTestIds.Header.LiveWidgetTimer, initialTimerLabel);
            await Assert.That(updatedTimerLabel).IsNotEqualTo(initialTimerLabel);

            await CaptureScreenshotAsync(page, BrowserTestConstants.GoLive.WidgetReturnScreenshotPath);
            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Header.LiveWidget),
                noWaitAfter: true);

            await StudioRouteDriver.WaitForGoLiveReadyAsync(page, BrowserTestConstants.Routes.GoLiveDemo);
            await Expect(page.GetByTestId(UiTestIds.GoLive.ActiveSourceLabel)).ToContainTextAsync(BrowserTestConstants.GoLive.SideCameraLabel);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task GoLivePage_StartRecording_MarksHeaderIndicatorAsRecordingOutsideStudioRoute()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await GoLiveFlowTests.SeedGoLiveSceneForReuseAsync(page);
            await GoLiveTestSeedHelper.SeedBrowserLocalRecordingPreferencesAsync(page);
            await StudioRouteDriver.OpenGoLiveAsync(page);
            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.GoLive.StartRecording),
                noWaitAfter: true);

            await page.WaitForFunctionAsync(
                BrowserTestConstants.GoLive.RecordingRuntimeActiveScript,
                BrowserTestConstants.GoLive.RuntimeSessionId,
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await Expect(page.GetByTestId(UiTestIds.Header.GoLive)).ToHaveCountAsync(0);
            await StudioRouteDriver.NavigateToSettingsFromGoLiveAsync(page);
            await Expect(page.GetByTestId(UiTestIds.Header.GoLive))
                .ToHaveAttributeAsync("data-live-state", BrowserTestConstants.GoLive.RecordingStateValue);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task GoLivePage_GenericActiveSession_WidgetReturnsToPlainGoLiveRoute_WithoutInjectingEditorScriptId()
    {
        var pages = await _fixture.NewSharedPagesAsync(
            BrowserTestConstants.GoLive.SharedContextPageCount,
            nameof(GoLivePage_GenericActiveSession_WidgetReturnsToPlainGoLiveRoute_WithoutInjectingEditorScriptId));
        var primaryPage = pages[0];
        var secondaryPage = pages[1];

        try
        {
            await GoLiveFlowTests.SeedGoLiveSceneForReuseAsync(primaryPage);
            await GoLiveTestSeedHelper.SeedBrowserLocalRecordingPreferencesAsync(primaryPage);
            await StudioRouteDriver.OpenGoLiveRouteAsync(primaryPage, BrowserTestConstants.Routes.GoLive);
            await Assert.That(new Uri(primaryPage.Url).PathAndQuery).IsEqualTo(BrowserTestConstants.Routes.GoLive);

            await UiInteractionDriver.ClickAndContinueAsync(
                primaryPage.GetByTestId(UiTestIds.GoLive.StartRecording),
                noWaitAfter: true);
            await primaryPage.WaitForFunctionAsync(
                BrowserTestConstants.GoLive.RecordingRuntimeActiveScript,
                BrowserTestConstants.GoLive.RuntimeSessionId,
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await Assert.That(new Uri(primaryPage.Url).PathAndQuery).IsEqualTo(BrowserTestConstants.Routes.GoLive);

            await EditorRouteDriver.OpenReadyAsync(
                secondaryPage,
                BrowserTestConstants.Routes.EditorDemo,
                "go-live-generic-active-session-secondary-editor");
            await Expect(secondaryPage.GetByTestId(UiTestIds.Header.LiveWidget)).ToBeVisibleAsync();

            await UiInteractionDriver.ClickAndContinueAsync(secondaryPage.GetByTestId(UiTestIds.Header.LiveWidget));

            await StudioRouteDriver.WaitForGoLiveReadyAsync(secondaryPage, BrowserTestConstants.Routes.GoLive);
            await Assert.That(new Uri(secondaryPage.Url).PathAndQuery).IsEqualTo(BrowserTestConstants.Routes.GoLive);
        }
        finally
        {
            await primaryPage.Context.CloseAsync();
        }
    }

    [Test]
    public async Task GoLivePage_RecordingState_PropagatesAcrossSharedTabsAndReturnsToIdleAfterStop()
    {
        UiScenarioArtifacts.ResetScenario(BrowserTestConstants.GoLive.CrossTabIndicatorScenario);

        var pages = await _fixture.NewSharedPagesAsync(
            BrowserTestConstants.GoLive.SharedContextPageCount,
            nameof(GoLivePage_RecordingState_PropagatesAcrossSharedTabsAndReturnsToIdleAfterStop));
        var primaryPage = pages[0];
        var secondaryPage = pages[1];

        try
        {
            await GoLiveFlowTests.SeedGoLiveSceneForReuseAsync(primaryPage);
            await GoLiveTestSeedHelper.SeedBrowserLocalRecordingPreferencesAsync(primaryPage);

            await StudioRouteDriver.OpenSettingsAsync(secondaryPage);
            await Expect(secondaryPage.GetByTestId(UiTestIds.Header.GoLive))
                .ToHaveAttributeAsync("data-live-state", BrowserTestConstants.GoLive.IdleStateValue);

            await StudioRouteDriver.OpenGoLiveAsync(primaryPage);

            await UiInteractionDriver.ClickAndContinueAsync(
                primaryPage.GetByTestId(UiTestIds.GoLive.StartRecording),
                noWaitAfter: true);
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

            await UiInteractionDriver.ClickAndContinueAsync(
                primaryPage.GetByTestId(UiTestIds.GoLive.StartRecording),
                noWaitAfter: true);
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

    [Test]
    public async Task GoLivePage_StartRecording_UsesSelectedProgramSourceAndShowsRecordingMetadata()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await GoLiveFlowTests.SeedGoLiveSceneForReuseAsync(page);
            await GoLiveTestSeedHelper.SeedBrowserLocalRecordingPreferencesAsync(page);
            await StudioRouteDriver.OpenGoLiveAsync(page);
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.GoLive.SourceCameraSelect(BrowserTestConstants.GoLive.SecondSourceId)),
                noWaitAfter: true);

            var programVideo = page.GetByTestId(UiTestIds.GoLive.ProgramVideo);
            var programVideoHandle = await programVideo.ElementHandleAsync();
            await Assert.That(programVideoHandle).IsNotNull();
            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.ElementUsesVideoDeviceScript,
                new object[] { UiTestIds.GoLive.ProgramVideo, BrowserTestConstants.Media.SecondaryCameraId },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await page.WaitForFunctionAsync(
                BrowserTestConstants.GoLive.PreviewReadyScript,
                programVideoHandle,
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.ElementHasVisibleVideoScript,
                new object[] { UiTestIds.GoLive.ProgramVideo, BrowserTestConstants.Media.MinimumVisiblePixelCount },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.GoLive.StartRecording),
                noWaitAfter: true);

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

            await Assert.That(programState.GetProperty("primarySourceId").GetString()).IsEqualTo(BrowserTestConstants.GoLive.SecondSourceId);
            await Assert.That(programState.GetProperty("videoSourceCount").GetInt32()).IsEqualTo(1);
            await Assert.That(programState.GetProperty("width").GetInt32() > 0).IsTrue();
            await Assert.That(programState.GetProperty("height").GetInt32() > 0).IsTrue();
            await Assert.That(recordingState.GetProperty("active").GetBoolean()).IsTrue();
            await Assert.That(string.IsNullOrWhiteSpace(recordingState.GetProperty("fileName").GetString())).IsFalse();
            await Assert.That(string.IsNullOrWhiteSpace(recordingState.GetProperty("mimeType").GetString())).IsFalse();
            await Assert.That(recordingState.GetProperty("videoBitrateKbps").GetInt32() > 0).IsTrue();

            var bitrateMetric = page.GetByTestId(UiTestIds.GoLive.StatusMetric(GoLiveMetricIds.StatusBitrate));
            var outputMetric = page.GetByTestId(UiTestIds.GoLive.StatusMetric(GoLiveMetricIds.StatusOutput));
            var recordingMetric = page.GetByTestId(UiTestIds.GoLive.RuntimeMetric(GoLiveMetricIds.RuntimeRecording));
            var runtimeMetric = page.GetByTestId(UiTestIds.GoLive.RuntimeMetric(GoLiveMetricIds.RuntimeEngine));

            await Expect(bitrateMetric).ToContainTextAsync(
                SettingsPagePreferences.Default.RecordingVideoBitrateKbps.ToString(CultureInfo.InvariantCulture));
            await Expect(outputMetric).ToContainTextAsync(BrowserTestConstants.GoLive.OutputWidthLabel);

            var recordingMetricText = await recordingMetric.TextContentAsync();
            await Assert.That(string.IsNullOrWhiteSpace(recordingMetricText)).IsFalse();
            await Assert.That(recordingMetricText).Contains(BrowserTestConstants.GoLive.ByteSuffix);

            var runtimeMetricText = await runtimeMetric.TextContentAsync();
            await Assert.That(string.IsNullOrWhiteSpace(runtimeMetricText)).IsFalse();
            await Assert.That(runtimeMetricText).Contains(recordingState.GetProperty("mimeType").GetString()!.Contains(BrowserTestConstants.GoLive.Mp4MimeFragment, StringComparison.OrdinalIgnoreCase)
                    ? BrowserTestConstants.GoLive.Mp4ContainerLabel
                    : BrowserTestConstants.GoLive.WebmContainerLabel);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task GoLivePage_StartRecording_FilePickerSave_ProducesDecodableProgramVideoAndAudio()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await page.AddInitScriptAsync(scriptPath: GetRecordingFileHarnessScriptPath());
            await GoLiveFlowTests.SeedGoLiveSceneForReuseAsync(page);
            await GoLiveFlowTests.SeedGoLivePrimaryMicrophoneAsync(page);
            await GoLiveFlowTests.SeedRecordingPreferencesAsync(
                page,
                SettingsPagePreferences.Default with
                {
                    HasSeenOnboarding = true,
                    RecordingFolder = RecordingPreferenceCatalog.LocationLabels.LocalFile
                });
            await StudioRouteDriver.OpenGoLiveAsync(page);
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.GoLive.SourceCameraSelect(BrowserTestConstants.GoLive.SecondSourceId)),
                noWaitAfter: true);
            await WaitForProgramVideoDeviceReadyAsync(page, BrowserTestConstants.Media.SecondaryCameraId);
            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.GoLive.StartRecording),
                noWaitAfter: true);

            await page.WaitForFunctionAsync(
                BrowserTestConstants.GoLive.RecordingRuntimeMetadataReadyScript,
                BrowserTestConstants.GoLive.RuntimeSessionId,
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.WaitForFunctionAsync(
                BrowserTestConstants.GoLive.RecordingRuntimeAudioLevelsReadyScript,
                new object[] { BrowserTestConstants.GoLive.RuntimeSessionId, BrowserTestConstants.GoLive.MinimumActiveLevelPercent },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.GoLive.StartRecording),
                noWaitAfter: true);
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

            await Assert.That(savedRecording.GetProperty("pickerCallCount").GetInt32() >= 1).IsTrue();
            await Assert.That(savedRecording.GetProperty("sizeBytes").GetInt64() > 0).IsTrue();
            await Assert.That(savedAnalysis.GetProperty("width").GetInt32() > 0).IsTrue();
            await Assert.That(savedAnalysis.GetProperty("height").GetInt32() > 0).IsTrue();
            await Assert.That(savedAnalysis.GetProperty("hasAudioTrack").GetBoolean()).IsTrue();
            await Assert.That(savedAnalysis.GetProperty("hasAudibleAudio").GetBoolean()).IsTrue();
            await Assert.That(savedAnalysis.GetProperty("hasVisibleVideo").GetBoolean()).IsTrue();
            await Assert.That(savedAnalysis.GetProperty("nonBlackPixelCount").GetInt32() >= BrowserTestConstants.Media.MinimumVisiblePixelCount).IsTrue();
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task GoLivePage_AudioTab_ShowsLiveMicrophoneProgramAndRecordingLevels()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await GoLiveFlowTests.SeedGoLiveSceneForReuseAsync(page);
            await GoLiveFlowTests.SeedGoLivePrimaryMicrophoneAsync(page);
            await GoLiveTestSeedHelper.SeedBrowserLocalRecordingPreferencesAsync(page);
            await StudioRouteDriver.OpenGoLiveAsync(page);
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();

            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.GoLive.AudioTab));

            var micChannel = page.GetByTestId(UiTestIds.GoLive.AudioChannel(BrowserTestConstants.GoLive.MicChannelId));
            var programChannel = page.GetByTestId(UiTestIds.GoLive.AudioChannel(BrowserTestConstants.GoLive.ProgramChannelId));
            var recordingChannel = page.GetByTestId(UiTestIds.GoLive.AudioChannel(BrowserTestConstants.GoLive.RecordingChannelId));

            await Expect(micChannel)
                .ToHaveAttributeAsync(BrowserTestConstants.GoLive.LiveStateAttributeName, BrowserTestConstants.GoLive.ActiveStateValue);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.GoLive.StartRecording),
                noWaitAfter: true);
            await page.WaitForFunctionAsync(
                BrowserTestConstants.GoLive.RecordingRuntimeAudioLevelsReadyScript,
                new object[] { BrowserTestConstants.GoLive.RuntimeSessionId, BrowserTestConstants.GoLive.MinimumActiveLevelPercent },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            var runtimeState = await page.EvaluateAsync<JsonElement>(
                BrowserTestConstants.GoLive.GetRuntimeStateScript,
                BrowserTestConstants.GoLive.RuntimeSessionId);

            var audioState = runtimeState.GetProperty("audio");
            await Assert.That(audioState.GetProperty("programLevelPercent").GetInt32() >= BrowserTestConstants.GoLive.MinimumActiveLevelPercent).IsTrue();
            await Assert.That(audioState.GetProperty("recordingLevelPercent").GetInt32() >= BrowserTestConstants.GoLive.MinimumActiveLevelPercent).IsTrue();

            await Expect(programChannel)
                .ToHaveAttributeAsync(BrowserTestConstants.GoLive.LiveStateAttributeName, BrowserTestConstants.GoLive.ActiveStateValue);
            await Expect(recordingChannel)
                .ToHaveAttributeAsync(BrowserTestConstants.GoLive.LiveStateAttributeName, BrowserTestConstants.GoLive.ActiveStateValue);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.GoLive.StartRecording),
                noWaitAfter: true);
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

    private static async Task<string> WaitForTextChangeAsync(IPage page, string testId, string initialText)
    {
        for (var attempt = 0; attempt < BrowserTestConstants.AppShellFlow.LiveWidgetTimerPollAttempts; attempt++)
        {
            await page.WaitForTimeoutAsync(BrowserTestConstants.AppShellFlow.LiveWidgetTimerPollDelayMs);
            var currentText = (await page.GetByTestId(testId).TextContentAsync())?.Trim() ?? string.Empty;
            if (!string.Equals(initialText, currentText, StringComparison.Ordinal))
            {
                return currentText;
            }
        }

        return initialText;
    }

    private static async Task WaitForProgramVideoDeviceReadyAsync(IPage page, string deviceId)
    {
        var programVideo = page.GetByTestId(UiTestIds.GoLive.ProgramVideo);
        var programVideoHandle = await programVideo.ElementHandleAsync();
        await Assert.That(programVideoHandle).IsNotNull();
        await page.WaitForFunctionAsync(
            BrowserTestConstants.Media.ElementUsesVideoDeviceScript,
            new object[] { UiTestIds.GoLive.ProgramVideo, deviceId },
            new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
        await page.WaitForFunctionAsync(
            BrowserTestConstants.GoLive.PreviewReadyScript,
            programVideoHandle,
            new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
        await page.WaitForFunctionAsync(
            BrowserTestConstants.Media.ElementHasVisibleVideoScript,
            new object[] { UiTestIds.GoLive.ProgramVideo, BrowserTestConstants.Media.MinimumVisiblePixelCount },
            new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
    }

    private static string GetRecordingFileHarnessScriptPath() =>
        UiTestAssetPaths.GetRecordingFileHarnessScriptPath();
}
