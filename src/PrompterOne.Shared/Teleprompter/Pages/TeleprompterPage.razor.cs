using Microsoft.AspNetCore.Components;
using PrompterOne.Core.Abstractions;
using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Core.Services;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Services.Diagnostics;

namespace PrompterOne.Shared.Pages;

public partial class TeleprompterPage : IAsyncDisposable
{
    private const int DefaultReaderFontSize = 36;
    private const int DefaultReaderFocalPointPercent = 30;
    private const int MaxReaderGroupCharacterCount = 24;
    private const int MaxReaderGroupWordCount = 5;
    private const string LoadReaderMessage = "Unable to prepare teleprompter playback.";
    private const string LoadReaderOperation = "Teleprompter load";
    private const int ReaderBackwardStep = -1;
    private const int ReaderCardBackwardStep = -1;
    private const int ReaderCardForwardStep = 1;
    private const int ReaderCountdownPreDelayMilliseconds = 600;
    private const int ReaderCountdownStepMilliseconds = 700;
    private const int ReaderFirstWordDelayMilliseconds = 700;
    private const int ReaderFontStep = 4;
    private const int ReaderForwardStep = 1;
    private const int ReaderGuideActiveDurationMilliseconds = 800;
    private const int ReaderMaxFontSize = 56;
    private const int ReaderMaxTextWidth = 1100;
    private const int ReaderMaxFocalPointPercent = 55;
    private const int ReaderMinFontSize = 24;
    private const int ReaderMinTextWidth = 400;
    private const int ReaderMinFocalPointPercent = 15;
    private const int DefaultReaderTextWidth = ReaderMaxTextWidth;

    [Inject] private AppBootstrapper Bootstrapper { get; set; } = null!;
    [Inject] private CameraPreviewInterop CameraPreviewInterop { get; set; } = null!;
    [Inject] private ScriptCompiler Compiler { get; set; } = null!;
    [Inject] private UiDiagnosticsService Diagnostics { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private AppShellService Shell { get; set; } = null!;
    [Inject] private IMediaDeviceService MediaDeviceService { get; set; } = null!;
    [Inject] private IMediaSceneService MediaSceneService { get; set; } = null!;
    [Inject] private TpsParser Parser { get; set; } = null!;
    [Inject] private IScriptRepository ScriptRepository { get; set; } = null!;
    [Inject] private IScriptSessionService SessionService { get; set; } = null!;
    [Inject] private StudioSettingsStore StudioSettingsStore { get; set; } = null!;
    [Inject] private TeleprompterReaderInterop ReaderInterop { get; set; } = null!;
    [Inject] private IUserSettingsStore UserSettingsStore { get; set; } = null!;

    [SupplyParameterFromQuery(Name = AppRoutes.ScriptIdQueryKey)]
    public string? ScriptId { get; set; }

    private CancellationTokenSource? _readerPlaybackCts;
    private ElementReference _screenRoot;
    private ReaderCameraLayerViewModel _cameraLayer = ReaderCameraLayerViewModel.Placeholder;
    private IReadOnlyList<ReaderCardViewModel> _cards = [];
    private StudioSettings _studioSettings = StudioSettings.Default;
    private bool _activateReaderCameraAfterRender;
    private bool _areWidthGuidesActive;
    private bool _focusScreenAfterRender = true;
    private bool _isFocalGuideActive;
    private bool _isReaderCameraActive;
    private bool _isReaderCountdownActive;
    private bool _isReaderPlaying;
    private bool _loadState = true;
    private int _activeReaderCardIndex;
    private int _activeReaderWordIndex;
    private int _readerFontSize = DefaultReaderFontSize;
    private int _readerFocalPointPercent = DefaultReaderFocalPointPercent;
    private int _readerTextWidth = DefaultReaderTextWidth;
    private int _totalDurationMilliseconds = 1000;
    private int _totalSeconds = 1;
    private int? _countdownValue;
    private long _focalGuideVersion;
    private long _widthGuideVersion;
    private string _edgeSectionLabel = string.Empty;
    private string _elapsedLabel = "0:00 / 0:01";
    private string _gradientClass = string.Empty;
    private string _readerProgressFillWidth = "0%";
    private string _screenSubtitle = string.Empty;
    private string _screenTitle = string.Empty;

    protected override Task OnParametersSetAsync()
    {
        StopReaderPlaybackLoop();
        ResetReaderAlignmentState();
        ResetReaderCardTransitionState();
        _loadState = true;
        _focusScreenAfterRender = true;
        return Task.CompletedTask;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_loadState)
        {
            _loadState = false;
            await Diagnostics.RunAsync(
                LoadReaderOperation,
                LoadReaderMessage,
                async () =>
                {
                    await Bootstrapper.EnsureReadyAsync();
                    await EnsureSessionLoadedAsync();
                    await PopulateReaderStateAsync();
                    await PopulateCameraStateAsync();
                    StateHasChanged();
                });
            return;
        }

        if (_focusScreenAfterRender)
        {
            _focusScreenAfterRender = false;
            await _screenRoot.FocusAsync();
        }

        if (_activateReaderCameraAfterRender)
        {
            _activateReaderCameraAfterRender = false;
            await AttachReaderCameraAsync();
        }

        await AlignActiveReaderTextAsync();
        await RestorePendingReaderTextTransitionsAsync();
    }

    private async Task EnsureSessionLoadedAsync()
    {
        if (!string.IsNullOrWhiteSpace(ScriptId))
        {
            var document = await ScriptRepository.GetAsync(ScriptId);
            if (document is not null &&
                !string.Equals(SessionService.State.ScriptId, document.Id, StringComparison.Ordinal))
            {
                await SessionService.OpenAsync(document);
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(SessionService.State.ScriptId))
        {
            return;
        }
    }

    private async Task PopulateReaderStateAsync()
    {
        var nextCards = await BuildReaderCardsAsync();
        ResetReaderAlignmentState();
        ResetReaderCardTransitionState();
        _cards = nextCards;
        _screenTitle = SessionService.State.Title;
        _readerFontSize = NormalizeReaderFontSize(SessionService.State.ReaderSettings.FontScale);
        _readerFocalPointPercent = NormalizeReaderFocalPointPercent(SessionService.State.ReaderSettings.FocalPointPercent);
        _readerTextWidth = NormalizeReaderTextWidth(SessionService.State.ReaderSettings.TextWidth);
        _activeReaderCardIndex = 0;
        _activeReaderWordIndex = -1;
        _isReaderPlaying = false;
        _isReaderCountdownActive = false;
        _countdownValue = null;
        _totalDurationMilliseconds = Math.Max(1000, _cards.Sum(card => card.DurationMilliseconds));
        _totalSeconds = Math.Max(1, (int)Math.Ceiling(_totalDurationMilliseconds / 1000d));
        UpdateReaderDisplayState();
    }

    private async Task PopulateCameraStateAsync()
    {
        _studioSettings = await StudioSettingsStore.LoadAsync();
        var autoStart = SessionService.State.ReaderSettings.ShowCameraScene;
        var devices = await MediaDeviceService.GetDevicesAsync();
        var availableCameras = devices.Where(device => device.Kind == MediaDeviceKind.Camera).ToList();
        var preferredCameraId = _studioSettings.Camera.DefaultCameraId;
        var visibleSceneCameras = MediaSceneService.State.Cameras
            .Where(camera => camera.Transform.Visible)
            .OrderBy(camera => string.Equals(camera.DeviceId, preferredCameraId, StringComparison.Ordinal) ? 0 : 1)
            .ThenByDescending(camera => camera.Transform.ZIndex)
            .ThenBy(camera => camera.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var configuredCamera = availableCameras.FirstOrDefault(device => string.Equals(device.DeviceId, preferredCameraId, StringComparison.Ordinal));

        var primarySceneCamera = visibleSceneCameras.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(preferredCameraId))
        {
            primarySceneCamera = visibleSceneCameras.FirstOrDefault(camera => string.Equals(camera.DeviceId, preferredCameraId, StringComparison.Ordinal))
                ?? primarySceneCamera;
        }

        var shouldUseConfiguredPrimary = configuredCamera is not null &&
            visibleSceneCameras.All(camera => !string.Equals(camera.DeviceId, configuredCamera.DeviceId, StringComparison.Ordinal));

        if (shouldUseConfiguredPrimary)
        {
            var transform = new MediaSourceTransform(MirrorHorizontal: _studioSettings.Camera.MirrorCamera);
            _cameraLayer = new ReaderCameraLayerViewModel(
                ElementId: UiDomIds.Teleprompter.Camera,
                DeviceId: configuredCamera!.DeviceId,
                AutoStart: autoStart,
                Role: "primary",
                Order: 0,
                CssClass: "rd-camera",
                Style: BuildPrimaryCameraStyle(transform),
                TestId: UiTestIds.Teleprompter.CameraBackground);
            _isReaderCameraActive = autoStart;
            _activateReaderCameraAfterRender = _isReaderCameraActive;
            return;
        }

        if (primarySceneCamera is not null)
        {
            _cameraLayer = new ReaderCameraLayerViewModel(
                ElementId: UiDomIds.Teleprompter.Camera,
                DeviceId: primarySceneCamera.DeviceId,
                AutoStart: autoStart,
                Role: "primary",
                Order: 0,
                CssClass: "rd-camera",
                Style: BuildPrimaryCameraStyle(primarySceneCamera.Transform),
                TestId: UiTestIds.Teleprompter.CameraBackground);
            _isReaderCameraActive = autoStart;
            _activateReaderCameraAfterRender = _isReaderCameraActive;
            return;
        }

        var defaultCamera = availableCameras.FirstOrDefault(device => string.Equals(device.DeviceId, preferredCameraId, StringComparison.Ordinal))
            ?? availableCameras.FirstOrDefault(device => device.IsDefault)
            ?? availableCameras.FirstOrDefault();
        var defaultTransform = new MediaSourceTransform(MirrorHorizontal: _studioSettings.Camera.MirrorCamera);

        _cameraLayer = new ReaderCameraLayerViewModel(
            ElementId: UiDomIds.Teleprompter.Camera,
            DeviceId: defaultCamera?.DeviceId ?? string.Empty,
            AutoStart: autoStart && !string.IsNullOrWhiteSpace(defaultCamera?.DeviceId),
            Role: "primary",
            Order: 0,
            CssClass: "rd-camera",
            Style: BuildPrimaryCameraStyle(defaultTransform),
            TestId: UiTestIds.Teleprompter.CameraBackground);
        _isReaderCameraActive = autoStart && !string.IsNullOrWhiteSpace(defaultCamera?.DeviceId);
        _activateReaderCameraAfterRender = _isReaderCameraActive;
    }

    private static int NormalizeReaderFontSize(double fontScale)
    {
        var safeScale = fontScale > 0 ? fontScale : 1d;
        return Math.Clamp((int)Math.Round(DefaultReaderFontSize * safeScale), ReaderMinFontSize, ReaderMaxFontSize);
    }

    private static int NormalizeReaderTextWidth(double textWidthRatio)
    {
        var safeRatio = textWidthRatio > 0 ? textWidthRatio : (double)DefaultReaderTextWidth / ReaderMaxTextWidth;
        return Math.Clamp((int)Math.Round(ReaderMaxTextWidth * safeRatio), ReaderMinTextWidth, ReaderMaxTextWidth);
    }

    private static int NormalizeReaderFocalPointPercent(int focalPointPercent)
    {
        var safePercent = focalPointPercent > 0 ? focalPointPercent : DefaultReaderFocalPointPercent;
        return Math.Clamp(safePercent, ReaderMinFocalPointPercent, ReaderMaxFocalPointPercent);
    }

    private static int ParseReaderControlValue(object? rawValue, int min, int max, int fallback)
    {
        if (rawValue is null)
        {
            return fallback;
        }

        return int.TryParse(rawValue.ToString(), out var parsed)
            ? Math.Clamp(parsed, min, max)
            : fallback;
    }
}
