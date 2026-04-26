using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Core.Abstractions;
using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Streaming;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Settings.Models;
using PrompterOne.Shared.Storage;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

[NotInParallel]
public sealed class GoLiveSessionInteractionTests : BunitContext
{
    private const int RecordingAudioBitrateKbps = 256;
    private const int RecordingVideoBitrateKbps = 12000;
    private const string RotateLocalRecordingTakeInteropMethod = GoLiveOutputInteropMethodNames.RotateLocalRecordingTake;
    private const string SceneSettingsStorageKey = BrowserAppSettingsKeys.SceneSettings;
    private const string StartLocalRecordingInteropMethod = GoLiveOutputInteropMethodNames.StartLocalRecording;
    private const string StreamingResolutionLabel = "1920 × 1080";
    private const string VideoFrameRateLabel = "30 FPS";

    private readonly AppHarness _harness;

    public GoLiveSessionInteractionTests()
    {
        _harness = TestHarnessFactory.Create(this);
    }

    [Test]
    public async Task GoLivePage_StartStream_UsesSelectedCameraAsActiveProgramSource()
    {
        SeedSceneState(CreateTwoCameraScene());
        SeedStudioSettings(StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                TransportConnections =
                [
                    AppTestData.GoLive.CreateVdoNinjaConnection()
                ]
            }
        });
        Services.GetRequiredService<NavigationManager>().NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.Page)));
        await cut.InvokeAsync(() => cut.FindByTestId(UiTestIds.GoLive.SourceCameraSelect(AppTestData.Camera.SecondSourceId)).Click());
        await cut.InvokeAsync(() => cut.FindByTestId(UiTestIds.GoLive.StartStream).Click());

        cut.WaitForAssertion(() =>
        {
            var session = Services.GetRequiredService<GoLiveSessionService>().State;
            Assert.True(session.IsStreamActive);
            Assert.Equal(AppTestData.Camera.SecondSourceId, session.ActiveSourceId);
            Assert.Equal(AppTestData.Camera.SideCamera, session.ActiveSourceLabel);
        });
    }

    [Test]
    public void GoLivePage_Load_HidesLocalOutputTogglesFromRuntimeSidebar()
    {
        SeedSceneState(CreateTwoCameraScene());
        Services.GetRequiredService<NavigationManager>().NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll($"[data-test='{UiTestIds.GoLive.RecordingToggle}']"));
        });
    }

    [Test]
    public void GoLivePage_LocalRecordingControls_ShowBrowserLocalStatesAndToggleRecording()
    {
        SeedSceneState(CreateTwoCameraScene());
        SeedStudioSettings(StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                Recording = new RecordingProfile(IsEnabled: true)
            }
        });

        Services.GetRequiredService<NavigationManager>().NavigateTo(AppTestData.Routes.GoLiveDemo);
        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.LocalRecordingControls));
            Assert.Contains("Browser-local", cut.FindByTestId(UiTestIds.GoLive.LocalRecordingStatus).TextContent, StringComparison.Ordinal);
            Assert.Equal("stopped", cut.FindByTestId(UiTestIds.GoLive.LocalRecordingVideoButton).GetAttribute("data-state"));
            Assert.Equal("stopped", cut.FindByTestId(UiTestIds.GoLive.LocalRecordingAudioButton).GetAttribute("data-state"));
        });

        cut.FindByTestId(UiTestIds.GoLive.LocalRecordingVideoButton).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(
                _harness.JsRuntime.Invocations,
                invocation => string.Equals(invocation, StartLocalRecordingInteropMethod, StringComparison.Ordinal));
            Assert.Equal("recording", cut.FindByTestId(UiTestIds.GoLive.LocalRecordingVideoButton).GetAttribute("data-state"));
            Assert.Equal("recording", cut.FindByTestId(UiTestIds.GoLive.LocalRecordingAudioButton).GetAttribute("data-state"));
        });
    }

    [Test]
    public void GoLivePage_StartStream_WithVdoNinjaArmed_CallsVdoNinjaOutputInterop()
    {
        SeedSceneState(CreateTwoCameraScene());
        SeedStudioSettings(StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                TransportConnections =
                [
                    AppTestData.GoLive.CreateVdoNinjaConnection()
                ]
            }
        });

        Services.GetRequiredService<NavigationManager>().NavigateTo(AppTestData.Routes.GoLiveDemo);
        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.Page)));
        cut.FindByTestId(UiTestIds.GoLive.StartStream).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(GoLiveOutputInteropMethodNames.StartVdoNinjaSession, _harness.JsRuntime.Invocations);
            Assert.DoesNotContain(GoLiveOutputInteropMethodNames.StartLiveKitSession, _harness.JsRuntime.Invocations);
        });
    }

    [Test]
    public void GoLivePage_StartStream_WithLiveKitArmed_CallsLiveKitOutputInterop_WhenVdoNinjaIsNotConfigured()
    {
        SeedSceneState(CreateTwoCameraScene());
        SeedStudioSettings(StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                TransportConnections =
                [
                    AppTestData.GoLive.CreateLiveKitConnection()
                ]
            }
        });

        Services.GetRequiredService<NavigationManager>().NavigateTo(AppTestData.Routes.GoLiveDemo);
        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.Page)));
        cut.FindByTestId(UiTestIds.GoLive.StartStream).Click();

        cut.WaitForAssertion(() =>
            Assert.Contains(GoLiveOutputInteropMethodNames.StartLiveKitSession, _harness.JsRuntime.Invocations));
    }

    [Test]
    public void GoLivePage_StartStream_WithVdoNinjaAndLiveKitArmed_StartsBothPublishTransports()
    {
        SeedSceneState(CreateTwoCameraScene());
        SeedStudioSettings(StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                TransportConnections =
                [
                    AppTestData.GoLive.CreateVdoNinjaConnection(),
                    AppTestData.GoLive.CreateLiveKitConnection()
                ]
            }
        });

        Services.GetRequiredService<NavigationManager>().NavigateTo(AppTestData.Routes.GoLiveDemo);
        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.Page)));
        cut.FindByTestId(UiTestIds.GoLive.StartStream).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(GoLiveOutputInteropMethodNames.StartVdoNinjaSession, _harness.JsRuntime.Invocations);
            Assert.Contains(GoLiveOutputInteropMethodNames.StartLiveKitSession, _harness.JsRuntime.Invocations);
        });
    }

    [Test]
    public async Task GoLivePage_SwitchProgramSource_WhileVdoNinjaActive_RefreshesOutputSessionDevices()
    {
        SeedSceneState(CreateTwoCameraScene());
        SeedStudioSettings(StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                TransportConnections =
                [
                    AppTestData.GoLive.CreateVdoNinjaConnection()
                ]
            }
        });

        Services.GetRequiredService<NavigationManager>().NavigateTo(AppTestData.Routes.GoLiveDemo);
        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.Page)));
        cut.FindByTestId(UiTestIds.GoLive.StartStream).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.True(Services.GetRequiredService<GoLiveSessionService>().State.IsStreamActive);
            Assert.Contains(
                _harness.JsRuntime.Invocations,
                invocation => string.Equals(invocation, GoLiveOutputInteropMethodNames.StartVdoNinjaSession, StringComparison.Ordinal));
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.SourceCameraSelect(AppTestData.Camera.SecondSourceId)));
        });

        var invocationCountBeforeSwitch = _harness.JsRuntime.Invocations.Count;

        await cut.InvokeAsync(() => cut.FindByTestId(UiTestIds.GoLive.SourceCameraSelect(AppTestData.Camera.SecondSourceId)).Click());
        cut.WaitForAssertion(() =>
            Assert.Equal(
                AppTestData.Camera.SecondSourceId,
                Services.GetRequiredService<GoLiveSessionService>().State.SelectedSourceId));
        await cut.InvokeAsync(() => cut.FindByTestId(UiTestIds.GoLive.TakeToAir).Click());

        cut.WaitForAssertion(() =>
            Assert.Contains(
                _harness.JsRuntime.Invocations.Skip(invocationCountBeforeSwitch),
                invocation => string.Equals(invocation, GoLiveOutputInteropMethodNames.UpdateSessionDevices, StringComparison.Ordinal)));
    }

    [Test]
    public void GoLivePage_StartRecording_WithRecordingArmed_CallsLocalRecordingInterop()
    {
        SeedSceneState(CreateTwoCameraScene());
        SeedStudioSettings(StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                Recording = new RecordingProfile(IsEnabled: true)
            }
        });

        Services.GetRequiredService<NavigationManager>().NavigateTo(AppTestData.Routes.GoLiveDemo);
        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.Page)));
        cut.FindByTestId(UiTestIds.GoLive.StartRecording).Click();

        Assert.Contains(
            _harness.JsRuntime.Invocations,
            invocation => string.Equals(invocation, StartLocalRecordingInteropMethod, StringComparison.Ordinal));
        Assert.True(Services.GetRequiredService<GoLiveSessionService>().State.IsRecordingActive);
    }

    [Test]
    public void GoLivePage_StartStream_WithRelayOnlyDestination_DoesNotMarkSessionLive()
    {
        SeedSceneState(CreateTwoCameraScene());
        SeedStudioSettings(StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                DistributionTargets =
                [
                    AppTestData.GoLive.CreateYoutubeTarget()
                ]
            }
        });

        Services.GetRequiredService<NavigationManager>().NavigateTo(AppTestData.Routes.GoLiveDemo);
        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.Page)));
        cut.FindByTestId(UiTestIds.GoLive.StartStream).Click();

        var session = Services.GetRequiredService<GoLiveSessionService>().State;
        Assert.False(session.IsStreamActive);
    }

    [Test]
    public void GoLivePage_StartRecording_PassesSceneCompositionAndExportPreferencesToRecordingInterop()
    {
        SeedSceneState(CreateSceneWithTwoAudioInputs());
        SeedRecordingPreferences(SettingsPagePreferences.Default with
        {
            RecordingContainer = RecordingPreferenceCatalog.Containers.Mp4,
            RecordingVideoCodec = RecordingPreferenceCatalog.VideoCodecs.H264Avc,
            RecordingVideoBitrateKbps = RecordingVideoBitrateKbps,
            RecordingAudioCodec = RecordingPreferenceCatalog.AudioCodecs.Aac,
            RecordingAudioBitrateKbps = RecordingAudioBitrateKbps,
            RecordingAudioSampleRate = RecordingPreferenceCatalog.SampleRates.Khz48,
            RecordingAudioChannels = RecordingPreferenceCatalog.AudioChannels.Mono
        });
        SeedStudioSettings(StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                Recording = new RecordingProfile(IsEnabled: true),
                ProgramCapture = new ProgramCaptureProfile(ResolutionPreset: StreamingResolutionPreset.FullHd1080p30)
            }
        });

        Services.GetRequiredService<NavigationManager>().NavigateTo(AppTestData.Routes.GoLiveDemo);
        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.Page)));
        cut.FindByTestId(UiTestIds.GoLive.StartRecording).Click();

        var invocation = Assert.Single(
            _harness.JsRuntime.InvocationRecords,
            record => string.Equals(record.Identifier, StartLocalRecordingInteropMethod, StringComparison.Ordinal));
        var request = Assert.IsType<GoLiveOutputRuntimeRequest>(invocation.Arguments[1]);

        Assert.Equal(AppTestData.Camera.FirstSourceId, request.PrimarySourceId);
        Assert.Equal(2, request.VideoSources.Count);
        Assert.Equal(1, request.VideoSources.Count(source => source.IsRenderable));
        Assert.Equal(2, request.AudioInputs.Count);
        Assert.Equal(StreamingResolutionLabel, request.ProgramVideo.ResolutionLabel);
        Assert.Equal(VideoFrameRateLabel, request.ProgramVideo.FrameRateLabel);
        Assert.Equal(RecordingPreferenceCatalog.Containers.Mp4, request.Recording.ContainerLabel);
        Assert.Equal(RecordingPreferenceCatalog.VideoCodecs.H264Avc, request.Recording.VideoCodecLabel);
        Assert.Equal(RecordingPreferenceCatalog.AudioCodecs.Aac, request.Recording.AudioCodecLabel);
        Assert.Equal(RecordingVideoBitrateKbps, request.Recording.VideoBitrateKbps);
        Assert.Equal(RecordingAudioBitrateKbps, request.Recording.AudioBitrateKbps);
        Assert.Equal(1, request.Recording.AudioChannelCount);
        Assert.False(request.Recording.PreferFilePicker);
    }

    [Test]
    public async Task GoLivePage_RecordingBlockNavigation_WhileRecording_RotatesTakeWithNextBlockFileStem()
    {
        SeedSceneState(CreateTwoCameraScene());
        SeedStudioSettings(StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                Recording = new RecordingProfile(IsEnabled: true)
            }
        });

        Services.GetRequiredService<NavigationManager>().NavigateTo(AppTestData.Routes.GoLiveLeadership);
        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.Page)));
        await cut.InvokeAsync(() => cut.FindByTestId(UiTestIds.GoLive.RecordingBlockContextToggle).Click());
        await cut.InvokeAsync(() => cut.FindByTestId(UiTestIds.GoLive.StartRecording).Click());

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.RecordingBlockActive));
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.RecordingBlockNext));
        });
        var firstBlockTitle = GetCueTitle(cut.FindByTestId(UiTestIds.GoLive.RecordingBlockActive));
        var secondBlockTitle = GetCueTitle(cut.FindByTestId(UiTestIds.GoLive.RecordingBlockNext));

        var startInvocation = Assert.Single(
            _harness.JsRuntime.InvocationRecords,
            record => string.Equals(record.Identifier, StartLocalRecordingInteropMethod, StringComparison.Ordinal));
        var startRequest = Assert.IsType<GoLiveOutputRuntimeRequest>(startInvocation.Arguments[1]);
        Assert.Contains("take 01", startRequest.Recording.FileStem, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(firstBlockTitle, startRequest.Recording.FileStem, StringComparison.Ordinal);

        await cut.InvokeAsync(() => cut.FindByTestId(UiTestIds.GoLive.RecordingBlockNextControl).Click());

        var rotateInvocation = Assert.Single(
            _harness.JsRuntime.InvocationRecords,
            record => string.Equals(record.Identifier, RotateLocalRecordingTakeInteropMethod, StringComparison.Ordinal));
        var rotateRequest = Assert.IsType<GoLiveOutputRuntimeRequest>(rotateInvocation.Arguments[1]);
        Assert.Contains("take 02", rotateRequest.Recording.FileStem, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(secondBlockTitle, rotateRequest.Recording.FileStem, StringComparison.Ordinal);
        Assert.DoesNotContain(firstBlockTitle, rotateRequest.Recording.FileStem, StringComparison.Ordinal);
    }

    [Test]
    public async Task GoLivePage_StopRecording_PersistsBlockTakeWithoutChangingScriptSource()
    {
        SeedSceneState(CreateTwoCameraScene());
        SeedStudioSettings(StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                Recording = new RecordingProfile(IsEnabled: true)
            }
        });

        Services.GetRequiredService<NavigationManager>().NavigateTo(AppTestData.Routes.GoLiveLeadership);
        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.Page)));
        var session = Services.GetRequiredService<IScriptSessionService>().State;
        var sourceBeforeRecording = session.Text;

        await cut.InvokeAsync(() => cut.FindByTestId(UiTestIds.GoLive.RecordingBlockContextToggle).Click());
        await cut.InvokeAsync(() => cut.FindByTestId(UiTestIds.GoLive.StartRecording).Click());
        await cut.InvokeAsync(() => cut.FindByTestId(UiTestIds.GoLive.StartRecording).Click());

        var takes = await Services.GetRequiredService<GoLiveBlockTakeStore>().LoadAsync(session.ScriptId);
        Assert.Single(takes);
        Assert.Equal(sourceBeforeRecording, Services.GetRequiredService<IScriptSessionService>().State.Text);
    }

    [Test]
    public void GoLivePage_StartRecording_WithLocalFilePreference_PrefersFilePickerExport()
    {
        SeedSceneState(CreateSceneWithTwoAudioInputs());
        SeedRecordingPreferences(SettingsPagePreferences.Default with
        {
            RecordingFolder = RecordingPreferenceCatalog.LocationLabels.LocalFile
        });
        SeedStudioSettings(StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                Recording = new RecordingProfile(IsEnabled: true)
            }
        });

        Services.GetRequiredService<NavigationManager>().NavigateTo(AppTestData.Routes.GoLiveDemo);
        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.Page)));
        cut.FindByTestId(UiTestIds.GoLive.StartRecording).Click();

        var invocation = Assert.Single(
            _harness.JsRuntime.InvocationRecords,
            record => string.Equals(record.Identifier, StartLocalRecordingInteropMethod, StringComparison.Ordinal));
        var request = Assert.IsType<GoLiveOutputRuntimeRequest>(invocation.Arguments[1]);

        Assert.True(request.Recording.PreferFilePicker);
    }

    [Test]
    public void GoLivePage_StartRecording_WithPictureInPictureLayout_KeepsOverlaySourcesRenderable()
    {
        SeedSceneState(CreateSceneWithTwoAudioInputs());
        SeedStudioSettings(StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                Recording = new RecordingProfile(IsEnabled: true),
                ProgramCapture = new ProgramCaptureProfile(ResolutionPreset: StreamingResolutionPreset.FullHd1080p30)
            }
        });

        Services.GetRequiredService<NavigationManager>().NavigateTo(AppTestData.Routes.GoLiveDemo);
        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.Page)));
        cut.FindByTestId(UiTestIds.GoLive.LayoutPictureInPicture).Click();
        cut.FindByTestId(UiTestIds.GoLive.StartRecording).Click();

        var invocation = Assert.Single(
            _harness.JsRuntime.InvocationRecords,
            record => string.Equals(record.Identifier, StartLocalRecordingInteropMethod, StringComparison.Ordinal));
        var request = Assert.IsType<GoLiveOutputRuntimeRequest>(invocation.Arguments[1]);

        Assert.Equal(2, request.VideoSources.Count(source => source.IsRenderable));
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
                    new MediaSourceTransform(IncludeInOutput: true))
            ],
            AppTestData.Microphone.PrimaryDeviceId,
            AppTestData.Scripts.BroadcastMic,
            new AudioBusState([new AudioInputState(AppTestData.Microphone.PrimaryDeviceId, AppTestData.Scripts.BroadcastMic)]));

    private static MediaSceneState CreateSceneWithTwoAudioInputs() =>
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
                    new MediaSourceTransform(X: 0.18, Y: 0.18, Width: 0.22, Height: 0.22, IncludeInOutput: true, MirrorHorizontal: false))
            ],
            AppTestData.Microphone.PrimaryDeviceId,
            AppTestData.Scripts.BroadcastMic,
            new AudioBusState(
                [
                    new AudioInputState(AppTestData.Microphone.PrimaryDeviceId, AppTestData.Scripts.BroadcastMic),
                    new AudioInputState("mic-guest", "Guest mic", Gain: 0.72, DelayMs: 120, RouteTarget: AudioRouteTarget.Stream)
                ]));

    private void SeedSceneState(MediaSceneState sceneState)
    {
        _harness.JsRuntime.SavedJsonValues[SceneSettingsStorageKey] = JsonSerializer.Serialize(sceneState);
    }

    private void SeedStudioSettings(StudioSettings settings)
    {
        _harness.JsRuntime.SavedValues[StudioSettingsStore.StorageKey] = settings;
    }

    private void SeedRecordingPreferences(SettingsPagePreferences preferences)
    {
        _harness.JsRuntime.SavedValues[SettingsPagePreferences.StorageKey] = preferences;
    }

    private static string GetCueTitle(AngleSharp.Dom.IElement element) =>
        element.QuerySelector("strong")?.TextContent.Trim() ?? string.Empty;
}
