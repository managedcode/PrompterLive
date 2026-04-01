using PrompterOne.Core.Abstractions;
using PrompterOne.Core.Models.Workspace;

namespace PrompterOne.Shared.Services;

public sealed class StudioSettingsStore(IUserSettingsStore settingsStore)
{
    public const string StorageKey = "prompterone.studio";

    private readonly IUserSettingsStore _settingsStore = settingsStore;

    public async Task<StudioSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        return await _settingsStore.LoadAsync<StudioSettings>(StorageKey, cancellationToken) ?? StudioSettings.Default;
    }

    public Task SaveAsync(StudioSettings settings, CancellationToken cancellationToken = default)
    {
        return _settingsStore.SaveAsync(StorageKey, settings, cancellationToken);
    }
}
