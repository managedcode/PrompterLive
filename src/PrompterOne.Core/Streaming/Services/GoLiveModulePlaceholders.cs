using PrompterOne.Core.Abstractions;
using PrompterOne.Core.Models.Streaming;

namespace PrompterOne.Core.Services.Streaming;

public sealed class LiveKitSourceModule : IGoLiveSourceModule
{
    public string Id => GoLiveModuleDescriptors.LiveKitModuleId;

    public GoLiveModuleDescriptor Descriptor => GoLiveModuleDescriptors.LiveKit;

    public Task ConnectAsync(GoLiveSourceConfiguration configuration, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task DisconnectAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<IReadOnlyList<GoLiveRemoteSource>> GetSourcesAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<GoLiveRemoteSource>>(Array.Empty<GoLiveRemoteSource>());
}

public sealed class LiveKitOutputModule : IGoLiveOutputModule
{
    public string Id => GoLiveModuleDescriptors.LiveKitModuleId;

    public GoLiveModuleDescriptor Descriptor => GoLiveModuleDescriptors.LiveKit;

    public Task<GoLiveOutputTelemetry> GetTelemetryAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new GoLiveOutputTelemetry(false, "Browser runtime controlled by Go Live output interop."));

    public Task StartAsync(GoLiveProgramHandle program, GoLiveOutputConfiguration configuration, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class LocalRecordingOutputModule : IGoLiveOutputModule
{
    public string Id => GoLiveModuleDescriptors.LocalRecordingModuleId;

    public GoLiveModuleDescriptor Descriptor => GoLiveModuleDescriptors.LocalRecording;

    public Task<GoLiveOutputTelemetry> GetTelemetryAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new GoLiveOutputTelemetry(false, "Browser runtime controlled by Go Live output interop."));

    public Task StartAsync(GoLiveProgramHandle program, GoLiveOutputConfiguration configuration, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class VdoNinjaSourceModule : IGoLiveSourceModule
{
    public string Id => GoLiveModuleDescriptors.VdoNinjaModuleId;

    public GoLiveModuleDescriptor Descriptor => GoLiveModuleDescriptors.VdoNinja;

    public Task ConnectAsync(GoLiveSourceConfiguration configuration, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task DisconnectAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<IReadOnlyList<GoLiveRemoteSource>> GetSourcesAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<GoLiveRemoteSource>>(Array.Empty<GoLiveRemoteSource>());
}

public sealed class VdoNinjaOutputModule : IGoLiveOutputModule
{
    public string Id => GoLiveModuleDescriptors.VdoNinjaModuleId;

    public GoLiveModuleDescriptor Descriptor => GoLiveModuleDescriptors.VdoNinja;

    public Task<GoLiveOutputTelemetry> GetTelemetryAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new GoLiveOutputTelemetry(false, "Browser runtime controlled by Go Live output interop."));

    public Task StartAsync(GoLiveProgramHandle program, GoLiveOutputConfiguration configuration, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
