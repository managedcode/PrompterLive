using PrompterOne.Core.Models.Media;
using PrompterOne.Shared.GoLive.Models;
using PrompterOne.Shared.Services;

namespace PrompterOne.Shared.Pages;

public partial class GoLivePage
{
    private SceneCameraSource? LiveProgramCamera => ResolveLiveSurfaceCamera(SelectedCamera);

    private SceneCameraSource? LivePreviewCamera => ResolveLiveSurfaceCamera(ActiveCamera);

    private string LivePreviewEmptyDescription => IsActivePreviewLimited
        ? string.Concat(ActiveSourceLabel, ". ", GoLiveText.Surface.SingleLocalCameraPreviewLimitDescription)
        : string.Empty;

    private string LivePreviewEmptyTitle => IsActivePreviewLimited
        ? GoLiveText.Surface.SingleLocalCameraPreviewLimitTitle
        : string.Empty;

    private IReadOnlyList<string> LiveSourceIds => BuildLiveSourceIds();

    private bool ShowSingleLocalPreviewHint =>
        UsesSingleLocalCameraPreviewMode && SceneCameras.Count > 1;

    private static string SingleLocalPreviewHint => GoLiveText.Surface.SingleLocalCameraPreviewHint;

    private bool SupportsConcurrentLocalCameraCaptures => _captureCapabilities.SupportsConcurrentLocalCameraCaptures;

    private bool UsesSingleLocalCameraPreviewMode => !SupportsConcurrentLocalCameraCaptures;

    private bool IsActivePreviewLimited =>
        UsesSingleLocalCameraPreviewMode
        && ActiveCamera is not null
        && LivePreviewCamera is null;

    private async Task LoadCaptureCapabilitiesAsync()
    {
        try
        {
            _captureCapabilities = await BrowserMediaCaptureCapabilityService.GetAsync();
        }
        catch
        {
            _captureCapabilities = BrowserMediaCaptureCapabilities.Default;
        }
    }

    private IReadOnlyList<SceneCameraSource> BuildRuntimeSceneSources(SceneCameraSource? primaryCamera)
    {
        if (!UsesSingleLocalCameraPreviewMode)
        {
            return AvailableSceneSources;
        }

        var runtimeLocalSourceId = ResolveRuntimeLocalSourceId(primaryCamera);
        if (string.IsNullOrWhiteSpace(runtimeLocalSourceId))
        {
            return AvailableSceneSources;
        }

        return AvailableSceneSources
            .Select(source => IsRemoteSource(source.SourceId) || string.Equals(source.SourceId, runtimeLocalSourceId, StringComparison.Ordinal)
                ? source
                : source with
                {
                    Transform = source.Transform with
                    {
                        IncludeInOutput = false
                    }
                })
            .ToArray();
    }

    private IReadOnlyList<string> BuildLiveSourceIds()
    {
        if (!UsesSingleLocalCameraPreviewMode)
        {
            return AvailableSceneSources.Select(source => source.SourceId).ToArray();
        }

        var focusedLocalSourceId = ResolveFocusedLocalSourceId();
        return AvailableSceneSources
            .Where(source =>
                IsRemoteSource(source.SourceId)
                || string.Equals(source.SourceId, focusedLocalSourceId, StringComparison.Ordinal))
            .Select(source => source.SourceId)
            .ToArray();
    }

    private SceneCameraSource? ResolveLiveSurfaceCamera(SceneCameraSource? camera)
    {
        if (camera is null)
        {
            return null;
        }

        if (!UsesSingleLocalCameraPreviewMode || IsRemoteSource(camera.SourceId))
        {
            return camera;
        }

        return string.Equals(camera.SourceId, ResolveFocusedLocalSourceId(), StringComparison.Ordinal)
            ? camera
            : null;
    }

    private string? ResolveFocusedLocalSourceId()
    {
        if (!UsesSingleLocalCameraPreviewMode)
        {
            return null;
        }

        if (SelectedCamera is not null && !IsRemoteSource(SelectedCamera.SourceId))
        {
            return SelectedCamera.SourceId;
        }

        if (ActiveCamera is not null && !IsRemoteSource(ActiveCamera.SourceId))
        {
            return ActiveCamera.SourceId;
        }

        return null;
    }

    private string? ResolveRuntimeLocalSourceId(SceneCameraSource? primaryCamera)
    {
        if (primaryCamera is not null && !IsRemoteSource(primaryCamera.SourceId))
        {
            return primaryCamera.SourceId;
        }

        return SceneCameras.FirstOrDefault(camera => camera.Transform.Visible && camera.Transform.IncludeInOutput)?.SourceId
            ?? (SceneCameras.Count > 0 ? SceneCameras[0].SourceId : null);
    }
}
