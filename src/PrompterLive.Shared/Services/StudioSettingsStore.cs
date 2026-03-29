using PrompterLive.Core.Models.Workspace;

namespace PrompterLive.Shared.Services;

public sealed class StudioSettingsStore
{
    public const string StorageKey = "prompterlive.studio";

    private readonly BrowserSettingsStore _settingsStore;

    public StudioSettingsStore(BrowserSettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
    }

    public async Task<StudioSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        return await _settingsStore.LoadAsync<StudioSettings>(StorageKey, cancellationToken) ?? StudioSettings.Default;
    }

    public Task SaveAsync(StudioSettings settings, CancellationToken cancellationToken = default)
    {
        return _settingsStore.SaveAsync(StorageKey, settings, cancellationToken);
    }
}
