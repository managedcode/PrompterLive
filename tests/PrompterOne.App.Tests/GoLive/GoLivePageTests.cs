using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Streaming;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.GoLive.Models;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Tests;

namespace PrompterOne.App.Tests;

public sealed class GoLivePageTests : BunitContext
{
    private const string ProgramMonitorClass = "gl-monitor-program";
    private const string SessionBarClass = "gl-topbar";
    private const string SettingsButtonClass = "gl-settings-btn";
    private const string SourcesRailClass = "gl-sources";
    private const string PreviewRailClass = "gl-sidebar-right";
    private const string SceneBarClass = "gl-scenes-bar";
    private const string SceneSettingsStorageKey = "prompterone.scene";

    private readonly AppHarness _harness;

    public GoLivePageTests()
    {
        _harness = TestHarnessFactory.Create(this);
    }

    [Fact]
    public void GoLivePage_PersistsMultipleDestinationsAndProgramSettings()
    {
        SeedSceneState(CreateTwoCameraScene());
        _harness.JsRuntime.SavedValues[StudioSettingsStore.StorageKey] = StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                ExternalDestinations =
                [
                    AppTestData.GoLive.CreateLiveKitDestination(isEnabled: false),
                    AppTestData.GoLive.CreateYoutubeDestination(isEnabled: false)
                ]
            }
        };

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.GoLive.Page, cut.Markup, StringComparison.Ordinal));

        cut.FindByTestId(UiTestIds.GoLive.LiveKitToggle).Click();
        cut.FindByTestId(UiTestIds.GoLive.YoutubeToggle).Click();
        cut.FindByTestId(UiTestIds.GoLive.RecordingToggle).Click();

        cut.WaitForAssertion(() =>
        {
            var settings = _harness.JsRuntime.GetSavedValue<StudioSettings>(StudioSettingsStore.StorageKey);
            Assert.True(settings.Streaming.LocalRecordingEnabled);
            Assert.Contains(
                settings.Streaming.ExternalDestinations ?? Array.Empty<StreamingProfile>(),
                destination => string.Equals(destination.Id, GoLiveTargetCatalog.TargetIds.LiveKit, StringComparison.Ordinal) && destination.IsEnabled);
            Assert.Contains(
                settings.Streaming.ExternalDestinations ?? Array.Empty<StreamingProfile>(),
                destination => string.Equals(destination.Id, GoLiveTargetCatalog.TargetIds.Youtube, StringComparison.Ordinal) && destination.IsEnabled);
        });
    }

    [Fact]
    public void GoLivePage_RendersProductionStudioLayoutLandmarks()
    {
        SeedSceneState(CreateTwoCameraScene());

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(SessionBarClass, cut.FindByTestId(UiTestIds.GoLive.SessionBar).ClassName, StringComparison.Ordinal);
            Assert.Contains(SourcesRailClass, cut.FindByTestId(UiTestIds.GoLive.SourceRail).ClassName, StringComparison.Ordinal);
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.Stage));
            Assert.Contains(PreviewRailClass, cut.FindByTestId(UiTestIds.GoLive.PreviewRail).ClassName, StringComparison.Ordinal);
            Assert.Contains(ProgramMonitorClass, cut.FindByTestId(UiTestIds.GoLive.ProgramCard).ClassName, StringComparison.Ordinal);
            Assert.Contains(SceneBarClass, cut.FindByTestId(UiTestIds.GoLive.SceneBar).ClassName, StringComparison.Ordinal);
            Assert.Contains(SettingsButtonClass, cut.FindByTestId(UiTestIds.GoLive.OpenSettings).ClassName, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void GoLivePage_TopLeftHomeControl_TargetsLibraryRoute()
    {
        SeedSceneState(CreateTwoCameraScene());
        TrackShellRoute(AppTestData.Routes.GoLiveDemo);

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() =>
        {
            var homeLink = cut.FindByTestId(UiTestIds.GoLive.Back);
            Assert.Equal(AppRoutes.Library, homeLink.GetAttribute("href"));
        });
    }

    [Fact]
    public void GoLivePage_BackControl_TargetsPreviousInAppRoute_WhenKnown()
    {
        SeedSceneState(CreateTwoCameraScene());
        TrackShellRoute(AppTestData.Routes.Settings);
        TrackShellRoute(AppTestData.Routes.GoLiveDemo);

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() =>
        {
            var backLink = cut.FindByTestId(UiTestIds.GoLive.Back);
            Assert.Equal(AppTestData.Routes.Settings, backLink.GetAttribute("href"));
        });
    }

    [Fact]
    public void GoLivePage_UsesGenericOperationalTitleInsteadOfLoadedScriptTitle()
    {
        SeedSceneState(CreateTwoCameraScene());

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.GoLiveLeadership);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() =>
        {
            var screenTitle = cut.FindByTestId(UiTestIds.GoLive.ScreenTitle).TextContent.Trim();
            var shellState = Services.GetRequiredService<AppShellService>().State;

            Assert.Equal(GoLiveText.Chrome.ScreenTitle, screenTitle);
            Assert.DoesNotContain(AppTestData.Scripts.TedLeadershipTitle, screenTitle, StringComparison.Ordinal);
            Assert.Equal(GoLiveText.Chrome.ScreenTitle, shellState.Title);
            Assert.Equal(AppShellScreen.GoLive, shellState.Screen);
        });
    }

    [Fact]
    public void GoLivePage_TogglesProgramSourcesAndPersistsScene()
    {
        SeedSceneState(CreateTwoCameraScene());

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.GoLive.SourcesCard, cut.Markup, StringComparison.Ordinal));
        cut.WaitForAssertion(() =>
            Assert.NotEmpty(cut.FindAll($"[data-testid^='{UiTestIds.GoLive.SourceCamera(string.Empty)}']")));

        cut.FindByTestId(UiTestIds.GoLive.SourceCameraAction(AppTestData.Camera.FirstDeviceId)).Click();

        var sceneState = _harness.JsRuntime.GetSavedValue<MediaSceneState>("prompterone.scene");
        Assert.False(
            sceneState.Cameras.Single(camera => camera.SourceId == AppTestData.Camera.FirstSourceId).Transform.IncludeInOutput);
    }

    [Fact]
    public void GoLivePage_LoadsPersistedDestinationsFromBrowserStorage()
    {
        SeedSceneState(CreateTwoCameraScene());
        _harness.JsRuntime.SavedValues[StudioSettingsStore.StorageKey] = StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                ExternalDestinations =
                [
                    AppTestData.GoLive.CreateLiveKitDestination(),
                    AppTestData.GoLive.CreateYoutubeDestination()
                ],
                DestinationSourceSelections =
                [
                    new GoLiveDestinationSourceSelection(
                        GoLiveTargetCatalog.TargetIds.LiveKit,
                        [AppTestData.Camera.SecondSourceId])
                ],
                BitrateKbps = AppTestData.Streaming.BitrateKbps
            }
        };

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(
                "on",
                cut.FindByTestId(UiTestIds.GoLive.LiveKitToggle).ClassName,
                StringComparison.Ordinal);
            Assert.Contains(
                "on",
                cut.FindByTestId(UiTestIds.GoLive.YoutubeToggle).ClassName,
                StringComparison.Ordinal);
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.ProviderCard(GoLiveTargetCatalog.TargetIds.LiveKit)));
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.ProviderCard(GoLiveTargetCatalog.TargetIds.Youtube)));
        });
    }

    [Fact]
    public void GoLivePage_Load_MigratesLegacyStreamingDestinationsIntoExternalDestinationList()
    {
        SeedSceneState(CreateTwoCameraScene());
        _harness.JsRuntime.SavedValues[StudioSettingsStore.StorageKey] = StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                LiveKitEnabled = true,
                LiveKitServerUrl = AppTestData.GoLive.LiveKitServer,
                LiveKitRoomName = AppTestData.GoLive.LiveKitRoom,
                LiveKitToken = AppTestData.GoLive.LiveKitToken,
                YoutubeEnabled = true,
                YoutubeRtmpUrl = AppTestData.GoLive.YoutubeUrl,
                YoutubeStreamKey = AppTestData.GoLive.YoutubeKey
            }
        };

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() =>
        {
            var settings = _harness.JsRuntime.GetSavedValue<StudioSettings>(StudioSettingsStore.StorageKey);
            Assert.Contains(
                settings.Streaming.ExternalDestinations ?? Array.Empty<StreamingProfile>(),
                destination => string.Equals(destination.Id, GoLiveTargetCatalog.TargetIds.LiveKit, StringComparison.Ordinal));
            Assert.Contains(
                settings.Streaming.ExternalDestinations ?? Array.Empty<StreamingProfile>(),
                destination => string.Equals(destination.Id, GoLiveTargetCatalog.TargetIds.Youtube, StringComparison.Ordinal));
            Assert.Contains("on", cut.FindByTestId(UiTestIds.GoLive.LiveKitToggle).ClassName, StringComparison.Ordinal);
            Assert.Contains("on", cut.FindByTestId(UiTestIds.GoLive.YoutubeToggle).ClassName, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void GoLivePage_SelectsSecondCameraForCanvas()
    {
        SeedSceneState(CreateTwoCameraScene());

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() =>
            Assert.Equal(2, cut.FindAll($"[data-testid^='{UiTestIds.GoLive.SourceCameraSelect(string.Empty)}']").Count));

        cut.FindByTestId(UiTestIds.GoLive.SourceCameraSelect(AppTestData.Camera.SecondSourceId)).Click();

        cut.WaitForAssertion(() =>
            Assert.Equal(
                AppTestData.Camera.SideCamera,
                cut.FindByTestId(UiTestIds.GoLive.SelectedSourceLabel).TextContent.Trim()));
    }

    [Fact]
    public void GoLivePage_RendersStudioParitySurfaceAndRemoteRoomFlow()
    {
        SeedSceneState(CreateTwoCameraScene());

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.ModeDirector));
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.SceneControls));
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.StreamTab));
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.ProviderCard(GoLiveTargetCatalog.TargetIds.Obs)));
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.ProviderCard(GoLiveTargetCatalog.TargetIds.Recording)));
        });

        cut.FindByTestId(UiTestIds.GoLive.RoomTab).Click();
        cut.FindByTestId(UiTestIds.GoLive.CreateRoom).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.RoomActive));
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.RoomInvite));
            var participant = cut.FindByTestId(UiTestIds.GoLive.RoomParticipant(AppTestData.GoLive.PrimaryParticipantId));
            Assert.NotNull(participant);
            Assert.Contains(AppTestData.GoLive.PrimaryParticipantName, participant.TextContent, StringComparison.Ordinal);
            Assert.Contains(AppTestData.GoLive.PrimaryParticipantRole, participant.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain(AppTestData.GoLive.LegacyNetworkUploadMetric, cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void GoLivePage_ShowsElapsedTimerForActiveRecordingSession()
    {
        SeedSceneState(CreateTwoCameraScene());

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();
        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.Page)));

        var sessionService = Services.GetRequiredService<GoLiveSessionService>();
        var current = sessionService.State;
        sessionService.SetState(current with
        {
            IsRecordingActive = true,
            RecordingStartedAt = DateTimeOffset.UtcNow.AddMinutes(-2).AddSeconds(-3)
        });

        cut.WaitForAssertion(() =>
        {
            var timerText = cut.FindByTestId(UiTestIds.GoLive.SessionTimer).TextContent.Trim();
            Assert.StartsWith(AppTestData.GoLive.SessionTimerPrefix, timerText, StringComparison.Ordinal);
        });
    }

    private static MediaSceneState CreateTwoCameraScene() =>
        new(
            [
                new SceneCameraSource(
                    AppTestData.Camera.FirstSourceId,
                    AppTestData.Camera.FirstDeviceId,
                    AppTestData.Camera.FrontCamera,
                    new MediaSourceTransform(IncludeInOutput: true)),
                new SceneCameraSource(
                    AppTestData.Camera.SecondSourceId,
                    AppTestData.Camera.SecondDeviceId,
                    AppTestData.Camera.SideCamera,
                    new MediaSourceTransform(IncludeInOutput: false))
            ],
            "mic-1",
            AppTestData.Scripts.BroadcastMic,
            new AudioBusState([new AudioInputState("mic-1", AppTestData.Scripts.BroadcastMic)]));

    private void SeedSceneState(MediaSceneState sceneState)
    {
        _harness.JsRuntime.SavedJsonValues[SceneSettingsStorageKey] = JsonSerializer.Serialize(sceneState);
    }

    private void TrackShellRoute(string route)
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        Services.GetRequiredService<AppShellService>()
            .TrackNavigation(navigation.ToAbsoluteUri(route).ToString());
    }
}
