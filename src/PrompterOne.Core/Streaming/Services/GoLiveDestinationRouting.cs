using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Streaming;
using PrompterOne.Core.Models.Workspace;

namespace PrompterOne.Core.Services.Streaming;

public static class GoLiveDestinationRouting
{
    public static IReadOnlyList<string> GetSelectedSourceIds(
        StreamStudioSettings streaming,
        string targetId,
        IReadOnlyList<SceneCameraSource> sceneCameras)
    {
        var normalized = NormalizeSelections(streaming, sceneCameras);
        return normalized.FirstOrDefault(selection => string.Equals(selection.TargetId, targetId, StringComparison.Ordinal))
            ?.SourceIds
            ?? Array.Empty<string>();
    }

    public static StreamStudioSettings Normalize(
        StreamStudioSettings streaming,
        IReadOnlyList<SceneCameraSource> sceneCameras)
    {
        return streaming with
        {
            SourceSelections = NormalizeSelections(streaming, sceneCameras)
        };
    }

    public static StreamStudioSettings ToggleSource(
        StreamStudioSettings streaming,
        string targetId,
        string sourceId,
        IReadOnlyList<SceneCameraSource> sceneCameras)
    {
        var selectedSourceIds = GetSelectedSourceIds(streaming, targetId, sceneCameras).ToList();
        if (selectedSourceIds.Contains(sourceId, StringComparer.Ordinal))
        {
            selectedSourceIds.RemoveAll(candidate => string.Equals(candidate, sourceId, StringComparison.Ordinal));
        }
        else
        {
            selectedSourceIds.Add(sourceId);
        }

        return SetSelectedSourceIds(streaming, targetId, selectedSourceIds, sceneCameras);
    }

    public static StreamStudioSettings SetSelectedSourceIds(
        StreamStudioSettings streaming,
        string targetId,
        IReadOnlyList<string> sourceIds,
        IReadOnlyList<SceneCameraSource> sceneCameras)
    {
        var normalizedSelections = NormalizeSelections(streaming, sceneCameras)
            .Select(selection => string.Equals(selection.TargetId, targetId, StringComparison.Ordinal)
                ? selection with
                {
                    SourceIds = FilterValidSourceIds(sourceIds, sceneCameras)
                }
                : selection)
            .ToArray();

        return streaming with { SourceSelections = normalizedSelections };
    }

    private static IReadOnlyList<GoLiveDestinationSourceSelection> NormalizeSelections(
        StreamStudioSettings streaming,
        IReadOnlyList<SceneCameraSource> sceneCameras)
    {
        var fallbackSourceIds = BuildFallbackSourceIds(sceneCameras);
        var existingSelections = (streaming.SourceSelections ?? Array.Empty<GoLiveDestinationSourceSelection>())
            .ToDictionary(selection => selection.TargetId, selection => selection.SourceIds, StringComparer.Ordinal);

        return BuildAllTargetIds(streaming)
            .Select(targetId => new GoLiveDestinationSourceSelection(
                targetId,
                existingSelections.TryGetValue(targetId, out var existingSourceIds)
                    ? FilterValidSourceIds(existingSourceIds, sceneCameras)
                    : fallbackSourceIds))
            .ToArray();
    }

    private static IReadOnlyList<string> BuildAllTargetIds(StreamStudioSettings streaming)
    {
        var transportConnectionIds = (streaming.TransportConnections ?? Array.Empty<TransportConnectionProfile>())
            .Select(connection => connection.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal);

        return GoLiveTargetCatalog.LocalTargetIds
            .Concat(transportConnectionIds)
            .ToArray();
    }

    private static IReadOnlyList<string> BuildFallbackSourceIds(IReadOnlyList<SceneCameraSource> sceneCameras)
    {
        var included = sceneCameras
            .Where(camera => camera.Transform.Visible && camera.Transform.IncludeInOutput)
            .Select(camera => camera.SourceId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (included.Length > 0)
        {
            return included;
        }

        return sceneCameras
            .Where(camera => camera.Transform.Visible)
            .Select(camera => camera.SourceId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<string> FilterValidSourceIds(
        IReadOnlyList<string> sourceIds,
        IReadOnlyList<SceneCameraSource> sceneCameras)
    {
        var validSourceIds = sceneCameras
            .Select(camera => camera.SourceId)
            .ToHashSet(StringComparer.Ordinal);

        return sourceIds
            .Where(validSourceIds.Contains)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
