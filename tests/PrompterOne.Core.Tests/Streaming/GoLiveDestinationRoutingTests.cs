using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Streaming;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Core.Services.Streaming;

namespace PrompterOne.Core.Tests;

public sealed class GoLiveDestinationRoutingTests
{
    private const string FirstSourceId = "scene-cam-a";
    private const string SecondSourceId = "scene-cam-b";
    private const string LiveKitConnectionId = "livekit-main";
    private const string VdoConnectionId = "vdo-main";
    private const string UnknownSourceId = "scene-cam-missing";

    [Test]
    public void Normalize_SeedsMissingTargetsFromProgramFeedSources()
    {
        var streaming = new StreamStudioSettings(
            TransportConnections:
            [
                new TransportConnectionProfile(
                    LiveKitConnectionId,
                    "LiveKit Main",
                    StreamingPlatformKind.LiveKit)
            ]);

        var normalized = GoLiveDestinationRouting.Normalize(streaming, CreateSceneCameras());

        var liveKitSources = GoLiveDestinationRouting.GetSelectedSourceIds(
            normalized,
            LiveKitConnectionId,
            CreateSceneCameras());

        Assert.Equal(GoLiveTargetCatalog.LocalTargetIds.Count + 1, normalized.SourceSelections?.Count);
        Assert.Equal([FirstSourceId], liveKitSources);
    }

    [Test]
    public void ToggleSource_UpdatesOnlyRequestedTarget()
    {
        var streaming = GoLiveDestinationRouting.Normalize(
            new StreamStudioSettings(
                TransportConnections:
                [
                    new TransportConnectionProfile(
                        LiveKitConnectionId,
                        "LiveKit Main",
                        StreamingPlatformKind.LiveKit),
                    new TransportConnectionProfile(
                        VdoConnectionId,
                        "VDO Main",
                        StreamingPlatformKind.VdoNinja)
                ]),
            CreateSceneCameras());

        var updated = GoLiveDestinationRouting.ToggleSource(
            streaming,
            LiveKitConnectionId,
            SecondSourceId,
            CreateSceneCameras());

        Assert.Equal(
            [FirstSourceId, SecondSourceId],
            GoLiveDestinationRouting.GetSelectedSourceIds(updated, LiveKitConnectionId, CreateSceneCameras()));
        Assert.Equal(
            [FirstSourceId],
            GoLiveDestinationRouting.GetSelectedSourceIds(updated, VdoConnectionId, CreateSceneCameras()));
    }

    [Test]
    public void Normalize_RemovesUnknownSourcesFromPersistedSelections()
    {
        var streaming = new StreamStudioSettings(
            TransportConnections:
            [
                new TransportConnectionProfile(
                    LiveKitConnectionId,
                    "LiveKit Main",
                    StreamingPlatformKind.LiveKit)
            ],
            SourceSelections:
            [
                new GoLiveDestinationSourceSelection(
                    LiveKitConnectionId,
                    [FirstSourceId, UnknownSourceId])
            ]);

        var normalized = GoLiveDestinationRouting.Normalize(streaming, CreateSceneCameras());

        Assert.Equal(
            [FirstSourceId],
            GoLiveDestinationRouting.GetSelectedSourceIds(normalized, LiveKitConnectionId, CreateSceneCameras()));
    }

    [Test]
    public void Normalize_DoesNotCreateSourceSelectionsForDistributionTargets()
    {
        var streaming = new StreamStudioSettings(
            TransportConnections:
            [
                new TransportConnectionProfile(
                    LiveKitConnectionId,
                    "LiveKit Main",
                    StreamingPlatformKind.LiveKit)
            ],
            DistributionTargets:
            [
                new DistributionTargetProfile(
                    "youtube-primary",
                    "YouTube Primary",
                    StreamingPlatformKind.Youtube,
                    BoundTransportConnectionIds: [LiveKitConnectionId])
            ]);

        var normalized = GoLiveDestinationRouting.Normalize(streaming, CreateSceneCameras());

        Assert.DoesNotContain(
            normalized.SourceSelections ?? [],
            selection => string.Equals(selection.TargetId, "youtube-primary", StringComparison.Ordinal));
    }

    private static IReadOnlyList<SceneCameraSource> CreateSceneCameras() =>
    [
        new(
            FirstSourceId,
            "cam-1",
            "Front camera",
            new MediaSourceTransform(IncludeInOutput: true)),
        new(
            SecondSourceId,
            "cam-2",
            "Desk camera",
            new MediaSourceTransform(IncludeInOutput: false))
    ];
}
