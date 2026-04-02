using PrompterOne.Core.Abstractions;
using PrompterOne.Shared.Settings.Models;

namespace PrompterOne.Shared.Settings.Services;

public sealed class AiProviderSettingsStore(IUserSettingsStore settingsStore)
{
    private readonly IUserSettingsStore _settingsStore = settingsStore;

    public async Task<AiProviderSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsStore.LoadAsync<AiProviderSettings>(AiProviderSettings.StorageKey, cancellationToken);
        return (settings ?? AiProviderSettings.CreateDefault()).Normalize();
    }

    public Task SaveAsync(AiProviderSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return _settingsStore.SaveAsync(AiProviderSettings.StorageKey, settings.Normalize(), cancellationToken);
    }
}
