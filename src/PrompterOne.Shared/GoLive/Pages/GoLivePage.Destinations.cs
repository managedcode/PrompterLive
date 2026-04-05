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

    private IReadOnlyList<DistributionTargetProfile> DistributionTargets =>
        _studioSettings.Streaming.DistributionTargets ?? Array.Empty<DistributionTargetProfile>();

    private IReadOnlyList<TransportConnectionProfile> TransportConnections =>
        _studioSettings.Streaming.TransportConnections ?? Array.Empty<TransportConnectionProfile>();

    private IReadOnlyList<GoLiveDestinationSummaryViewModel> BuildDestinationSummary()
    {
        return TransportConnections
            .Select(BuildTransportConnectionSummary)
            .Concat(DistributionTargets.Select(BuildDistributionTargetSummary))
            .ToArray();
    }

    private GoLiveDestinationSummaryViewModel BuildDistributionTargetSummary(DistributionTargetProfile target)
    {
        var descriptor = StreamingDescriptorResolver.DescribeTarget(target, TransportConnections);
        var presentation = StreamingPlatformPresentationCatalog.Get(target.PlatformKind);

        return new GoLiveDestinationSummaryViewModel(
            target.Id,
            target.Name,
            presentation.GoLivePlatformLabel,
            target.IsEnabled,
            target.IsEnabled && descriptor.IsSupported && descriptor.IsReady,
            BuildTargetSummary(target, descriptor),
            BuildTargetStatusLabel(target, descriptor),
            presentation.Tone);
    }

    private GoLiveDestinationSummaryViewModel BuildTransportConnectionSummary(TransportConnectionProfile connection)
    {
        var descriptor = StreamingDescriptorResolver.DescribeTransport(connection);
        var presentation = StreamingPlatformPresentationCatalog.Get(connection.PlatformKind);

        return new GoLiveDestinationSummaryViewModel(
            connection.Id,
            connection.Name,
            presentation.GoLivePlatformLabel,
            connection.IsEnabled,
            BuildTransportConnectionIsReady(connection, descriptor),
            BuildTransportSummary(connection, descriptor),
            BuildTransportStatusLabel(connection, descriptor),
            presentation.Tone);
    }

    private TransportConnectionProfile? ResolvePrimaryRoomDestination()
    {
        return TransportConnections.FirstOrDefault(connection =>
                   connection.IsEnabled
                   && connection.PlatformKind == StreamingPlatformKind.VdoNinja
                   && (!string.IsNullOrWhiteSpace(connection.PublishUrl)
                       || !string.IsNullOrWhiteSpace(connection.RoomName)))
            ?? TransportConnections.FirstOrDefault(connection =>
                   connection.IsEnabled
                   && connection.PlatformKind == StreamingPlatformKind.LiveKit
                   && (!string.IsNullOrWhiteSpace(connection.RoomName)
                       || !string.IsNullOrWhiteSpace(connection.ServerUrl)
                       || !string.IsNullOrWhiteSpace(connection.Token)));
    }

    private async Task ToggleDestinationSummaryAsync(string targetId)
    {
        await UpdateGoLiveStreamingSettingsAsync(streaming =>
        {
            if (TransportConnections.Any(connection => string.Equals(connection.Id, targetId, StringComparison.Ordinal)))
            {
                return streaming with
                {
                    TransportConnections = TransportConnections
                        .Select(connection => string.Equals(connection.Id, targetId, StringComparison.Ordinal)
                            ? connection with { IsEnabled = !connection.IsEnabled }
                            : connection)
                        .ToArray()
                };
            }

            return streaming with
            {
                DistributionTargets = DistributionTargets
                    .Select(target => string.Equals(target.Id, targetId, StringComparison.Ordinal)
                        ? target with { IsEnabled = !target.IsEnabled }
                        : target)
                    .ToArray()
            };
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
        await SyncRemoteSourcesAsync();
    }

    private string BuildTargetSummary(DistributionTargetProfile target, StreamingPublishDescriptor descriptor)
    {
        if (!target.IsEnabled)
        {
            return Text(GoLiveText.Destination.DisabledSummary);
        }

        if (target.GetBoundTransportConnectionIds().Count == 0)
        {
            return Text(GoLiveText.Destination.TransportBindingSummary);
        }

        if (!descriptor.IsSupported)
        {
            return Text(GoLiveText.Destination.BlockedSummary);
        }

        return descriptor.Summary;
    }

    private string BuildTargetStatusLabel(DistributionTargetProfile target, StreamingPublishDescriptor descriptor)
    {
        if (!target.IsEnabled)
        {
            return Text(GoLiveText.Destination.DisabledStatusLabel);
        }

        if (!descriptor.IsSupported)
        {
            return Text(GoLiveText.Destination.BlockedStatusLabel);
        }

        if (!descriptor.IsReady)
        {
            return Text(GoLiveText.Destination.NeedsSetupStatusLabel);
        }

        return Text(GoLiveText.Destination.RelayStatusLabel);
    }

    private bool BuildTransportConnectionIsReady(TransportConnectionProfile connection, StreamingPublishDescriptor descriptor)
    {
        if (!connection.IsEnabled || !descriptor.IsReady)
        {
            return false;
        }

        return GetSelectedSourceIds(connection.Id).Count > 0;
    }

    private string BuildTransportStatusLabel(TransportConnectionProfile connection, StreamingPublishDescriptor descriptor)
    {
        if (!connection.IsEnabled)
        {
            return Text(GoLiveText.Destination.DisabledStatusLabel);
        }

        if (!BuildTransportConnectionIsReady(connection, descriptor))
        {
            return Text(GoLiveText.Destination.NeedsSetupStatusLabel);
        }

        return Text(GoLiveText.Destination.EnabledStatusLabel);
    }

    private string BuildTransportSummary(TransportConnectionProfile connection, StreamingPublishDescriptor descriptor)
    {
        if (!connection.IsEnabled)
        {
            return Text(GoLiveText.Destination.DisabledSummary);
        }

        if (GetSelectedSourceIds(connection.Id).Count == 0)
        {
            return Text(GoLiveText.Destination.NoSourceSummary);
        }

        return descriptor.Summary;
    }
}
