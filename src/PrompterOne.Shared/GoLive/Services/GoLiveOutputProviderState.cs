namespace PrompterOne.Shared.Services;

internal sealed record GoLiveOutputProviderSnapshot(
    bool Active);

internal sealed record GoLiveOutputVdoNinjaSnapshot(
    bool Active,
    bool Connected,
    int LastPeerLatencyMs,
    int PeerCount,
    string PublishUrl,
    string RoomName,
    string StreamId);

public sealed record GoLiveOutputVdoNinjaState(
    bool Active,
    bool Connected,
    int LastPeerLatencyMs,
    int PeerCount,
    string PublishUrl,
    string RoomName,
    string StreamId)
{
    public static GoLiveOutputVdoNinjaState Default { get; } = new(
        Active: false,
        Connected: false,
        LastPeerLatencyMs: 0,
        PeerCount: 0,
        PublishUrl: string.Empty,
        RoomName: string.Empty,
        StreamId: string.Empty);
}
