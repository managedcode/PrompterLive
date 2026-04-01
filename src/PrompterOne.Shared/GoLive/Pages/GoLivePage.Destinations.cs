using System.Globalization;
using PrompterOne.Core.Models.Streaming;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Core.Services.Streaming;
using PrompterOne.Shared.Components.GoLive;
using PrompterOne.Shared.GoLive.Models;
using PrompterOne.Shared.Services;

namespace PrompterOne.Shared.Pages;

public partial class GoLivePage
{
    private IReadOnlyList<string> GetSelectedSourceIds(string targetId) =>
        GoLiveDestinationRouting.GetSelectedSourceIds(_studioSettings.Streaming, targetId, SceneCameras);

    private IReadOnlyList<StreamingProfile> ExternalDestinations =>
        _studioSettings.Streaming.ExternalDestinations ?? Array.Empty<StreamingProfile>();

    private bool BuildDestinationIsReady(bool isEnabled, string targetId)
    {
        if (!isEnabled)
        {
            return false;
        }

        return GetSelectedSourceIds(targetId).Count > 0;
    }

    private bool BuildExternalDestinationIsReady(StreamingProfile destination, StreamingPublishDescriptor descriptor)
    {
        if (!destination.IsEnabled || !descriptor.IsReady)
        {
            return false;
        }

        return GetSelectedSourceIds(destination.Id).Count > 0;
    }

    private string BuildLocalSummary(string targetId)
    {
        var selectedSources = GetSelectedSourceIds(targetId);
        return selectedSources.Count == 0
            ? GoLiveText.Destination.NoSourceSummary
            : string.Concat(
                selectedSources.Count.ToString(CultureInfo.InvariantCulture),
                GoLiveText.Destination.LocalSummarySuffix);
    }

    private string BuildExternalSummary(StreamingProfile destination, StreamingPublishDescriptor descriptor)
    {
        if (!destination.IsEnabled)
        {
            return GoLiveText.Destination.DisabledSummary;
        }

        if (GetSelectedSourceIds(destination.Id).Count == 0)
        {
            return GoLiveText.Destination.NoSourceSummary;
        }

        return descriptor.Summary;
    }

    private string BuildTargetStatusLabel(bool isEnabled, string targetId)
    {
        if (!isEnabled)
        {
            return GoLiveText.Destination.DisabledStatusLabel;
        }

        return BuildDestinationIsReady(isEnabled, targetId)
            ? GoLiveText.Destination.EnabledStatusLabel
            : GoLiveText.Destination.NeedsSetupStatusLabel;
    }

    private string BuildExternalTargetStatusLabel(StreamingProfile destination, StreamingPublishDescriptor descriptor)
    {
        if (!destination.IsEnabled)
        {
            return GoLiveText.Destination.DisabledStatusLabel;
        }

        if (!BuildExternalDestinationIsReady(destination, descriptor))
        {
            return GoLiveText.Destination.NeedsSetupStatusLabel;
        }

        return descriptor.RequiresExternalRelay
            ? GoLiveText.Destination.RelayStatusLabel
            : GoLiveText.Destination.EnabledStatusLabel;
    }

    private IReadOnlyList<GoLiveDestinationSummaryViewModel> BuildDestinationSummary()
    {
        var localTargets = GoLiveTargetCatalog.LocalTargets
            .Select(BuildLocalDestinationSummary);
        var externalTargets = ExternalDestinations
            .Select(BuildExternalDestinationSummary);

        return localTargets
            .Concat(externalTargets)
            .ToArray();
    }

    private GoLiveDestinationSummaryViewModel BuildLocalDestinationSummary(GoLiveLocalTargetDefinition target)
    {
        var isEnabled = IsLocalTargetEnabled(target.Id);
        var isReady = BuildDestinationIsReady(isEnabled, target.Id);

        return new GoLiveDestinationSummaryViewModel(
            target.Id,
            target.Name,
            BuildLocalPlatformLabel(target.Id),
            isEnabled,
            isReady,
            BuildLocalSummary(target.Id),
            BuildTargetStatusLabel(isEnabled, target.Id),
            BuildLocalTargetTone(target.Id));
    }

    private GoLiveDestinationSummaryViewModel BuildExternalDestinationSummary(StreamingProfile destination)
    {
        var descriptor = StreamingDescriptorResolver.Describe(destination);
        var presentation = StreamingPlatformPresentationCatalog.Get(destination.PlatformKind);

        return new GoLiveDestinationSummaryViewModel(
            destination.Id,
            destination.Name,
            presentation.GoLivePlatformLabel,
            destination.IsEnabled,
            BuildExternalDestinationIsReady(destination, descriptor),
            BuildExternalSummary(destination, descriptor),
            BuildExternalTargetStatusLabel(destination, descriptor),
            presentation.Tone);
    }

    private static string BuildLocalPlatformLabel(string targetId) =>
        string.Equals(targetId, GoLiveTargetCatalog.TargetIds.Recording, StringComparison.Ordinal)
            ? GoLiveText.Destination.PrimaryChannelPlatformLabel
            : GoLiveText.Destination.SettingsPlatformLabel;

    private static string BuildLocalTargetTone(string targetId) =>
        string.Equals(targetId, GoLiveTargetCatalog.TargetIds.Recording, StringComparison.Ordinal)
            ? GoLiveText.Destination.RecordingTone
            : GoLiveText.Destination.LocalTone;

    private bool IsLocalTargetEnabled(string targetId) => targetId switch
    {
        GoLiveTargetCatalog.TargetIds.Obs => _studioSettings.Streaming.ObsVirtualCameraEnabled,
        GoLiveTargetCatalog.TargetIds.Ndi => _studioSettings.Streaming.NdiOutputEnabled,
        GoLiveTargetCatalog.TargetIds.Recording => _studioSettings.Streaming.LocalRecordingEnabled,
        _ => false
    };

    private StreamingProfile? ResolvePrimaryRoomDestination()
    {
        return ExternalDestinations.FirstOrDefault(destination =>
                   destination.IsEnabled
                   && destination.ProviderKind == StreamingProviderKind.LiveKit
                   && (!string.IsNullOrWhiteSpace(destination.RoomName)
                       || !string.IsNullOrWhiteSpace(destination.ServerUrl)
                       || !string.IsNullOrWhiteSpace(destination.Token)))
            ?? ExternalDestinations.FirstOrDefault(destination =>
                destination.IsEnabled
                && destination.ProviderKind == StreamingProviderKind.VdoNinja
                && (!string.IsNullOrWhiteSpace(destination.PublishUrl)
                    || !string.IsNullOrWhiteSpace(destination.RoomName)));
    }

    private async Task ToggleDestinationSummaryAsync(string targetId)
    {
        if (GoLiveTargetCatalog.LocalTargetIds.Contains(targetId, StringComparer.Ordinal))
        {
            await ToggleLocalDestinationAsync(targetId);
            return;
        }

        await UpdateGoLiveStreamingSettingsAsync(streaming => streaming with
        {
            ExternalDestinations = ExternalDestinations
                .Select(destination => string.Equals(destination.Id, targetId, StringComparison.Ordinal)
                    ? destination with { IsEnabled = !destination.IsEnabled }
                    : destination)
                .ToArray()
        });
    }

    private async Task ToggleLocalDestinationAsync(string targetId)
    {
        if (string.Equals(targetId, GoLiveTargetCatalog.TargetIds.Obs, StringComparison.Ordinal))
        {
            await EnsurePageReadyAsync();
            await UpdateGoLiveStreamingSettingsAsync(streaming => streaming with
            {
                ObsVirtualCameraEnabled = !streaming.ObsVirtualCameraEnabled,
                OutputMode = StreamingOutputMode.VirtualCamera
            });
            return;
        }

        if (string.Equals(targetId, GoLiveTargetCatalog.TargetIds.Ndi, StringComparison.Ordinal))
        {
            await EnsurePageReadyAsync();
            await UpdateGoLiveStreamingSettingsAsync(streaming => streaming with
            {
                NdiOutputEnabled = !streaming.NdiOutputEnabled,
                OutputMode = StreamingOutputMode.NdiOutput
            });
            return;
        }

        await EnsurePageReadyAsync();
        await UpdateGoLiveStreamingSettingsAsync(streaming => streaming with
        {
            LocalRecordingEnabled = !streaming.LocalRecordingEnabled,
            OutputMode = StreamingOutputMode.LocalRecording
        });
    }

    private async Task UpdateGoLiveStreamingSettingsAsync(Func<StreamStudioSettings, StreamStudioSettings> update)
    {
        await EnsurePageReadyAsync();
        _studioSettings = _studioSettings with
        {
            Streaming = GoLiveDestinationRouting.Normalize(update(_studioSettings.Streaming), SceneCameras)
        };

        await PersistStudioSettingsAsync();
    }
}
