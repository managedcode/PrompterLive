using Microsoft.AspNetCore.Components;
using PrompterOne.Core.Abstractions;
using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.GoLive.Models;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Services.Diagnostics;
using PrompterOne.Shared.Settings.Models;

namespace PrompterOne.Shared.Pages;

public partial class GoLivePage : ComponentBase
{
    [Inject] private AppBootstrapper Bootstrapper { get; set; } = null!;
    [Inject] private AppShellService Shell { get; set; } = null!;
    [Inject] private GoLiveSessionService GoLiveSession { get; set; } = null!;
    [Inject] private GoLiveOutputRuntimeService GoLiveOutputRuntime { get; set; } = null!;
    [Inject] private StreamingPublishDescriptorResolver StreamingDescriptorResolver { get; set; } = null!;
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
    private string _sessionSubtitle = GoLiveText.Chrome.StreamingSubtitle;
    private string _sessionTitle = ScriptWorkspaceState.UntitledScriptTitle;
    private StudioSettings _studioSettings = StudioSettings.Default;

    private bool HasPrimaryMicrophone => !string.IsNullOrWhiteSpace(MediaSceneService.State.PrimaryMicrophoneId);

    private SceneCameraSource? PreviewCamera =>
        SceneCameras.FirstOrDefault(camera => camera.Transform.Visible && camera.Transform.IncludeInOutput)
        ?? SceneCameras.FirstOrDefault(camera => camera.Transform.Visible)
        ?? (SceneCameras.Count > 0 ? SceneCameras[0] : null);

    private string PrimaryMicrophoneLabel => MediaSceneService.State.PrimaryMicrophoneLabel ?? GoLiveText.Audio.NoMicrophoneLabel;

    private string BackRoute => Shell.GetGoLiveBackRoute();

    private static string ScreenTitle => GoLiveText.Chrome.ScreenTitle;

    private string PrimaryMicrophoneRoute
    {
        get
        {
            var route = MediaSceneService.State.AudioBus.Inputs
                .FirstOrDefault(input => string.Equals(input.DeviceId, MediaSceneService.State.PrimaryMicrophoneId, StringComparison.Ordinal))
                ?.RouteTarget;

            return route is null
                ? GoLiveText.Audio.DefaultMicrophoneRouteLabel
                : FormatRouteTarget(route.Value);
        }
    }

    private IReadOnlyList<SceneCameraSource> SceneCameras => MediaSceneService.State.Cameras;

    protected override Task OnParametersSetAsync()
    {
        _bootstrapTask = null;
        _loadState = true;
        return Task.CompletedTask;
    }
}
