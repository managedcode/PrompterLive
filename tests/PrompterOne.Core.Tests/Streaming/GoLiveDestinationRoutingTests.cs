using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Streaming;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Core.Services.Streaming;

namespace PrompterOne.Core.Tests;

public sealed class GoLiveDestinationRoutingTests
{
    private const string FirstSourceId = "scene-cam-a";
    private const string SecondSourceId = "scene-cam-b";
    private const string UnknownSourceId = "scene-cam-missing";

    [Fact]
    public void Normalize_SeedsMissingTargetsFromProgramFeedSources()
    {
        var streaming = new StreamStudioSettings(
            ExternalDestinations:
            [
                StreamingPlatformCatalog.CreateProfile(StreamingPlatformKind.LiveKit, GoLiveTargetCatalog.TargetIds.LiveKit)
            ]);

        var normalized = GoLiveDestinationRouting.Normalize(streaming, CreateSceneCameras());

        var liveKitSources = GoLiveDestinationRouting.GetSelectedSourceIds(
            normalized,
            GoLiveTargetCatalog.TargetIds.LiveKit,
            CreateSceneCameras());

        Assert.Equal(GoLiveTargetCatalog.LocalTargetIds.Count + 1, normalized.DestinationSourceSelections?.Count);
        Assert.Equal([FirstSourceId], liveKitSources);
    }

    [Fact]
    public void ToggleSource_UpdatesOnlyRequestedTarget()
    {
        var streaming = GoLiveDestinationRouting.Normalize(
            new StreamStudioSettings(
                ExternalDestinations:
                [
                    StreamingPlatformCatalog.CreateProfile(StreamingPlatformKind.LiveKit, GoLiveTargetCatalog.TargetIds.LiveKit),
                    StreamingPlatformCatalog.CreateProfile(StreamingPlatformKind.Youtube, GoLiveTargetCatalog.TargetIds.Youtube)
                ]),
            CreateSceneCameras());

        var updated = GoLiveDestinationRouting.ToggleSource(
            streaming,
            GoLiveTargetCatalog.TargetIds.LiveKit,
            SecondSourceId,
            CreateSceneCameras());

        Assert.Equal(
            [FirstSourceId, SecondSourceId],
            GoLiveDestinationRouting.GetSelectedSourceIds(updated, GoLiveTargetCatalog.TargetIds.LiveKit, CreateSceneCameras()));
        Assert.Equal(
            [FirstSourceId],
            GoLiveDestinationRouting.GetSelectedSourceIds(updated, GoLiveTargetCatalog.TargetIds.Youtube, CreateSceneCameras()));
    }

    [Fact]
    public void Normalize_RemovesUnknownSourcesFromPersistedSelections()
    {
        var streaming = new StreamStudioSettings(
            ExternalDestinations:
            [
                StreamingPlatformCatalog.CreateProfile(StreamingPlatformKind.LiveKit, GoLiveTargetCatalog.TargetIds.LiveKit)
            ],
            DestinationSourceSelections:
            [
                new GoLiveDestinationSourceSelection(
                    GoLiveTargetCatalog.TargetIds.LiveKit,
                    [FirstSourceId, UnknownSourceId])
            ]);

        var normalized = GoLiveDestinationRouting.Normalize(streaming, CreateSceneCameras());

        Assert.Equal(
            [FirstSourceId],
            GoLiveDestinationRouting.GetSelectedSourceIds(normalized, GoLiveTargetCatalog.TargetIds.LiveKit, CreateSceneCameras()));
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
