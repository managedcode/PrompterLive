using Microsoft.AspNetCore.Components;
using PrompterOne.Core.Abstractions;
using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Services.Diagnostics;
using PrompterOne.Shared.Settings.Models;

namespace PrompterOne.Shared.Pages;

public partial class GoLivePage : ComponentBase
{
    private const string DefaultMicRouteLabel = "Monitor + Stream";
    private const string GoLiveLoadMessage = "Unable to prepare live routing right now.";
    private const string GoLiveLoadOperation = "Go Live load";
    private const string NoScriptProgressLabel = "No script loaded";
    private const string GoLiveSceneMessage = "Unable to save the current live scene.";
    private const string GoLiveSceneOperation = "Go Live save scene";
    private const string GoLiveStudioMessage = "Unable to save live routing settings.";
    private const string GoLiveStudioOperation = "Go Live save studio";
    private const string NoMicrophoneLabel = "No microphone";
    private const string StreamingSubtitle = "Program routing";

    [Inject] private AppBootstrapper Bootstrapper { get; set; } = null!;
    [Inject] private AppShellService Shell { get; set; } = null!;
    [Inject] private GoLiveSessionService GoLiveSession { get; set; } = null!;
    [Inject] private GoLiveOutputRuntimeService GoLiveOutputRuntime { get; set; } = null!;
    [Inject] private UiDiagnosticsService Diagnostics { get; set; } = null!;
    [Inject] private IMediaDeviceService MediaDeviceService { get; set; } = null!;
    [Inject] private IMediaSceneService MediaSceneService { get; set; } = null!;
    [Inject] private IScriptRepository ScriptRepository { get; set; } = null!;
    [Inject] private IScriptSessionService SessionService { get; set; } = null!;
    [Inject] private IUserSettingsStore SettingsStore { get; set; } = null!;
    [Inject] private StudioSettingsStore StudioSettingsStore { get; set; } = null!;

    [SupplyParameterFromQuery(Name = "id")]
    public string? ScriptId { get; set; }

    private Task? _bootstrapTask;
    private readonly SemaphoreSlim _interactionGate = new(1, 1);
    private IReadOnlyList<MediaDeviceInfo> _mediaDevices = [];
    private bool _loadState = true;
    private SettingsPagePreferences _recordingPreferences = SettingsPagePreferences.Default;
    private string _screenSubtitle = StreamingSubtitle;
    private string _screenTitle = ScriptWorkspaceState.UntitledScriptTitle;
    private StudioSettings _studioSettings = StudioSettings.Default;

    private bool HasAnyLiveOutput =>
        _studioSettings.Streaming.ObsVirtualCameraEnabled
        || _studioSettings.Streaming.NdiOutputEnabled
        || _studioSettings.Streaming.LocalRecordingEnabled
        || _studioSettings.Streaming.LiveKitEnabled
        || _studioSettings.Streaming.VdoNinjaEnabled
        || _studioSettings.Streaming.YoutubeEnabled
        || _studioSettings.Streaming.TwitchEnabled
        || _studioSettings.Streaming.CustomRtmpEnabled;

    private bool HasPrimaryMicrophone => !string.IsNullOrWhiteSpace(MediaSceneService.State.PrimaryMicrophoneId);

    private bool HasScriptContext => !string.IsNullOrWhiteSpace(SessionService.State.ScriptId);

    private string CurrentScriptProgressLabel => HasScriptContext
        ? _screenSubtitle
        : NoScriptProgressLabel;

    private string LearnRoute => HasScriptContext
        ? AppRoutes.LearnWithId(SessionService.State.ScriptId)
        : AppRoutes.Learn;

    private SceneCameraSource? PreviewCamera =>
        SceneCameras.FirstOrDefault(camera => camera.Transform.Visible && camera.Transform.IncludeInOutput)
        ?? SceneCameras.FirstOrDefault(camera => camera.Transform.Visible)
        ?? (SceneCameras.Count > 0 ? SceneCameras[0] : null);

    private string PrimaryMicrophoneLabel => MediaSceneService.State.PrimaryMicrophoneLabel ?? NoMicrophoneLabel;

    private string PrimaryMicrophoneRoute
    {
        get
        {
            var route = MediaSceneService.State.AudioBus.Inputs
                .FirstOrDefault(input => string.Equals(input.DeviceId, MediaSceneService.State.PrimaryMicrophoneId, StringComparison.Ordinal))
                ?.RouteTarget;

            return route is null
                ? DefaultMicRouteLabel
                : FormatRouteTarget(route.Value);
        }
    }

    private string ReadRoute => HasScriptContext
        ? AppRoutes.TeleprompterWithId(SessionService.State.ScriptId)
        : AppRoutes.Teleprompter;

    private IReadOnlyList<SceneCameraSource> SceneCameras => MediaSceneService.State.Cameras;

    protected override Task OnParametersSetAsync()
    {
        _bootstrapTask = null;
        _loadState = true;
        return Task.CompletedTask;
    }
}
