using Microsoft.AspNetCore.Components;
using PrompterOne.Core.Models.Streaming;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Core.Services.Streaming;

namespace PrompterOne.Shared.Pages;

public partial class SettingsPage
{
    private const string DirectRtmpOutputModeValue = "direct-rtmp";
    private const string LocalRecordingOutputModeValue = "local-recording";
    private const string NdiOutputModeValue = "ndi-output";
    private const string VirtualCameraOutputModeValue = "virtual-camera";

    private string SelectedStreamingOutputModeValue => _studioSettings.Streaming.OutputMode switch
    {
        StreamingOutputMode.DirectRtmp => DirectRtmpOutputModeValue,
        StreamingOutputMode.LocalRecording => LocalRecordingOutputModeValue,
        StreamingOutputMode.NdiOutput => NdiOutputModeValue,
        _ => VirtualCameraOutputModeValue
    };

    private Task ToggleObsOutputAsync() =>
        UpdateStreamingSettingsAsync(streaming => streaming with
        {
            ObsVirtualCameraEnabled = !streaming.ObsVirtualCameraEnabled,
            OutputMode = StreamingOutputMode.VirtualCamera
        });

    private Task ToggleNdiOutputAsync() =>
        UpdateStreamingSettingsAsync(streaming => streaming with
        {
            NdiOutputEnabled = !streaming.NdiOutputEnabled,
            OutputMode = StreamingOutputMode.NdiOutput
        });

    private Task ToggleRecordingOutputAsync() =>
        UpdateStreamingSettingsAsync(streaming => streaming with
        {
            LocalRecordingEnabled = !streaming.LocalRecordingEnabled,
            OutputMode = StreamingOutputMode.LocalRecording
        });

    private async Task AddExternalDestinationAsync(StreamingPlatformKind kind)
    {
        var existingIds = (_studioSettings.Streaming.ExternalDestinations ?? Array.Empty<StreamingProfile>())
            .Select(destination => destination.Id);
        var destination = StreamingPlatformCatalog.CreateProfile(kind, existingIds);
        destination = destination with
        {
            Name = BuildDestinationDisplayName(kind, destination.Id)
        };

        if (destination.ProviderKind == StreamingProviderKind.Rtmp)
        {
            destination = destination.SetPrimaryDestination(
                destination.Name,
                destination.GetPrimaryDestinationUrl(),
                destination.GetPrimaryDestinationStreamKey());
        }

        await UpdateStreamingSettingsAsync(streaming => streaming with
        {
            ExternalDestinations = (streaming.ExternalDestinations ?? Array.Empty<StreamingProfile>())
                .Append(destination)
                .ToArray()
        });
    }

    private Task ToggleExternalDestinationAsync(string destinationId) =>
        UpdateStreamingSettingsAsync(
            streaming => UpdateExternalDestination(
                streaming,
                destinationId,
                destination => destination with { IsEnabled = !destination.IsEnabled }));

    private Task RemoveExternalDestinationAsync(string destinationId) =>
        UpdateStreamingSettingsAsync(streaming => streaming with
        {
            ExternalDestinations = (streaming.ExternalDestinations ?? Array.Empty<StreamingProfile>())
                .Where(destination => !string.Equals(destination.Id, destinationId, StringComparison.Ordinal))
                .ToArray()
        });

    private Task UpdateExternalDestinationFieldAsync((string DestinationId, string FieldId, string Value) update) =>
        UpdateStreamingSettingsAsync(
            streaming => UpdateExternalDestination(
                streaming,
                update.DestinationId,
                destination => ApplyExternalDestinationField(destination, update.FieldId, update.Value)));

    private async Task ToggleStreamingDestinationSourceAsync((string TargetId, string SourceId) update)
    {
        await UpdateStreamingSettingsAsync(
            streaming => GoLiveDestinationRouting.ToggleSource(
                streaming,
                update.TargetId,
                update.SourceId,
                _sceneCameras),
            normalizeSources: false);
    }

    private async Task OnStreamingOutputResolutionChanged(ChangeEventArgs args)
    {
        if (!Enum.TryParse<StreamingResolutionPreset>(args.Value?.ToString(), out var outputResolution))
        {
            return;
        }

        await UpdateStreamingSettingsAsync(streaming => streaming with { OutputResolution = outputResolution });
    }

    private async Task OnStreamingOutputModeChanged(ChangeEventArgs args)
    {
        var nextMode = args.Value?.ToString() switch
        {
            DirectRtmpOutputModeValue => StreamingOutputMode.DirectRtmp,
            LocalRecordingOutputModeValue => StreamingOutputMode.LocalRecording,
            NdiOutputModeValue => StreamingOutputMode.NdiOutput,
            _ => StreamingOutputMode.VirtualCamera
        };

        await UpdateStreamingSettingsAsync(streaming => streaming with { OutputMode = nextMode });
    }

    private async Task UpdateStreamingBitrateAsync(ChangeEventArgs args)
    {
        if (!int.TryParse(args.Value?.ToString(), out var bitrate))
        {
            return;
        }

        await UpdateStreamingSettingsAsync(streaming => streaming with { BitrateKbps = Math.Max(250, bitrate) });
    }

    private Task ToggleSettingsTextOverlayAsync() =>
        UpdateStreamingSettingsAsync(streaming => streaming with
        {
            ShowTextOverlay = !streaming.ShowTextOverlay
        });

    private async Task ToggleSettingsIncludeCameraAsync()
    {
        var nextValue = !_studioSettings.Streaming.IncludeCameraInOutput;
        foreach (var camera in _sceneCameras)
        {
            MediaSceneService.SetIncludeInOutput(camera.SourceId, nextValue);
        }

        await PersistSceneAsync();
        await UpdateStreamingSettingsAsync(streaming => streaming with { IncludeCameraInOutput = nextValue });
    }

    private async Task UpdateStreamingSettingsAsync(
        Func<StreamStudioSettings, StreamStudioSettings> update,
        bool normalizeSources = true)
    {
        var nextStreaming = update(_studioSettings.Streaming);
        if (normalizeSources)
        {
            nextStreaming = GoLiveDestinationRouting.Normalize(nextStreaming, _sceneCameras);
        }

        _studioSettings = _studioSettings with { Streaming = nextStreaming };
        await PersistStudioSettingsAsync();
    }

    private static StreamStudioSettings UpdateExternalDestination(
        StreamStudioSettings streaming,
        string destinationId,
        Func<StreamingProfile, StreamingProfile> update)
    {
        return streaming with
        {
            ExternalDestinations = (streaming.ExternalDestinations ?? Array.Empty<StreamingProfile>())
                .Select(destination => string.Equals(destination.Id, destinationId, StringComparison.Ordinal)
                    ? update(destination)
                    : destination)
                .ToArray()
        };
    }

    private static StreamingProfile ApplyExternalDestinationField(
        StreamingProfile destination,
        string fieldId,
        string value)
    {
        return fieldId switch
        {
            StreamingDestinationFieldIds.Name => ApplyDestinationName(destination, value),
            StreamingDestinationFieldIds.ServerUrl => destination with { ServerUrl = value },
            StreamingDestinationFieldIds.RoomName => destination with { RoomName = value },
            StreamingDestinationFieldIds.Token => destination with { Token = value },
            StreamingDestinationFieldIds.PublishUrl => destination with { PublishUrl = value },
            StreamingDestinationFieldIds.RtmpUrl => destination.SetPrimaryDestination(
                destination.Name,
                value,
                destination.GetPrimaryDestinationStreamKey()),
            StreamingDestinationFieldIds.StreamKey => destination.SetPrimaryDestination(
                destination.Name,
                destination.GetPrimaryDestinationUrl(),
                value),
            _ => destination
        };
    }

    private static StreamingProfile ApplyDestinationName(StreamingProfile destination, string value)
    {
        var definition = StreamingPlatformCatalog.Get(destination.PlatformKind);
        var name = string.IsNullOrWhiteSpace(value)
            ? definition.DefaultProfileName
            : value;

        return destination.ProviderKind == StreamingProviderKind.Rtmp
            ? destination.SetPrimaryDestination(
                name,
                destination.GetPrimaryDestinationUrl(),
                destination.GetPrimaryDestinationStreamKey())
            : destination with { Name = name };
    }

    private static string BuildDestinationDisplayName(StreamingPlatformKind kind, string destinationId)
    {
        var definition = StreamingPlatformCatalog.Get(kind);
        if (string.Equals(destinationId, definition.IdPrefix, StringComparison.Ordinal))
        {
            return definition.DefaultProfileName;
        }

        var suffix = destinationId[(definition.IdPrefix.Length + 1)..];
        return $"{definition.DefaultProfileName} {suffix}";
    }
}
