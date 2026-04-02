using PrompterOne.Core.Models.Streaming;

namespace PrompterOne.Shared.Services;

internal sealed record GoLiveRemoteSourceSnapshot(
    string ConnectionId,
    string DeviceId,
    bool IsConnected,
    string Label,
    int PlatformKind,
    string SourceId);

internal sealed record GoLiveRemoteConnectionSnapshot(
    string ConnectionId,
    bool Connected,
    string RoomName,
    string ServerUrl,
    int PlatformKind,
    IReadOnlyList<GoLiveRemoteSourceSnapshot>? Sources);

internal sealed record GoLiveRemoteSourceRuntimeSnapshot(
    IReadOnlyList<GoLiveRemoteConnectionSnapshot>? Connections,
    IReadOnlyList<GoLiveRemoteSourceSnapshot>? Sources);

public sealed record GoLiveRemoteSourceState(
    string ConnectionId,
    string DeviceId,
    bool IsConnected,
    string Label,
    StreamingPlatformKind PlatformKind,
    string SourceId);

public sealed record GoLiveRemoteConnectionState(
    string ConnectionId,
    bool Connected,
    string RoomName,
    string ServerUrl,
    StreamingPlatformKind PlatformKind,
    IReadOnlyList<GoLiveRemoteSourceState> Sources);

public sealed record GoLiveRemoteSourceRuntimeState(
    IReadOnlyList<GoLiveRemoteConnectionState> Connections,
    IReadOnlyList<GoLiveRemoteSourceState> Sources)
{
    public static GoLiveRemoteSourceRuntimeState Default { get; } = new(
        Connections: [],
        Sources: []);

    public bool HasActiveConnections => Connections.Count > 0;

    public bool HasSources => Sources.Count > 0;

    internal static GoLiveRemoteSourceRuntimeState FromSnapshot(GoLiveRemoteSourceRuntimeSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return Default;
        }

        var sources = (snapshot.Sources ?? [])
            .Select(ToSourceState)
            .ToArray();
        var connections = (snapshot.Connections ?? [])
            .Select(connection => new GoLiveRemoteConnectionState(
                ConnectionId: connection.ConnectionId ?? string.Empty,
                Connected: connection.Connected,
                RoomName: connection.RoomName ?? string.Empty,
                ServerUrl: connection.ServerUrl ?? string.Empty,
                PlatformKind: ResolvePlatformKind(connection.PlatformKind),
                Sources: (connection.Sources ?? [])
                    .Select(ToSourceState)
                    .ToArray()))
            .ToArray();

        return new GoLiveRemoteSourceRuntimeState(connections, sources);
    }

    private static GoLiveRemoteSourceState ToSourceState(GoLiveRemoteSourceSnapshot source)
    {
        return new GoLiveRemoteSourceState(
            ConnectionId: source.ConnectionId ?? string.Empty,
            DeviceId: source.DeviceId ?? string.Empty,
            IsConnected: source.IsConnected,
            Label: source.Label ?? string.Empty,
            PlatformKind: ResolvePlatformKind(source.PlatformKind),
            SourceId: source.SourceId ?? string.Empty);
    }

    private static StreamingPlatformKind ResolvePlatformKind(int platformKind)
    {
        return Enum.IsDefined(typeof(StreamingPlatformKind), platformKind)
            ? (StreamingPlatformKind)platformKind
            : StreamingPlatformKind.LiveKit;
    }
}
