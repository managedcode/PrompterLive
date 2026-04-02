using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Streaming;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Core.Services.Streaming;

namespace PrompterOne.Shared.Services;

internal static class StreamingSettingsNormalizer
{
    public static StudioSettings Normalize(StudioSettings settings, IReadOnlyList<SceneCameraSource> sceneCameras)
    {
        var transportConnections = NormalizeTransportConnections(settings.Streaming.TransportConnections);
        var distributionTargets = NormalizeDistributionTargets(settings.Streaming.DistributionTargets, transportConnections);
        var normalizedStreaming = settings.Streaming with
        {
            ProgramCapture = settings.Streaming.ProgramCaptureSettings,
            Recording = settings.Streaming.RecordingSettings,
            TransportConnections = transportConnections,
            DistributionTargets = distributionTargets
        };

        normalizedStreaming = GoLiveDestinationRouting.Normalize(normalizedStreaming, sceneCameras);
        return settings with { Streaming = normalizedStreaming };
    }

    private static IReadOnlyList<TransportConnectionProfile> NormalizeTransportConnections(
        IReadOnlyList<TransportConnectionProfile>? connections)
    {
        var normalizedConnections = new List<TransportConnectionProfile>();
        foreach (var connection in connections ?? Array.Empty<TransportConnectionProfile>())
        {
            if (!StreamingPlatformCatalog.IsTransportKind(connection.PlatformKind))
            {
                continue;
            }

            var definition = StreamingPlatformCatalog.Get(connection.PlatformKind);
            var name = string.IsNullOrWhiteSpace(connection.Name)
                ? definition.DefaultProfileName
                : connection.Name;

            normalizedConnections.Add(connection with
            {
                Name = name,
                Roles = connection.Roles is StreamingTransportRole.None
                    ? StreamingTransportRole.Both
                    : connection.Roles,
                BaseUrl = connection.PlatformKind is StreamingPlatformKind.VdoNinja
                    ? NormalizeVdoBaseUrl(connection.BaseUrl)
                    : string.Empty
            });
        }

        return normalizedConnections;
    }

    private static IReadOnlyList<DistributionTargetProfile> NormalizeDistributionTargets(
        IReadOnlyList<DistributionTargetProfile>? targets,
        IReadOnlyList<TransportConnectionProfile> transportConnections)
    {
        var validConnectionIds = transportConnections
            .Select(connection => connection.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);

        var normalizedTargets = new List<DistributionTargetProfile>();
        foreach (var target in targets ?? Array.Empty<DistributionTargetProfile>())
        {
            if (!StreamingPlatformCatalog.IsDistributionKind(target.PlatformKind))
            {
                continue;
            }

            var definition = StreamingPlatformCatalog.Get(target.PlatformKind);
            var name = string.IsNullOrWhiteSpace(target.Name)
                ? definition.DefaultProfileName
                : target.Name;
            var boundTransportConnectionIds = target.GetBoundTransportConnectionIds()
                .Where(validConnectionIds.Contains)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            normalizedTargets.Add(target with
            {
                Name = name,
                RtmpUrl = string.IsNullOrWhiteSpace(target.RtmpUrl)
                    ? definition.DefaultRtmpUrl
                    : target.RtmpUrl,
                BoundTransportConnectionIds = boundTransportConnectionIds
            });
        }

        return normalizedTargets;
    }

    private static string NormalizeVdoBaseUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return VdoNinjaDefaults.HostedBaseUrl;
        }

        return value.EndsWith("/", StringComparison.Ordinal)
            ? value
            : string.Concat(value, "/");
    }
}
