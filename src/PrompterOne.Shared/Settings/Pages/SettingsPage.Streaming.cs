using Microsoft.AspNetCore.Components;
using PrompterOne.Core.Models.Streaming;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Core.Services.Streaming;

namespace PrompterOne.Shared.Pages;

public partial class SettingsPage
{
    private static readonly IReadOnlyDictionary<string, StreamingTransportRole> TransportRoleValues =
        new Dictionary<string, StreamingTransportRole>(StringComparer.Ordinal)
        {
            [nameof(StreamingTransportRole.Both)] = StreamingTransportRole.Both,
            [nameof(StreamingTransportRole.Publish)] = StreamingTransportRole.Publish,
            [nameof(StreamingTransportRole.Source)] = StreamingTransportRole.Source
        };

    private Task ToggleRecordingOutputAsync() =>
        UpdateStreamingSettingsAsync(streaming => streaming with
        {
            Recording = streaming.RecordingSettings with
            {
                IsEnabled = !streaming.RecordingSettings.IsEnabled
            }
        });

    private async Task AddTransportConnectionAsync(StreamingPlatformKind kind)
    {
        var connection = StreamingPlatformCatalog.CreateTransportConnection(
            kind,
            (_studioSettings.Streaming.TransportConnections ?? Array.Empty<TransportConnectionProfile>())
                .Select(existingConnection => existingConnection.Id));

        await UpdateStreamingSettingsAsync(streaming => streaming with
        {
            TransportConnections = (streaming.TransportConnections ?? Array.Empty<TransportConnectionProfile>())
                .Append(connection)
                .ToArray()
        });
    }

    private Task ToggleTransportConnectionAsync(string connectionId) =>
        UpdateStreamingSettingsAsync(
            streaming => UpdateTransportConnection(
                streaming,
                connectionId,
                connection => connection with { IsEnabled = !connection.IsEnabled }));

    private Task RemoveTransportConnectionAsync(string connectionId) =>
        UpdateStreamingSettingsAsync(streaming => streaming with
        {
            TransportConnections = (streaming.TransportConnections ?? Array.Empty<TransportConnectionProfile>())
                .Where(connection => !string.Equals(connection.Id, connectionId, StringComparison.Ordinal))
                .ToArray(),
            DistributionTargets = (streaming.DistributionTargets ?? Array.Empty<DistributionTargetProfile>())
                .Select(target => target with
                {
                    BoundTransportConnectionIds = target.GetBoundTransportConnectionIds()
                        .Where(id => !string.Equals(id, connectionId, StringComparison.Ordinal))
                        .ToArray()
                })
                .ToArray()
        });

    private Task UpdateTransportConnectionFieldAsync((string ConnectionId, string FieldId, string Value) update) =>
        UpdateStreamingSettingsAsync(
            streaming => UpdateTransportConnection(
                streaming,
                update.ConnectionId,
                connection => ApplyTransportConnectionField(connection, update.FieldId, update.Value)));

    private Task UpdateTransportConnectionRoleAsync((string ConnectionId, string Value) update) =>
        UpdateStreamingSettingsAsync(
            streaming => UpdateTransportConnection(
                streaming,
                update.ConnectionId,
                connection => connection with
                {
                    Roles = TransportRoleValues.GetValueOrDefault(update.Value, StreamingTransportRole.Both)
                }));

    private async Task AddDistributionTargetAsync(StreamingPlatformKind kind)
    {
        var target = StreamingPlatformCatalog.CreateDistributionTarget(
            kind,
            (_studioSettings.Streaming.DistributionTargets ?? Array.Empty<DistributionTargetProfile>())
                .Select(existingTarget => existingTarget.Id));

        await UpdateStreamingSettingsAsync(streaming => streaming with
        {
            DistributionTargets = (streaming.DistributionTargets ?? Array.Empty<DistributionTargetProfile>())
                .Append(target)
                .ToArray()
        });
    }

    private Task ToggleDistributionTargetAsync(string targetId) =>
        UpdateStreamingSettingsAsync(
            streaming => UpdateDistributionTarget(
                streaming,
                targetId,
                target => target with { IsEnabled = !target.IsEnabled }));

    private Task RemoveDistributionTargetAsync(string targetId) =>
        UpdateStreamingSettingsAsync(streaming => streaming with
        {
            DistributionTargets = (streaming.DistributionTargets ?? Array.Empty<DistributionTargetProfile>())
                .Where(target => !string.Equals(target.Id, targetId, StringComparison.Ordinal))
                .ToArray()
        });

    private Task UpdateDistributionTargetFieldAsync((string TargetId, string FieldId, string Value) update) =>
        UpdateStreamingSettingsAsync(
            streaming => UpdateDistributionTarget(
                streaming,
                update.TargetId,
                target => ApplyDistributionTargetField(target, update.FieldId, update.Value)));

    private Task ToggleDistributionTargetTransportAsync((string TargetId, string ConnectionId) update) =>
        UpdateStreamingSettingsAsync(
            streaming => UpdateDistributionTarget(
                streaming,
                update.TargetId,
                target =>
                {
                    var boundIds = target.GetBoundTransportConnectionIds().ToList();
                    if (boundIds.Contains(update.ConnectionId, StringComparer.Ordinal))
                    {
                        boundIds.RemoveAll(id => string.Equals(id, update.ConnectionId, StringComparison.Ordinal));
                    }
                    else
                    {
                        boundIds.Add(update.ConnectionId);
                    }

                    return target with { BoundTransportConnectionIds = boundIds.ToArray() };
                }));

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

        await UpdateStreamingSettingsAsync(streaming => streaming with
        {
            ProgramCapture = streaming.ProgramCaptureSettings with { ResolutionPreset = outputResolution }
        });
    }

    private async Task UpdateStreamingBitrateAsync(ChangeEventArgs args)
    {
        if (!int.TryParse(args.Value?.ToString(), out var bitrate))
        {
            return;
        }

        await UpdateStreamingSettingsAsync(streaming => streaming with
        {
            ProgramCapture = streaming.ProgramCaptureSettings with
            {
                BitrateKbps = Math.Max(250, bitrate)
            }
        });
    }

    private Task ToggleSettingsTextOverlayAsync() =>
        UpdateStreamingSettingsAsync(streaming => streaming with
        {
            ProgramCapture = streaming.ProgramCaptureSettings with
            {
                ShowTextOverlay = !streaming.ProgramCaptureSettings.ShowTextOverlay
            }
        });

    private async Task ToggleSettingsIncludeCameraAsync()
    {
        var nextValue = !_studioSettings.Streaming.ProgramCaptureSettings.IncludeCameraInOutput;
        foreach (var camera in _sceneCameras)
        {
            MediaSceneService.SetIncludeInOutput(camera.SourceId, nextValue);
        }

        await PersistSceneAsync();
        await UpdateStreamingSettingsAsync(streaming => streaming with
        {
            ProgramCapture = streaming.ProgramCaptureSettings with
            {
                IncludeCameraInOutput = nextValue
            }
        });
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

    private static StreamStudioSettings UpdateTransportConnection(
        StreamStudioSettings streaming,
        string connectionId,
        Func<TransportConnectionProfile, TransportConnectionProfile> update)
    {
        return streaming with
        {
            TransportConnections = (streaming.TransportConnections ?? Array.Empty<TransportConnectionProfile>())
                .Select(connection => string.Equals(connection.Id, connectionId, StringComparison.Ordinal)
                    ? update(connection)
                    : connection)
                .ToArray()
        };
    }

    private static StreamStudioSettings UpdateDistributionTarget(
        StreamStudioSettings streaming,
        string targetId,
        Func<DistributionTargetProfile, DistributionTargetProfile> update)
    {
        return streaming with
        {
            DistributionTargets = (streaming.DistributionTargets ?? Array.Empty<DistributionTargetProfile>())
                .Select(target => string.Equals(target.Id, targetId, StringComparison.Ordinal)
                    ? update(target)
                    : target)
                .ToArray()
        };
    }

    private static TransportConnectionProfile ApplyTransportConnectionField(
        TransportConnectionProfile connection,
        string fieldId,
        string value)
    {
        return fieldId switch
        {
            StreamingDestinationFieldIds.Name => connection with { Name = value },
            StreamingDestinationFieldIds.ServerUrl => connection with { ServerUrl = value },
            StreamingDestinationFieldIds.BaseUrl => connection with { BaseUrl = value },
            StreamingDestinationFieldIds.RoomName => connection with { RoomName = value },
            StreamingDestinationFieldIds.Token => connection with { Token = value },
            StreamingDestinationFieldIds.PublishUrl => connection with { PublishUrl = value },
            StreamingDestinationFieldIds.ViewUrl => connection with { ViewUrl = value },
            _ => connection
        };
    }

    private static DistributionTargetProfile ApplyDistributionTargetField(
        DistributionTargetProfile target,
        string fieldId,
        string value)
    {
        return fieldId switch
        {
            StreamingDestinationFieldIds.Name => target with { Name = value },
            StreamingDestinationFieldIds.RtmpUrl => target with { RtmpUrl = value },
            StreamingDestinationFieldIds.StreamKey => target with { StreamKey = value },
            _ => target
        };
    }
}
