using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Streaming;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.GoLive.Models;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Storage;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class GoLivePageTests : BunitContext
{
    private const string EnabledStateValue = "true";
    private const string SceneSettingsStorageKey = BrowserAppSettingsKeys.SceneSettings;

    private readonly AppHarness _harness;

    public GoLivePageTests()
    {
        _harness = TestHarnessFactory.Create(this);
    }

    [Test]
    public void GoLivePage_PersistsMultipleDestinationsAndProgramSettings()
    {
        SeedSceneState(CreateTwoCameraScene());
        _harness.JsRuntime.SavedValues[StudioSettingsStore.StorageKey] = StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                TransportConnections =
                [
                    AppTestData.GoLive.CreateVdoNinjaConnection(isEnabled: false)
                ],
                DistributionTargets =
                [
                    AppTestData.GoLive.CreateYoutubeTarget(isEnabled: false)
                ]
            }
        };

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.GoLive.Page, cut.Markup, StringComparison.Ordinal));

        cut.FindByTestId(UiTestIds.GoLive.VdoToggle).Click();
        cut.FindByTestId(UiTestIds.GoLive.YoutubeToggle).Click();
        cut.FindByTestId(UiTestIds.GoLive.StartRecording).Click();

        cut.WaitForAssertion(() =>
        {
            var settings = _harness.JsRuntime.GetSavedValue<StudioSettings>(StudioSettingsStore.StorageKey);
            Assert.True(settings.Streaming.RecordingSettings.IsEnabled);
            Assert.Contains(
                settings.Streaming.TransportConnections ?? Array.Empty<TransportConnectionProfile>(),
                connection => string.Equals(connection.Id, GoLiveTargetCatalog.TargetIds.VdoNinja, StringComparison.Ordinal) && connection.IsEnabled);
            Assert.Contains(
                settings.Streaming.DistributionTargets ?? Array.Empty<DistributionTargetProfile>(),
                target => string.Equals(target.Id, GoLiveTargetCatalog.TargetIds.Youtube, StringComparison.Ordinal) && target.IsEnabled);
        });
    }

    [Test]
    public void GoLivePage_RendersProductionStudioLayoutLandmarks()
    {
        SeedSceneState(CreateTwoCameraScene());

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.SessionBar));
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.SourceRail));
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.Stage));
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.PreviewRail));
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.ProgramCard));
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.SceneBar));
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.OpenSettings));
        });
    }

    [Test]
    public void GoLivePage_UsesSharedIconsAndPercentFillPrimitives_ForTopbarAndSidebarChrome()
    {
        SeedSceneState(CreateTwoCameraScene());

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.Back));
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.AddSource));
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.OpenSettings));
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.TakeToAir));
        });

        cut.FindByTestId(UiTestIds.GoLive.AudioTab).Click();

        cut.WaitForAssertion(() =>
        {
            var audioChannel = cut.FindByTestId(UiTestIds.GoLive.AudioChannel(AppTestData.GoLive.MicChannelId));
            Assert.Equal(GoLiveText.Session.IdleStateValue, audioChannel.GetAttribute("data-live-state"));
            Assert.True(int.TryParse(audioChannel.GetAttribute("data-live-level"), out var liveLevel));
            Assert.True(liveLevel >= 0);
        });

        cut.FindByTestId(UiTestIds.GoLive.RoomTab).Click();
        cut.WaitForState(() =>
            cut.FindAll($"[data-test='{UiTestIds.GoLive.CreateRoom}']").Count > 0 ||
            cut.FindAll($"[data-test='{UiTestIds.GoLive.RoomInvite}']").Count > 0);

        var createRoomButtons = cut.FindAll($"[data-test='{UiTestIds.GoLive.CreateRoom}']");
        if (createRoomButtons.Count > 0)
        {
            createRoomButtons[0].Click();
        }

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.RoomInvite));
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.RoomParticipant(AppTestData.GoLive.PrimaryParticipantId)));
        });
    }

    [Test]
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

    [Test]
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

    [Test]
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
            var expectedScreenTitle = Text(GoLiveText.Chrome.ScreenTitle);

            Assert.Equal(expectedScreenTitle, screenTitle);
            Assert.DoesNotContain(AppTestData.Scripts.TedLeadershipTitle, screenTitle, StringComparison.Ordinal);
            Assert.Equal(expectedScreenTitle, shellState.Title);
            Assert.Equal(AppShellScreen.GoLive, shellState.Screen);
        });
    }

    [Test]
    public void GoLivePage_TogglesProgramSourcesAndPersistsScene()
    {
        SeedSceneState(CreateTwoCameraScene());

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.GoLive.SourcesCard, cut.Markup, StringComparison.Ordinal));
        cut.WaitForAssertion(() =>
            Assert.NotEmpty(cut.FindAll($"[data-test^='{UiTestIds.GoLive.SourceCamera(string.Empty)}']")));

        cut.FindByTestId(UiTestIds.GoLive.SourceCameraAction(AppTestData.Camera.FirstDeviceId)).Click();

        var sceneState = _harness.JsRuntime.GetSavedValue<MediaSceneState>(BrowserAppSettingsKeys.SceneSettings);
        Assert.False(
            sceneState.Cameras.Single(camera => camera.SourceId == AppTestData.Camera.FirstSourceId).Transform.IncludeInOutput);
    }

    [Test]
    public void GoLivePage_LoadsPersistedDestinationsFromBrowserStorage()
    {
        SeedSceneState(CreateTwoCameraScene());
        _harness.JsRuntime.SavedValues[StudioSettingsStore.StorageKey] = StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                TransportConnections =
                [
                    AppTestData.GoLive.CreateVdoNinjaConnection()
                ],
                DistributionTargets =
                [
                    AppTestData.GoLive.CreateYoutubeTarget()
                ],
                SourceSelections =
                [
                    new GoLiveDestinationSourceSelection(
                        GoLiveTargetCatalog.TargetIds.VdoNinja,
                        [AppTestData.Camera.SecondSourceId])
                ],
                ProgramCapture = new ProgramCaptureProfile(BitrateKbps: AppTestData.Streaming.BitrateKbps)
            }
        };

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(
                EnabledStateValue,
                cut.FindByTestId(UiTestIds.GoLive.VdoToggle).GetAttribute("data-enabled"));
            Assert.Equal(
                EnabledStateValue,
                cut.FindByTestId(UiTestIds.GoLive.YoutubeToggle).GetAttribute("data-enabled"));
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.ProviderCard(GoLiveTargetCatalog.TargetIds.VdoNinja)));
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.ProviderCard(GoLiveTargetCatalog.TargetIds.Youtube)));
        });
    }

    [Test]
    public void GoLivePage_StreamSidebar_DoesNotRenderHardcodedLocalDestinationCards()
    {
        SeedSceneState(CreateTwoCameraScene());
        _harness.JsRuntime.SavedValues[StudioSettingsStore.StorageKey] = StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                TransportConnections =
                [
                    AppTestData.GoLive.CreateVdoNinjaConnection()
                ],
                DistributionTargets =
                [
                    AppTestData.GoLive.CreateYoutubeTarget()
                ]
            }
        };

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.ProviderCard(GoLiveTargetCatalog.TargetIds.VdoNinja)));
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.ProviderCard(GoLiveTargetCatalog.TargetIds.Youtube)));
            Assert.Empty(cut.FindAll($"[data-test='{UiTestIds.GoLive.ProviderCard(GoLiveTargetCatalog.TargetIds.Recording)}']"));
        });
    }

    [Test]
    public void GoLivePage_SelectsSecondCameraForCanvas()
    {
        SeedSceneState(CreateTwoCameraScene());

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() =>
            Assert.Equal(2, cut.FindAll($"[data-test^='{UiTestIds.GoLive.SourceCameraSelect(string.Empty)}']").Count));

        cut.FindByTestId(UiTestIds.GoLive.SourceCameraSelect(AppTestData.Camera.SecondSourceId)).Click();

        cut.WaitForAssertion(() =>
            Assert.Equal(
                AppTestData.Camera.SideCamera,
                cut.FindByTestId(UiTestIds.GoLive.SelectedSourceLabel).TextContent.Trim()));
    }

    [Test]
    public void GoLivePage_LoadsRemoteGuestSourcesFromEnabledTransportConnections()
    {
        SeedSceneState(CreateTwoCameraScene());
        _harness.JsRuntime.SeedRemoteSources(
            GoLiveOutputRuntimeContract.SessionId,
            (
                AppTestData.GoLive.RemoteLiveKitConnectionId,
                AppTestData.GoLive.RemoteLiveKitSourceId,
                AppTestData.GoLive.RemoteLiveKitSourceLabel,
                StreamingPlatformKind.LiveKit,
                AppTestData.GoLive.LiveKitRoom,
                AppTestData.GoLive.LiveKitServer,
                true));
        _harness.JsRuntime.SavedValues[StudioSettingsStore.StorageKey] = StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                TransportConnections =
                [
                    AppTestData.GoLive.CreateLiveKitSourceConnection()
                ]
            }
        };

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() =>
            Assert.Contains(
                AppTestData.GoLive.RemoteLiveKitSourceLabel,
                cut.FindByTestId(UiTestIds.GoLive.SourceCameraSelect(AppTestData.GoLive.RemoteLiveKitSourceId)).TextContent,
                StringComparison.Ordinal));

        cut.FindByTestId(UiTestIds.GoLive.RoomTab).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(
                AppTestData.GoLive.RemoteLiveKitSourceLabel,
                cut.FindByTestId(UiTestIds.GoLive.RoomParticipant(AppTestData.GoLive.RemoteLiveKitSourceId)).TextContent,
                StringComparison.Ordinal);
        });
    }

    [Test]
    public void GoLivePage_PrunesAnonymousPersistedSources_AndExposesCameraDiagnostics()
    {
        var sceneState = new MediaSceneState(
            [
                new SceneCameraSource(
                    "invalid-source",
                    string.Empty,
                    string.Empty,
                    new MediaSourceTransform(IncludeInOutput: true)),
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

        SeedSceneState(sceneState);

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() =>
        {
            var sourceCards = cut.FindAll($"article[data-test^='{UiTestIds.GoLive.SourceCamera(string.Empty)}']");
            Assert.Equal(2, sourceCards.Count);
            Assert.DoesNotContain(sourceCards, card => string.IsNullOrWhiteSpace(card.GetAttribute(UiTestIds.GoLive.SourceIdAttribute)));
            Assert.DoesNotContain(sourceCards, card => string.IsNullOrWhiteSpace(card.GetAttribute(UiTestIds.GoLive.SourceDeviceIdAttribute)));
        });
    }

    [Test]
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
            Assert.Empty(cut.FindAll($"[data-test='{UiTestIds.GoLive.ProviderCard(GoLiveTargetCatalog.TargetIds.Recording)}']"));
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

    [Test]
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

    private string Text(string key) =>
        Services.GetRequiredService<IStringLocalizer<SharedResource>>()[key];
}
