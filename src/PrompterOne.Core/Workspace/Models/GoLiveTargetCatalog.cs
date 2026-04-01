namespace PrompterOne.Core.Models.Workspace;

public sealed record GoLiveLocalTargetDefinition(
    string Id,
    string Name);

public static class GoLiveTargetCatalog
{
    public static IReadOnlyList<string> LocalTargetIds { get; } =
    [
        TargetIds.Obs,
        TargetIds.Ndi,
        TargetIds.Recording
    ];

    public static IReadOnlyList<GoLiveLocalTargetDefinition> LocalTargets { get; } =
    [
        new(TargetIds.Obs, TargetNames.Obs),
        new(TargetIds.Ndi, TargetNames.Ndi),
        new(TargetIds.Recording, TargetNames.Recording)
    ];

    public static class TargetIds
    {
        public const string Obs = "obs-studio";
        public const string Ndi = "ndi-output";
        public const string Recording = "local-recording";
        public const string LiveKit = "livekit";
        public const string VdoNinja = "vdoninja";
        public const string Youtube = "youtube-live";
        public const string Twitch = "twitch-live";
        public const string CustomRtmp = "custom-rtmp";
    }

    public static class TargetNames
    {
        public const string Obs = "OBS Studio";
        public const string Ndi = "NDI Output";
        public const string Recording = "Local Recording";
        public const string LiveKit = "LiveKit";
        public const string VdoNinja = "VDO.Ninja";
        public const string Youtube = "YouTube Live";
        public const string Twitch = "Twitch";
        public const string CustomRtmp = StreamingDefaults.CustomTargetName;
    }
}
