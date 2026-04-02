namespace PrompterOne.Shared.Settings.Models;

public static class SettingsStreamingCardIds
{
    public const string CustomRtmp = "streaming-custom-rtmp";
    public const string LiveKit = "streaming-livekit";
    public const string Recording = "streaming-recording";
    public const string Twitch = "streaming-twitch";
    public const string VdoNinja = "streaming-vdo";
    public const string Youtube = "streaming-youtube";

    public static string DistributionTarget(string targetId) => $"streaming-target-{targetId}";

    public static string TransportConnection(string connectionId) => $"streaming-connection-{connectionId}";
}
