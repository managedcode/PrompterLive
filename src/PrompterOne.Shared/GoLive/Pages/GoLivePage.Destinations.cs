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

    private bool BuildExternalDestinationIsReady(StreamingProfile destination, StreamingPublishDescriptor descriptor)
    {
        if (!destination.IsEnabled || !descriptor.IsReady)
        {
            return false;
        }

        return GetSelectedSourceIds(destination.Id).Count > 0;
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
        return ExternalDestinations
            .Select(BuildExternalDestinationSummary)
            .ToArray();
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

    private StreamingProfile? ResolvePrimaryRoomDestination()
    {
        return ExternalDestinations.FirstOrDefault(destination =>
                   destination.IsEnabled
                   && destination.ProviderKind == StreamingProviderKind.VdoNinja
                   && (!string.IsNullOrWhiteSpace(destination.PublishUrl)
                       || !string.IsNullOrWhiteSpace(destination.RoomName)))
            ?? ExternalDestinations.FirstOrDefault(destination =>
                   destination.IsEnabled
                   && destination.ProviderKind == StreamingProviderKind.LiveKit
                   && (!string.IsNullOrWhiteSpace(destination.RoomName)
                       || !string.IsNullOrWhiteSpace(destination.ServerUrl)
                       || !string.IsNullOrWhiteSpace(destination.Token)))
            ;
    }

    private async Task ToggleDestinationSummaryAsync(string targetId)
    {
        await UpdateGoLiveStreamingSettingsAsync(streaming => streaming with
        {
            ExternalDestinations = ExternalDestinations
                .Select(destination => string.Equals(destination.Id, targetId, StringComparison.Ordinal)
                    ? destination with { IsEnabled = !destination.IsEnabled }
                    : destination)
                .ToArray()
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
