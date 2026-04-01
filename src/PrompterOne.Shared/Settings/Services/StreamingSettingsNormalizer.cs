using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Streaming;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Core.Services.Streaming;

namespace PrompterOne.Shared.Services;

internal static class StreamingSettingsNormalizer
{
    public static StudioSettings Normalize(StudioSettings settings, IReadOnlyList<SceneCameraSource> sceneCameras)
    {
        var normalizedStreaming = NormalizeLocalTargets(settings.Streaming);
        normalizedStreaming = normalizedStreaming with
        {
            ExternalDestinations = NormalizeExternalDestinations(normalizedStreaming)
        };
        normalizedStreaming = GoLiveDestinationRouting.Normalize(normalizedStreaming, sceneCameras);
        return settings with { Streaming = normalizedStreaming };
    }

    private static StreamStudioSettings NormalizeLocalTargets(StreamStudioSettings streaming)
    {
        var normalizedStreaming = streaming with
        {
            CustomRtmpName = NormalizeCustomName(streaming.CustomRtmpName)
        };

        if (HasExplicitStreamingTargets(normalizedStreaming))
        {
            return normalizedStreaming;
        }

        var customRtmpUrl = string.IsNullOrWhiteSpace(normalizedStreaming.CustomRtmpUrl)
            ? normalizedStreaming.RtmpUrl
            : normalizedStreaming.CustomRtmpUrl;
        var customRtmpKey = string.IsNullOrWhiteSpace(normalizedStreaming.CustomRtmpStreamKey)
            ? normalizedStreaming.StreamKey
            : normalizedStreaming.CustomRtmpStreamKey;

        return normalizedStreaming.OutputMode switch
        {
            StreamingOutputMode.VirtualCamera => normalizedStreaming with { ObsVirtualCameraEnabled = true },
            StreamingOutputMode.NdiOutput => normalizedStreaming with { NdiOutputEnabled = true },
            StreamingOutputMode.LocalRecording => normalizedStreaming with { LocalRecordingEnabled = true },
            StreamingOutputMode.DirectRtmp => normalizedStreaming with
            {
                CustomRtmpEnabled = !string.IsNullOrWhiteSpace(customRtmpUrl),
                CustomRtmpUrl = customRtmpUrl,
                CustomRtmpStreamKey = customRtmpKey
            },
            _ => normalizedStreaming
        };
    }

    private static bool HasExplicitStreamingTargets(StreamStudioSettings streaming)
    {
        return streaming.ObsVirtualCameraEnabled
            || streaming.NdiOutputEnabled
            || streaming.LocalRecordingEnabled
            || HasExternalDestinationData(streaming);
    }

    private static bool HasExternalDestinationData(StreamStudioSettings streaming)
    {
        if (streaming.ExternalDestinations is { Count: > 0 })
        {
            return true;
        }

        return streaming.LiveKitEnabled
            || streaming.VdoNinjaEnabled
            || streaming.YoutubeEnabled
            || streaming.TwitchEnabled
            || streaming.CustomRtmpEnabled
            || HasAnyValue(streaming.LiveKitServerUrl, streaming.LiveKitRoomName, streaming.LiveKitToken)
            || HasAnyValue(streaming.VdoNinjaRoomName, streaming.VdoNinjaPublishUrl)
            || HasAnyValue(streaming.YoutubeRtmpUrl, streaming.YoutubeStreamKey)
            || HasAnyValue(streaming.TwitchRtmpUrl, streaming.TwitchStreamKey)
            || HasAnyValue(streaming.CustomRtmpUrl, streaming.CustomRtmpStreamKey, streaming.RtmpUrl, streaming.StreamKey);
    }

    private static IReadOnlyList<StreamingProfile> NormalizeExternalDestinations(StreamStudioSettings streaming)
    {
        var existingDestinations = streaming.ExternalDestinations ?? Array.Empty<StreamingProfile>();
        if (existingDestinations.Count > 0)
        {
            return existingDestinations
                .Select(NormalizeExternalDestination)
                .ToArray();
        }

        return BuildLegacyExternalDestinations(streaming);
    }

    private static IReadOnlyList<StreamingProfile> BuildLegacyExternalDestinations(StreamStudioSettings streaming)
    {
        var destinations = new List<StreamingProfile>();

        AddLegacyDestination(
            destinations,
            GoLiveTargetCatalog.TargetIds.LiveKit,
            StreamingPlatformKind.LiveKit,
            streaming.LiveKitEnabled || HasAnyValue(streaming.LiveKitServerUrl, streaming.LiveKitRoomName, streaming.LiveKitToken),
            profile => profile with
            {
                IsEnabled = streaming.LiveKitEnabled,
                ServerUrl = streaming.LiveKitServerUrl,
                RoomName = streaming.LiveKitRoomName,
                Token = streaming.LiveKitToken
            });

        AddLegacyDestination(
            destinations,
            GoLiveTargetCatalog.TargetIds.VdoNinja,
            StreamingPlatformKind.VdoNinja,
            streaming.VdoNinjaEnabled || HasAnyValue(streaming.VdoNinjaRoomName, streaming.VdoNinjaPublishUrl),
            profile => profile with
            {
                IsEnabled = streaming.VdoNinjaEnabled,
                RoomName = streaming.VdoNinjaRoomName,
                PublishUrl = streaming.VdoNinjaPublishUrl
            });

        AddLegacyDestination(
            destinations,
            GoLiveTargetCatalog.TargetIds.Youtube,
            StreamingPlatformKind.Youtube,
            streaming.YoutubeEnabled || HasAnyValue(streaming.YoutubeRtmpUrl, streaming.YoutubeStreamKey),
            profile =>
            {
                var enabledProfile = profile with
                {
                    IsEnabled = streaming.YoutubeEnabled
                };

                return enabledProfile.SetPrimaryDestination(
                    StreamingPlatformCatalog.Get(StreamingPlatformKind.Youtube).DefaultProfileName,
                    string.IsNullOrWhiteSpace(streaming.YoutubeRtmpUrl)
                        ? StreamingPlatformCatalog.Get(StreamingPlatformKind.Youtube).DefaultRtmpUrl
                        : streaming.YoutubeRtmpUrl,
                    streaming.YoutubeStreamKey);
            });

        AddLegacyDestination(
            destinations,
            GoLiveTargetCatalog.TargetIds.Twitch,
            StreamingPlatformKind.Twitch,
            streaming.TwitchEnabled || HasAnyValue(streaming.TwitchRtmpUrl, streaming.TwitchStreamKey),
            profile =>
            {
                var enabledProfile = profile with
                {
                    IsEnabled = streaming.TwitchEnabled
                };

                return enabledProfile.SetPrimaryDestination(
                    StreamingPlatformCatalog.Get(StreamingPlatformKind.Twitch).DefaultProfileName,
                    string.IsNullOrWhiteSpace(streaming.TwitchRtmpUrl)
                        ? StreamingPlatformCatalog.Get(StreamingPlatformKind.Twitch).DefaultRtmpUrl
                        : streaming.TwitchRtmpUrl,
                    streaming.TwitchStreamKey);
            });

        var customRtmpUrl = string.IsNullOrWhiteSpace(streaming.CustomRtmpUrl)
            ? streaming.RtmpUrl
            : streaming.CustomRtmpUrl;
        var customRtmpKey = string.IsNullOrWhiteSpace(streaming.CustomRtmpStreamKey)
            ? streaming.StreamKey
            : streaming.CustomRtmpStreamKey;
        var customRtmpName = NormalizeCustomName(streaming.CustomRtmpName);

        AddLegacyDestination(
            destinations,
            GoLiveTargetCatalog.TargetIds.CustomRtmp,
            StreamingPlatformKind.CustomRtmp,
            streaming.CustomRtmpEnabled || HasAnyValue(customRtmpUrl, customRtmpKey),
            profile =>
            {
                var enabledProfile = profile with
                {
                    IsEnabled = streaming.CustomRtmpEnabled,
                    Name = customRtmpName
                };

                return enabledProfile.SetPrimaryDestination(customRtmpName, customRtmpUrl, customRtmpKey);
            });

        return destinations
            .Select(NormalizeExternalDestination)
            .ToArray();
    }

    private static void AddLegacyDestination(
        ICollection<StreamingProfile> destinations,
        string id,
        StreamingPlatformKind kind,
        bool shouldInclude,
        Func<StreamingProfile, StreamingProfile> update)
    {
        if (!shouldInclude)
        {
            return;
        }

        var profile = StreamingPlatformCatalog.CreateProfile(kind, id);
        destinations.Add(update(profile));
    }

    private static StreamingProfile NormalizeExternalDestination(StreamingProfile profile)
    {
        var kind = ResolvePlatformKind(profile);
        var definition = StreamingPlatformCatalog.Get(kind);
        var name = string.IsNullOrWhiteSpace(profile.Name)
            ? definition.DefaultProfileName
            : profile.Name;

        if (definition.ProviderKind != StreamingProviderKind.Rtmp)
        {
            return profile with
            {
                Name = name,
                PlatformKind = kind,
                ProviderKind = definition.ProviderKind,
                Destinations = Array.Empty<StreamingDestination>()
            };
        }

        var primaryDestination = profile.GetPrimaryDestination();
        var destinationName = string.IsNullOrWhiteSpace(primaryDestination.Name)
            ? name
            : primaryDestination.Name;
        var destinationUrl = string.IsNullOrWhiteSpace(primaryDestination.Url)
            ? definition.DefaultRtmpUrl
            : primaryDestination.Url;

        return profile with
        {
            Name = name,
            PlatformKind = kind,
            ProviderKind = definition.ProviderKind,
            Destinations =
            [
                new StreamingDestination(
                    destinationName,
                    destinationUrl,
                    primaryDestination.StreamKey,
                    primaryDestination.IsEnabled)
            ]
        };
    }

    private static StreamingPlatformKind ResolvePlatformKind(StreamingProfile profile)
    {
        if (TryResolvePlatformKind(profile.Id, out var kind))
        {
            return kind;
        }

        return profile.ProviderKind switch
        {
            StreamingProviderKind.LiveKit => StreamingPlatformKind.LiveKit,
            StreamingProviderKind.VdoNinja => StreamingPlatformKind.VdoNinja,
            _ => profile.PlatformKind is StreamingPlatformKind.Youtube
                or StreamingPlatformKind.Twitch
                or StreamingPlatformKind.CustomRtmp
                    ? profile.PlatformKind
                    : StreamingPlatformKind.CustomRtmp
        };
    }

    private static bool TryResolvePlatformKind(string destinationId, out StreamingPlatformKind kind)
    {
        foreach (var definition in StreamingPlatformCatalog.All)
        {
            if (string.Equals(destinationId, definition.IdPrefix, StringComparison.Ordinal)
                || destinationId.StartsWith($"{definition.IdPrefix}-", StringComparison.Ordinal))
            {
                kind = definition.Kind;
                return true;
            }
        }

        kind = default;
        return false;
    }

    private static bool HasAnyValue(params string?[] values) =>
        values.Any(value => !string.IsNullOrWhiteSpace(value));

    private static string NormalizeCustomName(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? StreamingDefaults.CustomTargetName
            : value;
}
