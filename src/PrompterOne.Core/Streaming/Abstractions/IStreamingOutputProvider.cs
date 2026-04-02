using PrompterOne.Core.Models.Streaming;

namespace PrompterOne.Core.Abstractions;

public interface IGoLiveProgramCaptureService
{
    Task<GoLiveProgramHandle> StartProgramAsync(string sessionId, CancellationToken cancellationToken);

    Task UpdateProgramAsync(GoLiveProgramHandle handle, CancellationToken cancellationToken);

    Task StopProgramAsync(GoLiveProgramHandle handle, CancellationToken cancellationToken);
}

public interface IGoLiveSourceModule
{
    string Id { get; }

    GoLiveModuleDescriptor Descriptor { get; }

    Task ConnectAsync(GoLiveSourceConfiguration configuration, CancellationToken cancellationToken);

    Task<IReadOnlyList<GoLiveRemoteSource>> GetSourcesAsync(CancellationToken cancellationToken);

    Task DisconnectAsync(CancellationToken cancellationToken);
}

public interface IGoLiveOutputModule
{
    string Id { get; }

    GoLiveModuleDescriptor Descriptor { get; }

    Task StartAsync(GoLiveProgramHandle program, GoLiveOutputConfiguration configuration, CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);

    Task<GoLiveOutputTelemetry> GetTelemetryAsync(CancellationToken cancellationToken);
}

public interface IGoLiveModuleRegistry
{
    IGoLiveSourceModule? ResolveSource(string moduleId);

    IGoLiveOutputModule? ResolveOutput(string moduleId);

    IReadOnlyList<GoLiveModuleDescriptor> GetSourceDescriptors();

    IReadOnlyList<GoLiveModuleDescriptor> GetOutputDescriptors();
}
