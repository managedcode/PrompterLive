using PrompterOne.Core.Models.Workspace;

namespace PrompterOne.Core.Models.Streaming;

[Flags]
public enum StreamingTransportRole
{
    None = 0,
    Source = 1,
    Publish = 2,
    Both = Source | Publish
}

public enum StreamingProviderKind
{
    LiveKit,
    VdoNinja,
    Rtmp
}

public enum StreamingPlatformKind
{
    LiveKit,
    VdoNinja,
    Youtube,
    Twitch,
    CustomRtmp
}

public sealed record StreamingPlatformDefinition(
    StreamingPlatformKind Kind,
    string IdPrefix,
    string DisplayName,
    StreamingProviderKind ProviderKind,
    string DefaultProfileName,
    string DefaultRtmpUrl = "");

public sealed record ProgramCaptureProfile(
    StreamingResolutionPreset ResolutionPreset = StreamingResolutionPreset.FullHd1080p30,
    int BitrateKbps = 6000,
    bool ShowTextOverlay = true,
    bool IncludeCameraInOutput = true);

public sealed record RecordingProfile(
    bool IsEnabled = false,
    string ContainerLabel = "",
    string VideoCodecLabel = "",
    string AudioCodecLabel = "",
    int VideoBitrateKbps = 0,
    int AudioBitrateKbps = 0,
    int AudioSampleRate = 0,
    int AudioChannelCount = 0);

public sealed record TransportConnectionProfile(
    string Id,
    string Name,
    StreamingPlatformKind PlatformKind,
    StreamingTransportRole Roles = StreamingTransportRole.Both,
    bool IsEnabled = false,
    string ServerUrl = "",
    string BaseUrl = "",
    string RoomName = "",
    string Token = "",
    string PublishUrl = "",
    string ViewUrl = "");

public sealed record DistributionTargetProfile(
    string Id,
    string Name,
    StreamingPlatformKind PlatformKind,
    bool IsEnabled = false,
    string RtmpUrl = "",
    string StreamKey = "",
    IReadOnlyList<string>? BoundTransportConnectionIds = null);

public sealed record StreamingPublishDescriptor(
    string ProviderId,
    string DisplayName,
    bool IsReady,
    bool IsSupported,
    bool RequiresExternalRelay,
    string Summary,
    IReadOnlyDictionary<string, string> Parameters,
    string? LaunchUrl = null);

public static class StreamingPlatformCatalog
{
    public static IReadOnlyList<StreamingPlatformDefinition> All { get; } =
    [
        new(
            StreamingPlatformKind.LiveKit,
            GoLiveTargetCatalog.TargetIds.LiveKit,
            GoLiveTargetCatalog.TargetNames.LiveKit,
            StreamingProviderKind.LiveKit,
            GoLiveTargetCatalog.TargetNames.LiveKit),
        new(
            StreamingPlatformKind.VdoNinja,
            GoLiveTargetCatalog.TargetIds.VdoNinja,
            GoLiveTargetCatalog.TargetNames.VdoNinja,
            StreamingProviderKind.VdoNinja,
            GoLiveTargetCatalog.TargetNames.VdoNinja),
        new(
            StreamingPlatformKind.Youtube,
            GoLiveTargetCatalog.TargetIds.Youtube,
            GoLiveTargetCatalog.TargetNames.Youtube,
            StreamingProviderKind.Rtmp,
            GoLiveTargetCatalog.TargetNames.Youtube,
            "rtmps://a.rtmp.youtube.com/live2"),
        new(
            StreamingPlatformKind.Twitch,
            GoLiveTargetCatalog.TargetIds.Twitch,
            GoLiveTargetCatalog.TargetNames.Twitch,
            StreamingProviderKind.Rtmp,
            GoLiveTargetCatalog.TargetNames.Twitch,
            "rtmp://live.twitch.tv/app"),
        new(
            StreamingPlatformKind.CustomRtmp,
            GoLiveTargetCatalog.TargetIds.CustomRtmp,
            GoLiveTargetCatalog.TargetNames.CustomRtmp,
            StreamingProviderKind.Rtmp,
            GoLiveTargetCatalog.TargetNames.CustomRtmp)
    ];

    public static IReadOnlyList<StreamingPlatformDefinition> TransportModules { get; } =
        All.Where(definition => definition.Kind is StreamingPlatformKind.LiveKit or StreamingPlatformKind.VdoNinja)
            .ToArray();

    public static IReadOnlyList<StreamingPlatformDefinition> DistributionTargets { get; } =
        All.Where(definition => definition.Kind is StreamingPlatformKind.Youtube or StreamingPlatformKind.Twitch or StreamingPlatformKind.CustomRtmp)
            .ToArray();

    public static TransportConnectionProfile CreateTransportConnection(
        StreamingPlatformKind kind,
        IEnumerable<string>? existingIds = null)
    {
        EnsureTransportKind(kind);
        var definition = Get(kind);
        var id = BuildNextId(definition.IdPrefix, existingIds ?? Array.Empty<string>());
        return new TransportConnectionProfile(
            Id: id,
            Name: BuildDisplayName(definition, id),
            PlatformKind: kind,
            BaseUrl: kind is StreamingPlatformKind.VdoNinja
                ? VdoNinjaDefaults.HostedBaseUrl
                : string.Empty);
    }

    public static DistributionTargetProfile CreateDistributionTarget(
        StreamingPlatformKind kind,
        IEnumerable<string>? existingIds = null)
    {
        EnsureDistributionKind(kind);
        var definition = Get(kind);
        var id = BuildNextId(definition.IdPrefix, existingIds ?? Array.Empty<string>());
        return new DistributionTargetProfile(
            Id: id,
            Name: BuildDisplayName(definition, id),
            PlatformKind: kind,
            RtmpUrl: definition.DefaultRtmpUrl,
            BoundTransportConnectionIds: Array.Empty<string>());
    }

    public static StreamingPlatformDefinition Get(StreamingPlatformKind kind) =>
        All.First(definition => definition.Kind == kind);

    public static bool IsTransportKind(StreamingPlatformKind kind) =>
        kind is StreamingPlatformKind.LiveKit or StreamingPlatformKind.VdoNinja;

    public static bool IsDistributionKind(StreamingPlatformKind kind) =>
        kind is StreamingPlatformKind.Youtube or StreamingPlatformKind.Twitch or StreamingPlatformKind.CustomRtmp;

    private static void EnsureTransportKind(StreamingPlatformKind kind)
    {
        if (!IsTransportKind(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Only LiveKit and VDO.Ninja can be created as transport connections.");
        }
    }

    private static void EnsureDistributionKind(StreamingPlatformKind kind)
    {
        if (!IsDistributionKind(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Only distribution targets can be created as downstream targets.");
        }
    }

    private static string BuildDisplayName(StreamingPlatformDefinition definition, string id)
    {
        if (string.Equals(id, definition.IdPrefix, StringComparison.Ordinal))
        {
            return definition.DefaultProfileName;
        }

        var suffix = id[(definition.IdPrefix.Length + 1)..];
        return string.Concat(definition.DefaultProfileName, " ", suffix);
    }

    private static string BuildNextId(string prefix, IEnumerable<string> existingIds)
    {
        var existingIdSet = existingIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);

        if (!existingIdSet.Contains(prefix))
        {
            return prefix;
        }

        var suffix = 2;
        while (existingIdSet.Contains($"{prefix}-{suffix}"))
        {
            suffix++;
        }

        return $"{prefix}-{suffix}";
    }
}

public static class TransportConnectionProfileExtensions
{
    public static StreamingProviderKind GetProviderKind(this TransportConnectionProfile connection) =>
        StreamingPlatformCatalog.Get(connection.PlatformKind).ProviderKind;
}

public static class DistributionTargetProfileExtensions
{
    public static IReadOnlyList<string> GetBoundTransportConnectionIds(this DistributionTargetProfile target) =>
        target.BoundTransportConnectionIds ?? Array.Empty<string>();
}

public static class VdoNinjaDefaults
{
    public const string HostedBaseUrl = "https://vdo.ninja/";
}
