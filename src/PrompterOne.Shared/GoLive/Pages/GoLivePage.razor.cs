using Microsoft.AspNetCore.Components;
using PrompterOne.Core.Abstractions;
using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.GoLive.Models;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Services.Diagnostics;
using PrompterOne.Shared.Settings.Models;

namespace PrompterOne.Shared.Pages;

public partial class GoLivePage : ComponentBase, IDisposable, IAsyncDisposable
{
    private const string ActiveStateValue = "active";
    private const string InactiveStateValue = "inactive";

    [Inject] private AppBootstrapper Bootstrapper { get; set; } = null!;
    [Inject] private AppShellService Shell { get; set; } = null!;
    [Inject] private GoLiveSessionService GoLiveSession { get; set; } = null!;
    [Inject] private GoLiveOutputRuntimeService GoLiveOutputRuntime { get; set; } = null!;
    [Inject] private GoLiveRemoteSourceRuntimeService GoLiveRemoteSourceRuntime { get; set; } = null!;
    [Inject] private BrowserMediaCaptureCapabilitiesService BrowserMediaCaptureCapabilityService { get; set; } = null!;
    [Inject] private MicrophoneLevelInterop MicrophoneLevelInterop { get; set; } = null!;
    [Inject] private StreamingPublishDescriptorResolver StreamingDescriptorResolver { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private UiDiagnosticsService Diagnostics { get; set; } = null!;
    [Inject] private IMediaDeviceService MediaDeviceService { get; set; } = null!;
    [Inject] private IMediaSceneService MediaSceneService { get; set; } = null!;
    [Inject] private IScriptRepository ScriptRepository { get; set; } = null!;
    [Inject] private IScriptSessionService SessionService { get; set; } = null!;
    [Inject] private IUserSettingsStore SettingsStore { get; set; } = null!;
    [Inject] private StudioSettingsStore StudioSettingsStore { get; set; } = null!;

    [Parameter]
    [SupplyParameterFromQuery(Name = AppRoutes.ScriptIdQueryKey)]
    public string? ScriptId { get; set; }

    private Task? _bootstrapTask;
    private readonly SemaphoreSlim _interactionGate = new(1, 1);
    private IReadOnlyList<MediaDeviceInfo> _mediaDevices = [];
    private BrowserMediaCaptureCapabilities _captureCapabilities = BrowserMediaCaptureCapabilities.Default;
    private bool _focusScreenAfterRender = true;
    private bool _loadState = true;
    private SettingsPagePreferences _recordingPreferences = SettingsPagePreferences.Default;
    private ElementReference _screenRoot;
    private string _sessionSubtitle = string.Empty;
    private string _sessionTitle = ScriptWorkspaceState.UntitledScriptTitle;
    private StudioSettings _studioSettings = StudioSettings.Default;

    private bool HasPrimaryMicrophone => !string.IsNullOrWhiteSpace(MediaSceneService.State.PrimaryMicrophoneId);

    private SceneCameraSource? PreviewCamera =>
        AvailableSceneSources.FirstOrDefault(camera => camera.Transform.Visible && camera.Transform.IncludeInOutput)
        ?? AvailableSceneSources.FirstOrDefault(camera => camera.Transform.Visible)
        ?? (AvailableSceneSources.Count > 0 ? AvailableSceneSources[0] : null);

    private string PrimaryMicrophoneLabel => string.IsNullOrWhiteSpace(MediaSceneService.State.PrimaryMicrophoneLabel)
        ? Text(GoLiveText.Audio.NoMicrophoneLabel)
        : MediaDeviceLabelSanitizer.Sanitize(MediaSceneService.State.PrimaryMicrophoneLabel);

    private string BackRoute => Shell.GetGoLiveBackRoute();

    private string ScreenTitle => Text(GoLiveText.Chrome.ScreenTitle);

    private string PrimaryMicrophoneRoute
    {
        get
        {
            var route = MediaSceneService.State.AudioBus.Inputs
                .FirstOrDefault(input => string.Equals(input.DeviceId, MediaSceneService.State.PrimaryMicrophoneId, StringComparison.Ordinal))
                ?.RouteTarget;

            return route is null
                ? Text(GoLiveText.Audio.DefaultMicrophoneRouteLabel)
                : FormatRouteTarget(route.Value);
        }
    }

    private IReadOnlyList<SceneCameraSource> SceneCameras => MediaSceneService.State.Cameras;

    protected override Task OnParametersSetAsync()
    {
        _bootstrapTask = null;
        _focusScreenAfterRender = true;
        _loadState = true;
        return Task.CompletedTask;
    }
}
