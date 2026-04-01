using PrompterOne.Core.Abstractions;

namespace PrompterOne.Shared.Storage.Cloud;

public sealed class BrowserCloudStorageStore(IUserSettingsStore settingsStore)
{
    private readonly IUserSettingsStore _settingsStore = settingsStore;

    public async Task<CloudStoragePreferences> LoadPreferencesAsync(CancellationToken cancellationToken = default)
    {
        var preferences = await _settingsStore.LoadAsync<CloudStoragePreferences>(
            CloudStorageStoreKeys.Preferences,
            cancellationToken);

        return (preferences ?? CloudStoragePreferences.CreateDefault()).Normalize();
    }

    public Task SavePreferencesAsync(CloudStoragePreferences preferences, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        return _settingsStore.SaveAsync(CloudStorageStoreKeys.Preferences, preferences.Normalize(), cancellationToken);
    }

    public async Task<DropboxCloudStorageCredentials> LoadDropboxCredentialsAsync(CancellationToken cancellationToken = default) =>
        await LoadAsync<DropboxCloudStorageCredentials>(CloudStorageStoreKeys.DropboxCredentials, cancellationToken) ?? new();

    public Task SaveDropboxCredentialsAsync(DropboxCloudStorageCredentials credentials, CancellationToken cancellationToken = default) =>
        SaveAsync(CloudStorageStoreKeys.DropboxCredentials, credentials, cancellationToken);

    public async Task<OneDriveCloudStorageCredentials> LoadOneDriveCredentialsAsync(CancellationToken cancellationToken = default) =>
        await LoadAsync<OneDriveCloudStorageCredentials>(CloudStorageStoreKeys.OneDriveCredentials, cancellationToken) ?? new();

    public Task SaveOneDriveCredentialsAsync(OneDriveCloudStorageCredentials credentials, CancellationToken cancellationToken = default) =>
        SaveAsync(CloudStorageStoreKeys.OneDriveCredentials, credentials, cancellationToken);

    public async Task<GoogleDriveCloudStorageCredentials> LoadGoogleDriveCredentialsAsync(CancellationToken cancellationToken = default) =>
        await LoadAsync<GoogleDriveCloudStorageCredentials>(CloudStorageStoreKeys.GoogleDriveCredentials, cancellationToken) ?? new();

    public Task SaveGoogleDriveCredentialsAsync(GoogleDriveCloudStorageCredentials credentials, CancellationToken cancellationToken = default) =>
        SaveAsync(CloudStorageStoreKeys.GoogleDriveCredentials, credentials, cancellationToken);

    public async Task<GoogleCloudStorageCredentials> LoadGoogleCloudStorageCredentialsAsync(CancellationToken cancellationToken = default) =>
        await LoadAsync<GoogleCloudStorageCredentials>(CloudStorageStoreKeys.GoogleCloudStorageCredentials, cancellationToken) ?? new();

    public Task SaveGoogleCloudStorageCredentialsAsync(GoogleCloudStorageCredentials credentials, CancellationToken cancellationToken = default) =>
        SaveAsync(CloudStorageStoreKeys.GoogleCloudStorageCredentials, credentials, cancellationToken);

    public async Task<CloudKitStorageCredentials> LoadCloudKitCredentialsAsync(CancellationToken cancellationToken = default) =>
        await LoadAsync<CloudKitStorageCredentials>(CloudStorageStoreKeys.CloudKitCredentials, cancellationToken) ?? new();

    public Task SaveCloudKitCredentialsAsync(CloudKitStorageCredentials credentials, CancellationToken cancellationToken = default) =>
        SaveAsync(CloudStorageStoreKeys.CloudKitCredentials, credentials, cancellationToken);

    public Task RemoveCredentialsAsync(string providerId, CancellationToken cancellationToken = default)
    {
        var key = providerId switch
        {
            CloudStorageProviderIds.CloudKit => CloudStorageStoreKeys.CloudKitCredentials,
            CloudStorageProviderIds.Dropbox => CloudStorageStoreKeys.DropboxCredentials,
            CloudStorageProviderIds.GoogleCloudStorage => CloudStorageStoreKeys.GoogleCloudStorageCredentials,
            CloudStorageProviderIds.GoogleDrive => CloudStorageStoreKeys.GoogleDriveCredentials,
            CloudStorageProviderIds.OneDrive => CloudStorageStoreKeys.OneDriveCredentials,
            _ => throw new InvalidOperationException($"Unknown cloud storage provider '{providerId}'.")
        };

        return _settingsStore.RemoveAsync(key, cancellationToken);
    }

    private Task<T?> LoadAsync<T>(string key, CancellationToken cancellationToken) =>
        _settingsStore.LoadAsync<T>(key, cancellationToken);

    private Task SaveAsync<T>(string key, T value, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(value);
        return _settingsStore.SaveAsync(key, value, cancellationToken);
    }
}
