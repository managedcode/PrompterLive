namespace PrompterOne.Core.Models.Streaming;

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

public sealed record StreamingDestination(
    string Name,
    string Url,
    string? StreamKey = null,
    bool IsEnabled = true);

public sealed record StreamingPlatformDefinition(
    StreamingPlatformKind Kind,
    string IdPrefix,
    string DisplayName,
    StreamingProviderKind ProviderKind,
    string DefaultProfileName,
    string DefaultRtmpUrl = "");

public sealed record StreamingProfile(
    string Id,
    string Name,
    StreamingProviderKind ProviderKind,
    StreamingPlatformKind PlatformKind,
    bool IsEnabled = false,
    string? ServerUrl = null,
    string? RoomName = null,
    string? Token = null,
    string? PublishUrl = null,
    IReadOnlyList<StreamingDestination>? Destinations = null,
    bool MirrorLocalPreview = true)
{
    public static StreamingProfile CreateDefault(StreamingProviderKind providerKind) =>
        StreamingPlatformCatalog.CreateProfile(providerKind switch
        {
            StreamingProviderKind.LiveKit => StreamingPlatformKind.LiveKit,
            StreamingProviderKind.VdoNinja => StreamingPlatformKind.VdoNinja,
            _ => StreamingPlatformKind.CustomRtmp
        });
}

public sealed record StreamingPublishDescriptor(
    string ProviderId,
    StreamingProviderKind ProviderKind,
    string DisplayName,
    bool IsReady,
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
            "livekit",
            "LiveKit",
            StreamingProviderKind.LiveKit,
            "LiveKit"),
        new(
            StreamingPlatformKind.VdoNinja,
            "vdoninja",
            "VDO.Ninja",
            StreamingProviderKind.VdoNinja,
            "VDO.Ninja"),
        new(
            StreamingPlatformKind.Youtube,
            "youtube-live",
            "YouTube Live",
            StreamingProviderKind.Rtmp,
            "YouTube Live",
            "rtmps://a.rtmp.youtube.com/live2"),
        new(
            StreamingPlatformKind.Twitch,
            "twitch-live",
            "Twitch",
            StreamingProviderKind.Rtmp,
            "Twitch",
            "rtmp://live.twitch.tv/app"),
        new(
            StreamingPlatformKind.CustomRtmp,
            "custom-rtmp",
            "Custom RTMP",
            StreamingProviderKind.Rtmp,
            "Custom RTMP")
    ];

    public static StreamingProfile CreateProfile(
        StreamingPlatformKind kind,
        IEnumerable<string>? existingIds = null)
    {
        var definition = Get(kind);
        var nextId = BuildNextId(definition.IdPrefix, existingIds ?? Array.Empty<string>());
        return CreateProfile(kind, nextId);
    }

    public static StreamingProfile CreateProfile(StreamingPlatformKind kind, string id)
    {
        var definition = Get(kind);
        return new StreamingProfile(
            Id: id,
            Name: definition.DefaultProfileName,
            ProviderKind: definition.ProviderKind,
            PlatformKind: kind,
            Destinations: definition.ProviderKind == StreamingProviderKind.Rtmp
                ? [new StreamingDestination(definition.DefaultProfileName, definition.DefaultRtmpUrl)]
                : Array.Empty<StreamingDestination>());
    }

    public static StreamingPlatformDefinition Get(StreamingPlatformKind kind) =>
        All.First(definition => definition.Kind == kind);

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

public static class StreamingProfileExtensions
{
    public static StreamingDestination GetPrimaryDestination(this StreamingProfile profile)
    {
        if (profile.Destinations is { Count: > 0 } destinations)
        {
            return destinations[0];
        }

        return new StreamingDestination(profile.Name, string.Empty);
    }

    public static string GetPrimaryDestinationUrl(this StreamingProfile profile) =>
        profile.GetPrimaryDestination().Url;

    public static string GetPrimaryDestinationStreamKey(this StreamingProfile profile) =>
        profile.GetPrimaryDestination().StreamKey ?? string.Empty;

    public static StreamingProfile SetPrimaryDestination(
        this StreamingProfile profile,
        string name,
        string url,
        string streamKey)
    {
        return profile with
        {
            Name = name,
            Destinations =
            [
                new StreamingDestination(
                    string.IsNullOrWhiteSpace(name) ? profile.Name : name,
                    url,
                    streamKey)
            ]
        };
    }
}
