using System.Text.Json;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.App.UITests;

public sealed class GoLiveFlowTests(StandaloneAppFixture fixture) : IClassFixture<StandaloneAppFixture>
{
    private const double MaxPreviewRailWidth = 360d;
    private const double MaxProgramAspectRatio = 1.95d;
    private const double MaxSourcesRailWidth = 260d;
    private const double MinPreviewRailWidth = 260d;
    private const double MinProgramAspectRatio = 1.55d;
    private const double MinSourcesRailWidth = 170d;
    private const string GoLiveLayoutParityScenario = "go-live-layout-parity";
    private const string GoLiveLayoutParityStep = "01-new-design-shell";
    private const string IncludeActionLabel = "Include";
    private const string RemoveActionLabel = "Remove";
    private const double TimerCenterTolerancePixels = 48d;

    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
    public async Task GoLivePage_ArmsDestinationsAndPersistsOperationalToggles()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await SeedGoLiveSceneForReuseAsync(page);
            await page.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.GoLive.ProgramCard)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.GoLive.SourcesCard)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.GoLive.LiveKitToggle).ClickAsync();
            await page.GetByTestId(UiTestIds.GoLive.YoutubeToggle).ClickAsync();
            await page.GetByTestId(UiTestIds.GoLive.RecordingToggle).ClickAsync();

            await page.WaitForFunctionAsync(
                BrowserTestConstants.GoLive.PersistedToggleTargetsScript,
                BrowserTestConstants.GoLive.StoredStudioSettingsKey,
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.ReloadAsync(new() { WaitUntil = Microsoft.Playwright.WaitUntilState.NetworkIdle });

            await Expect(page.GetByTestId(UiTestIds.GoLive.ObsToggle)).ToHaveClassAsync(BrowserTestConstants.Regexes.ToggleOnClass);
            await Expect(page.GetByTestId(UiTestIds.GoLive.LiveKitToggle)).ToHaveClassAsync(BrowserTestConstants.Regexes.ToggleOnClass);
            await Expect(page.GetByTestId(UiTestIds.GoLive.YoutubeToggle)).ToHaveClassAsync(BrowserTestConstants.Regexes.ToggleOnClass);
            await Expect(page.GetByTestId(UiTestIds.GoLive.RecordingToggle)).ToHaveClassAsync(BrowserTestConstants.Regexes.ToggleOnClass);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task GoLivePage_TogglesSceneCameraMembershipAndLinksBackToRead()
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

            await page.GetByTestId(UiTestIds.GoLive.OpenRead).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.TeleprompterDemo));
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page)).ToBeVisibleAsync();
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
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
            Assert.NotNull(previewHandle);

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

    [Fact]
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

    [Fact]
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

            var sessionBarBox = await GetRequiredBoxAsync(sessionBar);
            var sourceRailBox = await GetRequiredBoxAsync(sourceRail);
            var programCardBox = await GetRequiredBoxAsync(programCard);
            var programVideoBox = await GetRequiredBoxAsync(programVideo);
            var previewRailBox = await GetRequiredBoxAsync(previewRail);
            var settingsButtonBox = await GetRequiredBoxAsync(settingsButton);
            var streamButtonBox = await GetRequiredBoxAsync(streamButton);
            var timerBox = await GetRequiredBoxAsync(timer);

            Assert.InRange(sourceRailBox.Width, MinSourcesRailWidth, MaxSourcesRailWidth);
            Assert.InRange(previewRailBox.Width, MinPreviewRailWidth, MaxPreviewRailWidth);
            Assert.True(programCardBox.Width > sourceRailBox.Width);
            Assert.True(programCardBox.Width > previewRailBox.Width);

            var programAspectRatio = programVideoBox.Width / programVideoBox.Height;
            Assert.InRange(programAspectRatio, MinProgramAspectRatio, MaxProgramAspectRatio);
            Assert.True(streamButtonBox.X > settingsButtonBox.X);

            var sessionCenter = sessionBarBox.X + (sessionBarBox.Width / 2d);
            var timerCenter = timerBox.X + (timerBox.Width / 2d);
            Assert.InRange(Math.Abs(timerCenter - sessionCenter), 0d, TimerCenterTolerancePixels);

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

    [Fact]
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

            var sourceCards = page.Locator($"[data-testid^='{UiTestIds.GoLive.SourceCameraSelect(string.Empty)}']");
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

    [Fact]
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
            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.ElementUsesVideoDeviceScript,
                new object[]
                {
                    UiDomIds.GoLive.PreviewVideo,
                    BrowserTestConstants.Media.PrimaryCameraId
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

    [Fact]
    public async Task GoLivePage_StartStream_WithLiveKitArmed_PublishesProgramVideoAndAudio()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await SeedGoLiveSceneForReuseAsync(page);
            await SeedGoLiveOperationalSettingsAsync(page);
            await page.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();

            await page.EvaluateAsync(BrowserTestConstants.GoLive.InstallLiveKitHarnessScript);
            await Expect(page.GetByTestId(UiTestIds.GoLive.LiveKitToggle)).ToHaveClassAsync(BrowserTestConstants.Regexes.ToggleOnClass);
            await page.GetByTestId(UiTestIds.GoLive.StartStream).ClickAsync();

            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.HasAudioVideoRequestScript,
                new object[]
                {
                    BrowserTestConstants.Media.PrimaryCameraId,
                    BrowserTestConstants.Media.PrimaryMicrophoneId
                },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await page.WaitForFunctionAsync(
                BrowserTestConstants.GoLive.LiveKitHarnessReadyScript,
                null,
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            var harnessState = await page.EvaluateAsync<JsonElement>(BrowserTestConstants.GoLive.GetLiveKitHarnessScript);
            Assert.Equal(BrowserTestConstants.GoLive.LiveKitServer, harnessState.GetProperty("connectCalls")[0].GetProperty("url").GetString());
            Assert.Contains(
                harnessState.GetProperty("publishCalls").EnumerateArray().Select(call => call.GetProperty("kind").GetString()),
                kind => string.Equals(kind, "video", StringComparison.Ordinal));
            Assert.Contains(
                harnessState.GetProperty("publishCalls").EnumerateArray().Select(call => call.GetProperty("kind").GetString()),
                kind => string.Equals(kind, "audio", StringComparison.Ordinal));
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task GoLivePage_StartStream_WithObsArmed_RoutesMicrophoneAudioForObsBrowserSource()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await SeedGoLiveSceneForReuseAsync(page);
            await page.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();

            await page.EvaluateAsync(BrowserTestConstants.GoLive.EnableObsStudioScript);
            var obsToggle = page.GetByTestId(UiTestIds.GoLive.ObsToggle);
            await Expect(obsToggle).ToHaveClassAsync(BrowserTestConstants.Regexes.ToggleOnClass);
            await page.GetByTestId(UiTestIds.GoLive.StartStream).ClickAsync();

            await page.WaitForFunctionAsync(
                BrowserTestConstants.Media.HasAudioVideoRequestScript,
                new object[]
                {
                    BrowserTestConstants.Media.PrimaryCameraId,
                    BrowserTestConstants.Media.PrimaryMicrophoneId
                },
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await page.WaitForFunctionAsync(
                BrowserTestConstants.GoLive.ObsRuntimeAudioAttachedScript,
                BrowserTestConstants.GoLive.RuntimeSessionId,
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            var runtimeState = await page.EvaluateAsync<JsonElement>(
                BrowserTestConstants.GoLive.GetRuntimeStateScript,
                BrowserTestConstants.GoLive.RuntimeSessionId);

            Assert.True(runtimeState.GetProperty("obs").GetProperty("active").GetBoolean());
            Assert.True(runtimeState.GetProperty("obs").GetProperty("audioAttached").GetBoolean());
            Assert.Equal("obsstudio", runtimeState.GetProperty("obs").GetProperty("environment").GetString());
            Assert.Equal(BrowserTestConstants.Media.PrimaryCameraId, runtimeState.GetProperty("videoDeviceId").GetString());
            Assert.Equal(BrowserTestConstants.Media.PrimaryMicrophoneId, runtimeState.GetProperty("audioDeviceId").GetString());
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task GoLivePage_SwitchesStudioTabsAndCreatesRemoteRoom()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await SeedGoLiveSceneForReuseAsync(page);
            await page.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.GoLive.SceneControls)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.GoLive.UtilitySource(BrowserTestConstants.GoLive.PrompterUtilitySourceId))).ToBeVisibleAsync();

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
        await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();

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

    private static Task SeedGoLiveOperationalSettingsAsync(Microsoft.Playwright.IPage page) =>
        page.EvaluateAsync(
            BrowserTestConstants.GoLive.SeedOperationalStudioSettingsScript,
            new object[]
            {
                BrowserTestConstants.GoLive.StoredStudioSettingsKey,
                BrowserTestConstants.GoLive.LiveKitServer,
                BrowserTestConstants.GoLive.LiveKitRoom,
                BrowserTestConstants.GoLive.LiveKitToken,
                BrowserTestConstants.GoLive.YoutubeUrl,
                BrowserTestConstants.GoLive.YoutubeKey,
                BrowserTestConstants.GoLive.FirstSourceId
            });

    private readonly record struct LayoutBounds(double X, double Y, double Width, double Height);

    [Fact]
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
