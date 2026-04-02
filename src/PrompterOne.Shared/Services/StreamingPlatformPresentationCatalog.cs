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
                SettingsAccountLabel: "LiveKit · Browser publish",
                SettingsDescription: "Store LiveKit browser publishing settings here so Go Live stays operational-only.",
                GoLivePlatformLabel: "Guest room",
                Tone: "livekit",
                IconCssClass: "set-dest-rtmp"),
            StreamingPlatformKind.VdoNinja => new(
                SettingsAccountLabel: "VDO.Ninja · Browser room",
                SettingsDescription: "Keep VDO.Ninja room and publish URL setup in Settings and launch from Go Live only when ready.",
                GoLivePlatformLabel: "Browser room",
                Tone: "relay",
                IconCssClass: "set-dest-rtmp"),
            StreamingPlatformKind.Youtube => new(
                SettingsAccountLabel: "YouTube Live · RTMP / RTMPS",
                SettingsDescription: "Store YouTube RTMP credentials here and use Go Live only to arm and inspect the destination.",
                GoLivePlatformLabel: "Relay target",
                Tone: "youtube",
                IconCssClass: "set-dest-yt"),
            StreamingPlatformKind.Twitch => new(
                SettingsAccountLabel: "Twitch · RTMP",
                SettingsDescription: "Persist Twitch endpoint and key here so the runtime surface stays focused on program switching.",
                GoLivePlatformLabel: "Relay target",
                Tone: "twitch",
                IconCssClass: "set-dest-tw"),
            _ => new(
                SettingsAccountLabel: "Custom RTMP · Private ingest",
                SettingsDescription: "Use a private ingest endpoint or CDN and keep the credentials out of the live runtime surface.",
                GoLivePlatformLabel: "Relay target",
                Tone: "relay",
                IconCssClass: "set-dest-rtmp")
        };
}
