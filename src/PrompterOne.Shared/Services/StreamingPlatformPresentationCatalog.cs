using PrompterOne.Core.Models.Streaming;

namespace PrompterOne.Shared.Services;

public sealed record StreamingPlatformPresentation(
    string SettingsAccountLabel,
    string SettingsDescription,
    string GoLivePlatformLabel,
    string Tone,
    string IconCssClass);

public static class StreamingPlatformPresentationCatalog
{
    public static StreamingPlatformPresentation Get(StreamingPlatformKind kind) =>
        kind switch
        {
            StreamingPlatformKind.LiveKit => new(
                SettingsAccountLabel: "LiveKit · Transport connection",
                SettingsDescription: "Configure the LiveKit room transport here so the browser can publish the program feed and optionally ingest remote guests.",
                GoLivePlatformLabel: "Transport",
                Tone: "livekit",
                IconCssClass: "set-dest-rtmp"),
            StreamingPlatformKind.VdoNinja => new(
                SettingsAccountLabel: "VDO.Ninja · Transport connection",
                SettingsDescription: "Store hosted or self-hosted VDO.Ninja base, room, publish, and view URLs here for browser-side publishing and guest intake.",
                GoLivePlatformLabel: "Transport",
                Tone: "vdoninja",
                IconCssClass: "set-dest-rtmp"),
            StreamingPlatformKind.Youtube => new(
                SettingsAccountLabel: "YouTube Live · RTMP / RTMPS",
                SettingsDescription: "Bind YouTube to a relay-capable transport connection and keep the downstream RTMP credentials out of the live runtime surface.",
                GoLivePlatformLabel: "Relay target",
                Tone: "youtube",
                IconCssClass: "set-dest-yt"),
            StreamingPlatformKind.Twitch => new(
                SettingsAccountLabel: "Twitch · RTMP",
                SettingsDescription: "Bind Twitch to a relay-capable transport connection and keep the downstream endpoint out of the live runtime surface.",
                GoLivePlatformLabel: "Relay target",
                Tone: "twitch",
                IconCssClass: "set-dest-tw"),
            _ => new(
                SettingsAccountLabel: "Custom RTMP · Private ingest",
                SettingsDescription: "Use a private ingest endpoint or CDN through a relay-capable transport connection instead of faking direct browser RTMP.",
                GoLivePlatformLabel: "Relay target",
                Tone: "relay",
                IconCssClass: "set-dest-rtmp")
        };
}
