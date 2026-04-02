namespace PrompterOne.Core.Models.Streaming;

public sealed record GoLiveModuleCapabilities(
    bool CanIngestRemoteSources,
    bool CanPublishProgram,
    bool SupportsCustomServer,
    bool SupportsHostedMode,
    bool SupportsPublishUrl,
    bool SupportsDownstreamTargets,
    bool RequiresPrivateBackendControl,
    bool SupportsConcurrentPublish);

public sealed record GoLiveModuleDescriptor(
    string Id,
    string DisplayName,
    GoLiveModuleCapabilities Capabilities);

public sealed record GoLiveProgramHandle(
    string SessionId);

public sealed record GoLiveSourceConfiguration(
    TransportConnectionProfile Connection);

public sealed record GoLiveOutputConfiguration(
    TransportConnectionProfile Connection);

public sealed record GoLiveRemoteSource(
    string Id,
    string Label,
    bool IsConnected);

public sealed record GoLiveOutputTelemetry(
    bool IsActive,
    string Summary,
    IReadOnlyDictionary<string, string>? Metrics = null);
