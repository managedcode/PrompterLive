using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Streaming;
using PrompterOne.Shared.Services;

namespace PrompterOne.Shared.Pages;

public partial class GoLivePage
{
    private IReadOnlyList<SceneCameraSource> _remoteSceneSources = [];

    private IReadOnlyList<SceneCameraSource> AvailableSceneSources =>
        SceneCameras.Concat(_remoteSceneSources).ToArray();

    private bool HasSourceIntakeConnections =>
        TransportConnections.Any(connection =>
            connection.IsEnabled
            && connection.Roles.HasFlag(StreamingTransportRole.Source));

    private async Task SyncRemoteSourcesAsync()
    {
        if (!HasSourceIntakeConnections)
        {
            await GoLiveRemoteSourceRuntime.StopAsync();
            ApplyRemoteSourceState();
            return;
        }

        await GoLiveRemoteSourceRuntime.SyncConnectionsAsync(BuildRuntimeRequest(SelectedCamera ?? PreviewCamera));
        ApplyRemoteSourceState();
    }

    private void ApplyRemoteSourceState()
    {
        var existingSources = _remoteSceneSources.ToDictionary(source => source.SourceId, StringComparer.Ordinal);
        _remoteSceneSources = GoLiveRemoteSourceRuntime.State.Sources
            .Select(source => BuildRemoteSceneSource(source, existingSources.GetValueOrDefault(source.SourceId)))
            .ToArray();
        SyncGoLiveSessionState();
        EnsureStudioSurfaceState();
    }

    private void SetRemoteSourceIncludeInOutput(string sourceId, bool includeInOutput)
    {
        _remoteSceneSources = _remoteSceneSources
            .Select(source => string.Equals(source.SourceId, sourceId, StringComparison.Ordinal)
                ? source with
                {
                    Transform = source.Transform with
                    {
                        IncludeInOutput = includeInOutput
                    }
                }
                : source)
            .ToArray();
        SyncGoLiveSessionState();
    }

    private bool IsRemoteSource(string sourceId) =>
        _remoteSceneSources.Any(source => string.Equals(source.SourceId, sourceId, StringComparison.Ordinal));

    private static SceneCameraSource BuildRemoteSceneSource(
        GoLiveRemoteSourceState source,
        SceneCameraSource? existing)
    {
        return new SceneCameraSource(
            SourceId: source.SourceId,
            DeviceId: string.IsNullOrWhiteSpace(source.DeviceId) ? source.SourceId : source.DeviceId,
            Label: source.Label,
            Transform: existing?.Transform ?? new MediaSourceTransform(IncludeInOutput: true));
    }
}
