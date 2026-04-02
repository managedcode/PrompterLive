using PrompterOne.Core.Abstractions;
using PrompterOne.Core.Models.Streaming;

namespace PrompterOne.Shared.Services;

public sealed class StreamingPublishDescriptorResolver(IEnumerable<IStreamingOutputProvider> providers)
{
    private readonly IReadOnlyDictionary<StreamingProviderKind, IStreamingOutputProvider> _providers = providers
        .GroupBy(provider => provider.Kind)
        .ToDictionary(group => group.Key, group => group.First());

    public StreamingPublishDescriptor Describe(StreamingProfile profile)
    {
        if (_providers.TryGetValue(profile.ProviderKind, out var provider))
        {
            return provider.Describe(profile);
        }

        return new StreamingPublishDescriptor(
            ProviderId: profile.ProviderKind.ToString(),
            ProviderKind: profile.ProviderKind,
            DisplayName: profile.Name,
            IsReady: false,
            RequiresExternalRelay: false,
            Summary: "No streaming provider is registered for this destination.",
            Parameters: new Dictionary<string, string>(StringComparer.Ordinal));
    }
}
