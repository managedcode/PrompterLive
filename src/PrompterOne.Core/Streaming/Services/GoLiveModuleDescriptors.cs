using PrompterOne.Core.Models.Streaming;
using PrompterOne.Core.Models.Workspace;

namespace PrompterOne.Core.Services.Streaming;

internal static class GoLiveModuleDescriptors
{
    public const string LiveKitModuleId = GoLiveTargetCatalog.TargetIds.LiveKit;
    public const string LocalRecordingModuleId = GoLiveTargetCatalog.TargetIds.Recording;
    public const string VdoNinjaModuleId = GoLiveTargetCatalog.TargetIds.VdoNinja;

    public static GoLiveModuleDescriptor LiveKit { get; } = new(
        LiveKitModuleId,
        GoLiveTargetCatalog.TargetNames.LiveKit,
        new GoLiveModuleCapabilities(
            CanIngestRemoteSources: true,
            CanPublishProgram: true,
            SupportsCustomServer: true,
            SupportsHostedMode: true,
            SupportsPublishUrl: false,
            SupportsDownstreamTargets: true,
            RequiresPrivateBackendControl: true,
            SupportsConcurrentPublish: true));

    public static GoLiveModuleDescriptor LocalRecording { get; } = new(
        LocalRecordingModuleId,
        GoLiveTargetCatalog.TargetNames.Recording,
        new GoLiveModuleCapabilities(
            CanIngestRemoteSources: false,
            CanPublishProgram: true,
            SupportsCustomServer: false,
            SupportsHostedMode: false,
            SupportsPublishUrl: false,
            SupportsDownstreamTargets: false,
            RequiresPrivateBackendControl: false,
            SupportsConcurrentPublish: true));

    public static GoLiveModuleDescriptor VdoNinja { get; } = new(
        VdoNinjaModuleId,
        GoLiveTargetCatalog.TargetNames.VdoNinja,
        new GoLiveModuleCapabilities(
            CanIngestRemoteSources: true,
            CanPublishProgram: true,
            SupportsCustomServer: true,
            SupportsHostedMode: true,
            SupportsPublishUrl: true,
            SupportsDownstreamTargets: false,
            RequiresPrivateBackendControl: false,
            SupportsConcurrentPublish: true));
}
