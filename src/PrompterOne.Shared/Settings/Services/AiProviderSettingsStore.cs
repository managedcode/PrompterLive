using Microsoft.Extensions.Configuration;
using PrompterOne.Core.Abstractions;
using PrompterOne.Shared.Settings.Models;

namespace PrompterOne.Shared.Settings.Services;

public sealed class AiProviderSettingsStore(IUserSettingsStore settingsStore, IConfiguration configuration)
{
    private readonly IUserSettingsStore _settingsStore = settingsStore;
    private readonly IConfiguration _configuration = configuration;

    public async Task<AiProviderSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsStore.LoadAsync<AiProviderSettings>(AiProviderSettings.StorageKey, cancellationToken);
        if (settings is not null)
        {
            return settings.Normalize();
        }

        return (AiProviderAppSettingsFactory.Create(_configuration) ?? AiProviderSettings.CreateDefault()).Normalize();
    }

    public Task SaveAsync(AiProviderSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return _settingsStore.SaveAsync(AiProviderSettings.StorageKey, settings.Normalize(), cancellationToken);
    }
}
