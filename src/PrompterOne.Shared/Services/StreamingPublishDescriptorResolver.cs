using PrompterOne.Core.Abstractions;
using PrompterOne.Core.Models.Streaming;
using PrompterOne.Core.Models.Workspace;

namespace PrompterOne.Shared.Services;

public sealed class StreamingPublishDescriptorResolver(IGoLiveModuleRegistry moduleRegistry)
{
    private readonly IGoLiveModuleRegistry _moduleRegistry = moduleRegistry;

    public StreamingPublishDescriptor DescribeTransport(TransportConnectionProfile connection)
    {
        var descriptor = _moduleRegistry.ResolveOutput(GetModuleId(connection.PlatformKind))?.Descriptor;
        if (descriptor is null)
        {
            return BuildMissingDescriptor(connection.Name);
        }

        return connection.PlatformKind switch
        {
            StreamingPlatformKind.LiveKit => DescribeLiveKitConnection(connection, descriptor),
            StreamingPlatformKind.VdoNinja => DescribeVdoNinjaConnection(connection, descriptor),
            _ => BuildMissingDescriptor(connection.Name)
        };
    }

    public StreamingPublishDescriptor DescribeTarget(
        DistributionTargetProfile target,
        IReadOnlyList<TransportConnectionProfile> transportConnections)
    {
        var boundConnections = target.GetBoundTransportConnectionIds()
            .Select(connectionId => transportConnections.FirstOrDefault(connection => string.Equals(connection.Id, connectionId, StringComparison.Ordinal)))
            .Where(connection => connection is not null)
            .Cast<TransportConnectionProfile>()
            .ToArray();

        var enabledConnections = boundConnections
            .Where(connection => connection.IsEnabled)
            .ToArray();
        var supportedConnections = enabledConnections
            .Where(TransportSupportsDownstreamTargets)
            .ToArray();
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["rtmpUrl"] = target.RtmpUrl,
            ["boundConnectionIds"] = string.Join(",", target.GetBoundTransportConnectionIds())
        };

        if (boundConnections.Length == 0)
        {
            return new StreamingPublishDescriptor(
                ProviderId: target.Id,
                DisplayName: target.Name,
                IsReady: false,
                IsSupported: false,
                RequiresExternalRelay: true,
                Summary: "Bind this target to at least one transport connection first.",
                Parameters: parameters);
        }

        if (supportedConnections.Length == 0)
        {
            return new StreamingPublishDescriptor(
                ProviderId: target.Id,
                DisplayName: target.Name,
                IsReady: false,
                IsSupported: false,
                RequiresExternalRelay: true,
                Summary: "The selected transports do not expose a downstream relay path for this target.",
                Parameters: parameters);
        }

        var isConfigured = !string.IsNullOrWhiteSpace(target.RtmpUrl)
            && !string.IsNullOrWhiteSpace(target.StreamKey);
        parameters["streamKey"] = string.IsNullOrWhiteSpace(target.StreamKey) ? string.Empty : "configured";

        return new StreamingPublishDescriptor(
            ProviderId: target.Id,
            DisplayName: target.Name,
            IsReady: isConfigured,
            IsSupported: true,
            RequiresExternalRelay: true,
            Summary: isConfigured
                ? $"Relay this target through {string.Join(", ", supportedConnections.Select(connection => connection.Name))}."
                : "Configure the downstream RTMP endpoint and stream key for this target.",
            Parameters: parameters);
    }

    private static StreamingPublishDescriptor BuildMissingDescriptor(string name)
    {
        return new StreamingPublishDescriptor(
            ProviderId: name,
            DisplayName: name,
            IsReady: false,
            IsSupported: false,
            RequiresExternalRelay: false,
            Summary: "No streaming module is registered for this transport.",
            Parameters: new Dictionary<string, string>(StringComparer.Ordinal));
    }

    private static StreamingPublishDescriptor DescribeLiveKitConnection(
        TransportConnectionProfile connection,
        GoLiveModuleDescriptor descriptor)
    {
        var isReady = connection.IsEnabled
            && !string.IsNullOrWhiteSpace(connection.ServerUrl)
            && !string.IsNullOrWhiteSpace(connection.RoomName)
            && !string.IsNullOrWhiteSpace(connection.Token);
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["serverUrl"] = connection.ServerUrl,
            ["roomName"] = connection.RoomName,
            ["token"] = string.IsNullOrWhiteSpace(connection.Token) ? string.Empty : "configured"
        };

        return new StreamingPublishDescriptor(
            ProviderId: connection.Id,
            DisplayName: descriptor.DisplayName,
            IsReady: isReady,
            IsSupported: true,
            RequiresExternalRelay: false,
            Summary: isReady
                ? "Publishes the composed browser program into a LiveKit room."
                : "Configure the LiveKit server URL, room name, and access token.",
            Parameters: parameters);
    }

    private static StreamingPublishDescriptor DescribeVdoNinjaConnection(
        TransportConnectionProfile connection,
        GoLiveModuleDescriptor descriptor)
    {
        var launchUrl = BuildVdoPublishUrl(connection);
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["baseUrl"] = connection.BaseUrl,
            ["roomName"] = connection.RoomName,
            ["publishUrl"] = launchUrl,
            ["viewUrl"] = connection.ViewUrl
        };

        return new StreamingPublishDescriptor(
            ProviderId: connection.Id,
            DisplayName: descriptor.DisplayName,
            IsReady: connection.IsEnabled && !string.IsNullOrWhiteSpace(launchUrl),
            IsSupported: true,
            RequiresExternalRelay: false,
            Summary: !string.IsNullOrWhiteSpace(launchUrl)
                ? "Creates a publish-ready VDO.Ninja session for direct browser streaming."
                : "Provide a VDO.Ninja publish URL or room name.",
            Parameters: parameters,
            LaunchUrl: launchUrl);
    }

    private static string BuildVdoPublishUrl(TransportConnectionProfile connection)
    {
        if (!string.IsNullOrWhiteSpace(connection.PublishUrl))
        {
            return connection.PublishUrl;
        }

        if (string.IsNullOrWhiteSpace(connection.RoomName))
        {
            return string.Empty;
        }

        var baseUrl = string.IsNullOrWhiteSpace(connection.BaseUrl)
            ? VdoNinjaDefaults.HostedBaseUrl
            : connection.BaseUrl;
        var separator = baseUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return string.Concat(
            baseUrl,
            separator,
            "room=",
            Uri.EscapeDataString(connection.RoomName),
            "&push=",
            Uri.EscapeDataString(connection.RoomName));
    }

    private static string GetModuleId(StreamingPlatformKind kind) =>
        kind switch
        {
            StreamingPlatformKind.LiveKit => GoLiveTargetCatalog.TargetIds.LiveKit,
            StreamingPlatformKind.VdoNinja => GoLiveTargetCatalog.TargetIds.VdoNinja,
            _ => string.Empty
        };

    private bool TransportSupportsDownstreamTargets(TransportConnectionProfile connection)
    {
        var descriptor = _moduleRegistry.ResolveOutput(GetModuleId(connection.PlatformKind))?.Descriptor;
        return descriptor?.Capabilities.SupportsDownstreamTargets == true;
    }
}
