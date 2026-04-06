using PrompterOne.Core.Abstractions;
using PrompterOne.Core.Models.Streaming;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Core.Services.Streaming;

namespace PrompterOne.Core.Tests;

public sealed class GoLiveModuleRegistryTests
{
    [Test]
    public void Registry_ResolvesSourceAndOutputModulesBySharedIds()
    {
        var registry = new GoLiveModuleRegistry(
            [new LiveKitSourceModule(), new VdoNinjaSourceModule()],
            [new LocalRecordingOutputModule(), new LiveKitOutputModule(), new VdoNinjaOutputModule()]);

        Assert.NotNull(registry.ResolveSource(GoLiveTargetCatalog.TargetIds.LiveKit));
        Assert.NotNull(registry.ResolveSource(GoLiveTargetCatalog.TargetIds.VdoNinja));
        Assert.NotNull(registry.ResolveOutput(GoLiveTargetCatalog.TargetIds.Recording));
        Assert.NotNull(registry.ResolveOutput(GoLiveTargetCatalog.TargetIds.LiveKit));
        Assert.NotNull(registry.ResolveOutput(GoLiveTargetCatalog.TargetIds.VdoNinja));
    }

    [Test]
    public void Registry_ExposesConcurrentPublishCapabilitiesForLiveOutputs()
    {
        var registry = new GoLiveModuleRegistry(
            Array.Empty<IGoLiveSourceModule>(),
            [new LiveKitOutputModule(), new VdoNinjaOutputModule()]);

        var descriptors = registry.GetOutputDescriptors().ToDictionary(descriptor => descriptor.Id, StringComparer.Ordinal);

        Assert.True(descriptors[GoLiveTargetCatalog.TargetIds.LiveKit].Capabilities.CanPublishProgram);
        Assert.True(descriptors[GoLiveTargetCatalog.TargetIds.LiveKit].Capabilities.SupportsConcurrentPublish);
        Assert.True(descriptors[GoLiveTargetCatalog.TargetIds.LiveKit].Capabilities.SupportsDownstreamTargets);

        Assert.True(descriptors[GoLiveTargetCatalog.TargetIds.VdoNinja].Capabilities.CanPublishProgram);
        Assert.True(descriptors[GoLiveTargetCatalog.TargetIds.VdoNinja].Capabilities.SupportsConcurrentPublish);
        Assert.True(descriptors[GoLiveTargetCatalog.TargetIds.VdoNinja].Capabilities.SupportsPublishUrl);
    }

    [Test]
    public void Catalog_CreatesTransportConnectionsAndTargetsWithNewProfiles()
    {
        var liveKit = StreamingPlatformCatalog.CreateTransportConnection(StreamingPlatformKind.LiveKit);
        var vdoNinja = StreamingPlatformCatalog.CreateTransportConnection(
            StreamingPlatformKind.VdoNinja,
            [GoLiveTargetCatalog.TargetIds.VdoNinja]);
        var youtube = StreamingPlatformCatalog.CreateDistributionTarget(StreamingPlatformKind.Youtube);

        Assert.Equal(StreamingTransportRole.Both, liveKit.Roles);
        Assert.Equal(string.Empty, liveKit.BaseUrl);

        Assert.Equal($"{GoLiveTargetCatalog.TargetIds.VdoNinja}-2", vdoNinja.Id);
        Assert.Equal(VdoNinjaDefaults.HostedBaseUrl, vdoNinja.BaseUrl);
        Assert.Equal(StreamingTransportRole.Both, vdoNinja.Roles);

        Assert.Equal(StreamingPlatformKind.Youtube, youtube.PlatformKind);
        Assert.Equal("rtmps://a.rtmp.youtube.com/live2", youtube.RtmpUrl);
        Assert.Empty(youtube.BoundTransportConnectionIds ?? []);
    }
}
