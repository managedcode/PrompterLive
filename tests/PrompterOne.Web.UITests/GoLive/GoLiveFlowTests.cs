using System.Text.Json;
using Microsoft.Playwright;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class GoLiveFlowTests(StandaloneAppFixture fixture)
{
    private const int LayoutViewportHeight = 768;
    private const int LayoutViewportWidth = 1366;
    private const double MaxPreviewRailWidth = 360d;
    private const double MaxProgramAspectRatio = 1.95d;
    private const double MaxSourcesRailWidth = 260d;
    private const double MinPreviewRailWidth = 260d;
    private const double MinProgramAspectRatio = 1.55d;
    private const double MinSourcesRailWidth = 170d;
    private const string GoLiveLayoutParityScenario = "go-live-layout-parity";
    private const string GoLiveLayoutParityStep = "01-design-shell";
    private const string IncludeActionLabel = "Include";
    private const string RemoveActionLabel = "Remove";
    private const double TimerCenterTolerancePixels = 48d;

    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
    public async Task GoLivePage_PersistsTransportAndDistributionTargetsWithoutRenderingLocalOutputCards()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.SetViewportSizeAsync(LayoutViewportWidth, LayoutViewportHeight);
            await SeedGoLiveSceneForReuseAsync(page);
            await SeedGoLiveOperationalSettingsAsync(page);
            await page.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.GoLive.ProgramCard)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.GoLive.SourcesCard)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.GoLive.VdoToggle).ClickAsync();
            await page.GetByTestId(UiTestIds.GoLive.VdoToggle).ClickAsync();
            await page.GetByTestId(UiTestIds.GoLive.YoutubeToggle).ClickAsync();
            await page.GetByTestId(UiTestIds.GoLive.YoutubeToggle).ClickAsync();

            await page.WaitForFunctionAsync(
                BrowserTestConstants.GoLive.PersistedToggleTargetsScript,
                BrowserTestConstants.GoLive.StoredStudioSettingsKey,
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.ReloadAsync(new() { WaitUntil = Microsoft.Playwright.WaitUntilState.NetworkIdle });

            await Expect(page.GetByTestId(UiTestIds.GoLive.VdoToggle)).ToHaveClassAsync(BrowserTestConstants.Regexes.ToggleOnClass);
            await Expect(page.GetByTestId(UiTestIds.GoLive.YoutubeToggle)).ToHaveClassAsync(BrowserTestConstants.Regexes.ToggleOnClass);
            await Expect(page.GetByTestId(UiTestIds.GoLive.RecordingToggle)).ToHaveCountAsync(0);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task GoLivePage_TogglesSceneCameraMembershipAndRoutesTopLeftHomeControlToLibrary()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await SeedGoLiveSceneForReuseAsync(page);
            await page.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();

            var sourceButton = page.GetByTestId(UiTestIds.GoLive.SourceCameraAction(BrowserTestConstants.Media.PrimaryCameraId));
            await Expect(sourceButton).ToContainTextAsync(RemoveActionLabel);
            await sourceButton.ClickAsync();
            await Expect(sourceButton).ToContainTextAsync(IncludeActionLabel);
            await sourceButton.ClickAsync();
            await Expect(sourceButton).ToContainTextAsync(RemoveActionLabel);

            await page.GetByTestId(UiTestIds.GoLive.Back).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.Library));
            await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task GoLivePage_BackControl_ReturnsToPreviousInAppScreen_WhenEnteredFromSettings()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await SeedGoLiveSceneForReuseAsync(page);
            await page.GotoAsync(BrowserTestConstants.Routes.Settings);
            await Expect(page.GetByTestId(UiTestIds.Settings.Page)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.Header.GoLive).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(AppRoutes.GoLive));
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.GoLive.Back).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.Settings));
            await Expect(page.GetByTestId(UiTestIds.Settings.Page)).ToBeVisibleAsync();
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task GoLivePage_ShowsLiveCameraPreviewForProgramFeed()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await SeedGoLiveSceneForReuseAsync(page);
            await page.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();

            var previewCard = page.GetByTestId(UiTestIds.GoLive.PreviewCard);
            var previewVideo = page.GetByTestId(UiTestIds.GoLive.PreviewVideo);

            await Expect(previewCard).ToBeVisibleAsync();
            await Expect(previewVideo).ToBeVisibleAsync();

            var previewHandle = await previewVideo.ElementHandleAsync();
            await Assert.That(previewHandle).IsNotNull();

            await page.WaitForFunctionAsync(
                BrowserTestConstants.GoLive.PreviewReadyScript,
                previewHandle,
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await Expect(page.GetByTestId(UiTestIds.GoLive.PreviewSourceLabel)).ToBeVisibleAsync();
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task GoLivePage_HidesPreviewRailWhenRightPanelIsCollapsed()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await SeedGoLiveSceneForReuseAsync(page);
            await page.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.GoLive.PreviewRail)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.GoLive.RightPanelToggle).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.GoLive.PreviewRail)).ToHaveCountAsync(0);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task GoLivePage_UsesNewDesignStudioGridAndTopbarLayout()
    {
        UiScenarioArtifacts.ResetScenario(GoLiveLayoutParityScenario);
        var page = await _fixture.NewPageAsync();

        try
        {
            await SeedGoLiveSceneForReuseAsync(page);
            await page.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();

            var sessionBar = page.GetByTestId(UiTestIds.GoLive.SessionBar);
            var sourceRail = page.GetByTestId(UiTestIds.GoLive.SourceRail);
            var programCard = page.GetByTestId(UiTestIds.GoLive.ProgramCard);
            var programVideo = page.GetByTestId(UiTestIds.GoLive.ProgramVideo);
            var previewRail = page.GetByTestId(UiTestIds.GoLive.PreviewRail);
            var settingsButton = page.GetByTestId(UiTestIds.GoLive.OpenSettings);
            var streamButton = page.GetByTestId(UiTestIds.GoLive.StartStream);
            var timer = page.GetByTestId(UiTestIds.GoLive.SessionTimer);

            await Expect(sourceRail).ToBeVisibleAsync();
            await Expect(programCard).ToBeVisibleAsync();
            await Expect(programVideo).ToBeVisibleAsync();
            await Expect(previewRail).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.GoLive)).ToHaveCountAsync(0);

            var sessionBarBox = await GetRequiredBoxAsync(sessionBar);
            var sourceRailBox = await GetRequiredBoxAsync(sourceRail);
            var programCardBox = await GetRequiredBoxAsync(programCard);
            var programVideoBox = await GetRequiredBoxAsync(programVideo);
            var previewRailBox = await GetRequiredBoxAsync(previewRail);
            var settingsButtonBox = await GetRequiredBoxAsync(settingsButton);
            var streamButtonBox = await GetRequiredBoxAsync(streamButton);
            var timerBox = await GetRequiredBoxAsync(timer);

            await Assert.That(sourceRailBox.Width).IsBetween(MinSourcesRailWidth, MaxSourcesRailWidth);
            await Assert.That(previewRailBox.Width).IsBetween(MinPreviewRailWidth, MaxPreviewRailWidth);
            await Assert.That(programCardBox.Width > sourceRailBox.Width).IsTrue();
            await Assert.That(programCardBox.Width > previewRailBox.Width).IsTrue();

            var programAspectRatio = programVideoBox.Width / programVideoBox.Height;
            await Assert.That(programAspectRatio).IsBetween(MinProgramAspectRatio, MaxProgramAspectRatio);
            await Assert.That(streamButtonBox.X > settingsButtonBox.X).IsTrue();

            var sessionCenter = sessionBarBox.X + (sessionBarBox.Width / 2d);
            var timerCenter = timerBox.X + (timerBox.Width / 2d);
            await Assert.That(Math.Abs(timerCenter - sessionCenter)).IsBetween(0d, TimerCenterTolerancePixels);

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                GoLiveLayoutParityScenario,
                GoLiveLayoutParityStep);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task GoLivePage_WithEmptyScene_AutoSeedsDefaultDevicesAndShowsStudioShell()
    {
        UiScenarioArtifacts.ResetScenario(BrowserTestConstants.GoLive.AutoSeedScenario);
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();
            await page.EvaluateAsync(
                BrowserTestConstants.GoLive.SeedEmptySceneScript,
                BrowserTestConstants.GoLive.SceneStorageKey);

            await page.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.GoLive.ProgramCard)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.GoLive.PreviewCard)).ToBeVisibleAsync();

            var sourceCards = page.Locator($"[data-test^='{UiTestIds.GoLive.SourceCameraSelect(string.Empty)}']");
            await Expect(sourceCards.First).ToContainTextAsync(BrowserTestConstants.Media.PrimaryCameraLabel);

            await page.GetByTestId(UiTestIds.GoLive.AudioTab).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.GoLive.AudioChannel(BrowserTestConstants.GoLive.MicChannelId)))
                .ToContainTextAsync(BrowserTestConstants.Media.PrimaryMicrophoneLabel);

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.GoLive.AutoSeedScenario,
                BrowserTestConstants.GoLive.AutoSeedStudioStep);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static async Task<LayoutBounds> GetRequiredBoxAsync(Microsoft.Playwright.ILocator locator) =>
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

    [Test]
    public async Task GoLivePage_SelectsSecondaryCameraAndTakesItToAir()
    {
        UiScenarioArtifacts.ResetScenario(BrowserTestConstants.GoLive.CameraSwitchScenario);
        var page = await _fixture.NewPageAsync();

        try
        {
            await SeedGoLiveSceneForReuseAsync(page);
            await page.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();

            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.ElementUsesVideoDeviceScript,
                new object[]
                {
                    UiDomIds.GoLive.ProgramVideo,
                    BrowserTestConstants.Media.PrimaryCameraId
                },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.ElementUsesVideoDeviceScript,
                new object[]
                {
                    UiDomIds.GoLive.PreviewVideo,
                    BrowserTestConstants.Media.PrimaryCameraId
                },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.GetByTestId(UiTestIds.GoLive.SourceCameraSelect(BrowserTestConstants.GoLive.SecondSourceId)).ClickAsync();
            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.ElementUsesVideoDeviceScript,
                new object[]
                {
                    UiDomIds.GoLive.ProgramVideo,
                    BrowserTestConstants.Media.SecondaryCameraId
                },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.GetByTestId(UiTestIds.GoLive.TakeToAir).ClickAsync();
            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.ElementUsesVideoDeviceScript,
                new object[]
                {
                    UiDomIds.GoLive.PreviewVideo,
                    BrowserTestConstants.Media.SecondaryCameraId
                },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.GoLive.CameraSwitchScenario,
                BrowserTestConstants.GoLive.CameraSwitchStep);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task GoLivePage_OnSingleLocalCameraBrowsers_ShowsHintAndMovesLivePreviewToTheSelectedCamera()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.AddInitScriptAsync(BrowserTestConstants.Media.DisableConcurrentLocalCameraCaptureScript);
            await SeedGoLiveSceneForReuseAsync(page);
            await page.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.GoLive.SingleLocalPreviewHint)).ToBeVisibleAsync();

            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.ElementUsesVideoDeviceScript,
                new object[]
                {
                    UiDomIds.GoLive.ProgramVideo,
                    BrowserTestConstants.Media.PrimaryCameraId
                },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await Expect(page.GetByTestId(UiTestIds.GoLive.SourceVideo(BrowserTestConstants.GoLive.SecondSourceId))).ToHaveCountAsync(0);

            await page.GetByTestId(UiTestIds.GoLive.SourceCameraSelect(BrowserTestConstants.GoLive.SecondSourceId)).ClickAsync();

            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.ElementUsesVideoDeviceScript,
                new object[]
                {
                    UiDomIds.GoLive.ProgramVideo,
                    BrowserTestConstants.Media.SecondaryCameraId
                },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await Expect(page.GetByTestId(UiTestIds.GoLive.SourceVideo(BrowserTestConstants.GoLive.SecondSourceId))).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.GoLive.SourceVideo(BrowserTestConstants.GoLive.FirstSourceId))).ToHaveCountAsync(0);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task GoLivePage_StartStream_WithVdoNinjaArmed_PublishesProgramVideoAndAudio()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await SeedGoLiveSceneForReuseAsync(page);
            await SeedGoLivePrimaryMicrophoneAsync(page);
            await SeedGoLiveOperationalSettingsAsync(page);
            await page.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();

            await page.EvaluateAsync(BrowserTestConstants.GoLive.InstallVdoNinjaHarnessScript);
            await Expect(page.GetByTestId(UiTestIds.GoLive.VdoToggle)).ToHaveClassAsync(BrowserTestConstants.Regexes.ToggleOnClass);
            await page.GetByTestId(UiTestIds.GoLive.StartStream).ClickAsync();

            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.HasVideoOnlyRequestScript,
                new object[] { BrowserTestConstants.Media.PrimaryCameraId },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.HasAudioOnlyRequestScript,
                new object[] { BrowserTestConstants.Media.PrimaryMicrophoneId },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await page.WaitForFunctionAsync(
                BrowserTestConstants.GoLive.VdoNinjaHarnessReadyScript,
                null,
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            var harnessState = await page.EvaluateAsync<JsonElement>(BrowserTestConstants.GoLive.GetVdoNinjaHarnessScript);
            await Assert.That(harnessState.GetProperty("joinRoomCalls")[0].GetProperty("room").GetString()).IsEqualTo(BrowserTestConstants.GoLive.VdoNinjaRoom);
            await Assert.That(harnessState.GetProperty("publishCalls")[0].GetProperty("room").GetString()).IsEqualTo(BrowserTestConstants.GoLive.VdoNinjaRoom);
            await Assert.That(harnessState.GetProperty("publishCalls")[0].GetProperty("streamId").GetString()).IsEqualTo(BrowserTestConstants.GoLive.VdoNinjaPublishStreamId);
            await Assert.That(harnessState.GetProperty("publishCalls")[0].GetProperty("trackKinds").EnumerateArray().Select(kind => kind.GetString())).Contains(kind => string.Equals(kind, "video", StringComparison.Ordinal));
            await Assert.That(harnessState.GetProperty("publishCalls")[0].GetProperty("trackKinds").EnumerateArray().Select(kind => kind.GetString())).Contains(kind => string.Equals(kind, "audio", StringComparison.Ordinal));

            var runtimeState = await page.EvaluateAsync<JsonElement>(
                BrowserTestConstants.GoLive.GetRuntimeStateScript,
                BrowserTestConstants.GoLive.RuntimeSessionId);
            await Assert.That(runtimeState.GetProperty("vdoNinja").GetProperty("active").GetBoolean()).IsTrue();
            await Assert.That(runtimeState.GetProperty("vdoNinja").GetProperty("roomName").GetString()).IsEqualTo(BrowserTestConstants.GoLive.VdoNinjaRoom);
            await Assert.That(runtimeState.GetProperty("vdoNinja").GetProperty("publishUrl").GetString()).IsEqualTo(BrowserTestConstants.GoLive.VdoNinjaPublishUrl);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task GoLivePage_RemoteGuestSources_AppearAndDriveConcurrentLiveKitAndVdoOutputs()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.AddInitScriptAsync(BuildRemoteSourceSeedScript());
            await SeedGoLiveSceneForReuseAsync(page);
            await SeedGoLivePrimaryMicrophoneAsync(page);
            await SeedDualTransportStudioSettingsAsync(page);
            await page.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();
            await page.EvaluateAsync(BrowserTestConstants.GoLive.InstallLiveKitHarnessScript);
            await page.EvaluateAsync(BrowserTestConstants.GoLive.InstallVdoNinjaHarnessScript);

            await Expect(page.GetByTestId(UiTestIds.GoLive.SourceCameraSelect(BrowserTestConstants.GoLive.RemoteLiveKitGuestSourceId))).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.GoLive.SourceCameraSelect(BrowserTestConstants.GoLive.RemoteVdoGuestSourceId))).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.GoLive.SourceCameraSelect(BrowserTestConstants.GoLive.RemoteLiveKitGuestSourceId)).ClickAsync();
            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.ElementUsesVideoDeviceScript,
                new object[]
                {
                    UiDomIds.GoLive.ProgramVideo,
                    BrowserTestConstants.GoLive.RemoteLiveKitGuestSourceId
                },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.GetByTestId(UiTestIds.GoLive.StartStream).ClickAsync();

            await WaitForLiveKitPublishAsync(page);
            await page.WaitForFunctionAsync(
                BrowserTestConstants.GoLive.VdoNinjaHarnessReadyScript,
                null,
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await page.WaitForFunctionAsync(
                BrowserTestConstants.GoLive.RecordingRuntimeUsesProgramSourceScript,
                new object[] { BrowserTestConstants.GoLive.RuntimeSessionId, BrowserTestConstants.GoLive.RemoteLiveKitGuestSourceId },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.ElementUsesVideoDeviceScript,
                new object[]
                {
                    UiDomIds.GoLive.PreviewVideo,
                    BrowserTestConstants.GoLive.RemoteLiveKitGuestSourceId
                },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            var runtimeState = await page.EvaluateAsync<JsonElement>(
                BrowserTestConstants.GoLive.GetRuntimeStateScript,
                BrowserTestConstants.GoLive.RuntimeSessionId);

            await Assert.That(runtimeState.GetProperty("liveKit").GetProperty("active").GetBoolean()).IsTrue();
            await Assert.That(runtimeState.GetProperty("vdoNinja").GetProperty("active").GetBoolean()).IsTrue();
            await Assert.That(runtimeState.GetProperty("program").GetProperty("primarySourceId").GetString()).IsEqualTo(BrowserTestConstants.GoLive.RemoteLiveKitGuestSourceId);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task GoLivePage_SwitchesStudioTabsAndCreatesRemoteRoom()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await SeedGoLiveSceneForReuseAsync(page);
            await page.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.GoLive.SceneControls)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.GoLive.ProviderCard(GoLiveTargetCatalog.TargetIds.Recording))).ToHaveCountAsync(0);

            await page.GetByTestId(UiTestIds.GoLive.RoomTab).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.GoLive.RoomEmpty)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.GoLive.CreateRoom).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.GoLive.RoomActive)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.GoLive.RoomParticipant(BrowserTestConstants.GoLive.PrimaryParticipantId))).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.GoLive.RoomParticipant(BrowserTestConstants.GoLive.PrimaryParticipantId)))
                .ToContainTextAsync(BrowserTestConstants.GoLive.HostParticipantName);

            await page.GetByTestId(UiTestIds.GoLive.AudioTab).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.GoLive.AudioMixer)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.GoLive.AudioChannel(BrowserTestConstants.GoLive.MicChannelId))).ToBeVisibleAsync();
            await Expect(page.Locator("body")).Not.ToContainTextAsync(BrowserTestConstants.GoLive.LegacyNetworkUploadMetric);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    internal static async Task SeedGoLiveSceneForReuseAsync(Microsoft.Playwright.IPage page)
    {
        await page.GotoAsync(BrowserTestConstants.Routes.Library);
        await Expect(page.GetByTestId(UiTestIds.Library.Page))
            .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

        await page.EvaluateAsync(
            BrowserTestConstants.GoLive.SeedSceneScript,
            new object[]
            {
                BrowserTestConstants.GoLive.SceneStorageKey,
                BrowserTestConstants.GoLive.FirstSourceId,
                BrowserTestConstants.GoLive.SecondSourceId,
                BrowserTestConstants.Media.PrimaryCameraId,
                BrowserTestConstants.Media.SecondaryCameraId
            });
    }

    internal static Task SeedGoLivePrimaryMicrophoneAsync(Microsoft.Playwright.IPage page) =>
        page.EvaluateAsync(
            BrowserTestConstants.GoLive.SeedSceneWithPrimaryMicrophoneScript,
            new object[]
            {
                BrowserTestConstants.GoLive.SceneStorageKey,
                BrowserTestConstants.Media.PrimaryMicrophoneId,
                BrowserTestConstants.Media.PrimaryMicrophoneLabel
            });

    internal static Task SeedGoLiveOperationalSettingsAsync(Microsoft.Playwright.IPage page) =>
        page.EvaluateAsync(
            BrowserTestConstants.GoLive.SeedOperationalStudioSettingsScript,
            new object[]
            {
                BrowserTestConstants.GoLive.StoredStudioSettingsKey,
                BrowserTestConstants.GoLive.VdoNinjaRoom,
                BrowserTestConstants.GoLive.VdoNinjaPublishUrl,
                BrowserTestConstants.GoLive.YoutubeUrl,
                BrowserTestConstants.GoLive.YoutubeKey,
                BrowserTestConstants.GoLive.FirstSourceId
            });

    internal static Task SeedDualTransportStudioSettingsAsync(Microsoft.Playwright.IPage page) =>
        page.EvaluateAsync(
            BrowserTestConstants.GoLive.SeedDualTransportStudioSettingsScript,
            new object[]
            {
                BrowserTestConstants.GoLive.StoredStudioSettingsKey,
                BrowserTestConstants.GoLive.LiveKitServer,
                BrowserTestConstants.GoLive.LiveKitRoom,
                BrowserTestConstants.GoLive.LiveKitToken,
                BrowserTestConstants.GoLive.VdoNinjaRoom,
                BrowserTestConstants.GoLive.VdoNinjaPublishUrl,
                BrowserTestConstants.GoLive.FirstSourceId
            });

    private static string BuildRemoteSourceSeedScript()
    {
        var seed = JsonSerializer.Serialize(new Dictionary<string, object[]>
        {
            [BrowserTestConstants.GoLive.LiveKitTransportId] =
            [
                new
                {
                    sourceId = BrowserTestConstants.GoLive.RemoteLiveKitGuestExternalId,
                    label = BrowserTestConstants.GoLive.RemoteLiveKitGuestLabel
                }
            ],
            [BrowserTestConstants.GoLive.VdoNinjaTransportId] =
            [
                new
                {
                    sourceId = BrowserTestConstants.GoLive.RemoteVdoGuestExternalId,
                    label = BrowserTestConstants.GoLive.RemoteVdoGuestLabel
                }
            ]
        });

        return $$"""window["{{AppMediaRuntime.BrowserMedia.RemoteSourceSeedGlobalName}}"] = {{seed}};""";
    }

    private static async Task WaitForLiveKitPublishAsync(IPage page)
    {
        var deadline = DateTimeOffset.UtcNow.AddMilliseconds(BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs);

        while (DateTimeOffset.UtcNow < deadline)
        {
            var runtimeState = await page.EvaluateAsync<JsonElement?>(
                BrowserTestConstants.GoLive.GetRuntimeStateScript,
                BrowserTestConstants.GoLive.RuntimeSessionId);
            var harnessState = await page.EvaluateAsync<JsonElement?>(BrowserTestConstants.GoLive.GetLiveKitHarnessScript);

            if (IsLiveKitPublishReady(runtimeState, harnessState))
            {
                return;
            }

            await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.DiagnosticPollDelayMs);
        }

        _ = await page.EvaluateAsync<JsonElement?>(
            BrowserTestConstants.GoLive.GetRuntimeStateScript,
            BrowserTestConstants.GoLive.RuntimeSessionId);

        _ = await page.EvaluateAsync<JsonElement?>(BrowserTestConstants.GoLive.GetLiveKitHarnessScript);

        Assert.Fail("Unexpected execution path.");
    }

    private static bool IsLiveKitPublishReady(JsonElement? runtimeState, JsonElement? harnessState)
    {
        var liveKitActive = runtimeState.HasValue
            && runtimeState.Value.ValueKind is JsonValueKind.Object
            && runtimeState.Value.TryGetProperty("liveKit", out var liveKitState)
            && liveKitState.TryGetProperty("active", out var liveKitActiveValue)
            && liveKitActiveValue.ValueKind is JsonValueKind.True;

        var hasConnected = harnessState.HasValue
            && harnessState.Value.ValueKind is JsonValueKind.Object
            && harnessState.Value.TryGetProperty("connectCalls", out var connectCalls)
            && connectCalls.ValueKind is JsonValueKind.Array
            && connectCalls.GetArrayLength() >= 1;

        return liveKitActive
            && hasConnected
            && HarnessHasPublishedKind(harnessState, "video")
            && HarnessHasPublishedKind(harnessState, "audio");
    }

    private static bool HarnessHasPublishedKind(JsonElement? harnessState, string kind)
    {
        if (!harnessState.HasValue
            || harnessState.Value.ValueKind is not JsonValueKind.Object
            || !harnessState.Value.TryGetProperty("publishCalls", out var publishCalls)
            || publishCalls.ValueKind is not JsonValueKind.Array)
        {
            return false;
        }

        foreach (var publishCall in publishCalls.EnumerateArray())
        {
            if (publishCall.ValueKind is not JsonValueKind.Object
                || !publishCall.TryGetProperty("kind", out var publishedKind)
                || publishedKind.ValueKind is not JsonValueKind.String)
            {
                continue;
            }

            if (string.Equals(publishedKind.GetString(), kind, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private readonly record struct LayoutBounds(double X, double Y, double Width, double Height);

    [Test]
    public async Task SettingsPage_LinksIntoGoLiveRoutingAndGoLiveLinksBackToSettings()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Settings);
            await Expect(page.GetByTestId(UiTestIds.Settings.Page)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.Settings.CameraRoutingCta).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(AppRoutes.GoLive));
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.GoLive.OpenSettings).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.Settings));
            await Expect(page.GetByTestId(UiTestIds.Settings.Page)).ToBeVisibleAsync();
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
