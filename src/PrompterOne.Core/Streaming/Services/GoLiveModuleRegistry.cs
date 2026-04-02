using PrompterOne.Core.Abstractions;
using PrompterOne.Core.Models.Streaming;

namespace PrompterOne.Core.Services.Streaming;

public sealed class GoLiveModuleRegistry(
    IEnumerable<IGoLiveSourceModule> sourceModules,
    IEnumerable<IGoLiveOutputModule> outputModules) : IGoLiveModuleRegistry
{
    private readonly IReadOnlyDictionary<string, IGoLiveSourceModule> _sourceModules = sourceModules
        .ToDictionary(module => module.Id, StringComparer.Ordinal);

    private readonly IReadOnlyDictionary<string, IGoLiveOutputModule> _outputModules = outputModules
        .ToDictionary(module => module.Id, StringComparer.Ordinal);

    public IReadOnlyList<GoLiveModuleDescriptor> GetOutputDescriptors() =>
        _outputModules.Values.Select(module => module.Descriptor).ToArray();

    public IReadOnlyList<GoLiveModuleDescriptor> GetSourceDescriptors() =>
        _sourceModules.Values.Select(module => module.Descriptor).ToArray();

    public IGoLiveOutputModule? ResolveOutput(string moduleId) =>
        _outputModules.GetValueOrDefault(moduleId);

    public IGoLiveSourceModule? ResolveSource(string moduleId) =>
        _sourceModules.GetValueOrDefault(moduleId);
}
