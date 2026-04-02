using ManagedCode.Storage.CloudKit.Options;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Settings.Components;
using PrompterOne.Shared.Storage.Cloud;

namespace PrompterOne.Shared.Components.Settings;

public partial class SettingsCloudSection : ComponentBase
{
    private const string ConnectedStatusClass = "set-dest-ok";
    private const string ConnectedStatusLabel = "Connected";
    private const string DisconnectedSubtitle = "Not connected";
    private const string DisconnectedStatusLabel = "Disconnected";
    private const string OnCssClass = "on";
    private const string SetToggleCssClass = "set-toggle";

    private CloudStoragePreferences _preferences = CloudStoragePreferences.CreateDefault();
    private DropboxCloudStorageCredentials _dropboxCredentials = new();
    private OneDriveCloudStorageCredentials _oneDriveCredentials = new();
    private GoogleDriveCloudStorageCredentials _googleDriveCredentials = new();
    private GoogleCloudStorageCredentials _googleCloudStorageCredentials = new();
    private CloudKitStorageCredentials _cloudKitCredentials = new();
    private readonly Dictionary<string, string> _providerMessages = new(StringComparer.Ordinal);

    [Inject] private BrowserCloudStorageStore CloudStorageStore { get; set; } = null!;

    [Inject] private CloudStorageProviderFactory CloudStorageProviderFactory { get; set; } = null!;

    [Inject] private CloudStorageTransferService CloudStorageTransferService { get; set; } = null!;

    [Parameter] public string DisplayStyle { get; set; } = string.Empty;

    [Parameter] public Func<string, bool> IsCardOpen { get; set; } = static _ => false;

    [Parameter] public EventCallback SettingsImported { get; set; }

    [Parameter] public EventCallback<string> ToggleCard { get; set; }

    private static readonly IReadOnlyList<SettingsSelectOption> DefaultProviderOptions =
    [
        new(CloudStorageProviderIds.OneDrive, "OneDrive"),
        new(CloudStorageProviderIds.GoogleDrive, "Google Drive"),
        new(CloudStorageProviderIds.Dropbox, "Dropbox"),
        new(CloudStorageProviderIds.GoogleCloudStorage, "Google Cloud Storage"),
        new(CloudStorageProviderIds.CloudKit, "CloudKit"),
    ];

    private static readonly IReadOnlyList<SettingsSelectOption> CloudKitDatabaseOptions =
        Enum.GetValues<CloudKitDatabase>()
            .Select(d => new SettingsSelectOption(d.ToString(), d.ToString()))
            .ToArray();

    private static readonly IReadOnlyList<SettingsSelectOption> CloudKitEnvironmentOptions =
        Enum.GetValues<CloudKitEnvironment>()
            .Select(e => new SettingsSelectOption(e.ToString(), e.ToString()))
            .ToArray();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await LoadStateAsync();
        await EnsureProviderCardOpenAsync(_preferences.PrimaryProviderId);
        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadStateAsync()
    {
        _preferences = await CloudStorageStore.LoadPreferencesAsync();
        _dropboxCredentials = await CloudStorageStore.LoadDropboxCredentialsAsync();
        _oneDriveCredentials = await CloudStorageStore.LoadOneDriveCredentialsAsync();
        _googleDriveCredentials = await CloudStorageStore.LoadGoogleDriveCredentialsAsync();
        _googleCloudStorageCredentials = await CloudStorageStore.LoadGoogleCloudStorageCredentialsAsync();
        _cloudKitCredentials = await CloudStorageStore.LoadCloudKitCredentialsAsync();
    }

    private async Task OnPrimaryProviderChangedAsync(ChangeEventArgs args)
    {
        _preferences.PrimaryProviderId = args.Value?.ToString() ?? CloudStorageProviderIds.OneDrive;
        await PersistPreferencesAsync();
        await EnsureProviderCardOpenAsync(_preferences.PrimaryProviderId);
    }

    private async Task ToggleAutoSyncOnSaveAsync()
    {
        _preferences.AutoSyncOnSave = !_preferences.AutoSyncOnSave;
        await PersistPreferencesAsync();
    }

    private async Task ToggleSyncOnStartupAsync()
    {
        _preferences.SyncOnStartup = !_preferences.SyncOnStartup;
        await PersistPreferencesAsync();
    }

    private async Task OnCloudKitDatabaseChanged(ChangeEventArgs args)
    {
        if (Enum.TryParse<CloudKitDatabase>(args.Value?.ToString(), out var database))
        {
            _preferences.CloudKit.Database = database;
            await PersistPreferencesAsync();
        }
    }

    private async Task OnCloudKitEnvironmentChanged(ChangeEventArgs args)
    {
        if (Enum.TryParse<CloudKitEnvironment>(args.Value?.ToString(), out var environment))
        {
            _preferences.CloudKit.Environment = environment;
            await PersistPreferencesAsync();
        }
    }

    private async Task ToggleGoogleDriveAllDrivesAsync()
    {
        _preferences.GoogleDrive.SupportsAllDrives = !_preferences.GoogleDrive.SupportsAllDrives;
        await PersistPreferencesAsync();
    }

    private async Task SaveAndValidateAsync(string providerId)
    {
        await PersistProviderAsync(providerId);
        var result = await CloudStorageProviderFactory.ValidateAsync(_preferences, providerId);
        var connection = GetConnection(providerId);
        connection.IsConnected = result.IsSuccess;
        connection.LastError = result.IsSuccess ? null : result.Message;
        connection.LastValidatedAt = DateTimeOffset.UtcNow;
        _providerMessages[providerId] = result.Message;
        await PersistPreferencesAsync();
    }

    private async Task ExportAsync(string providerId)
    {
        var result = await CloudStorageTransferService.ExportAsync(_preferences, providerId);
        _providerMessages[providerId] = result.Message;
    }

    private async Task ImportAsync(string providerId)
    {
        var result = await CloudStorageTransferService.ImportAsync(_preferences, providerId);
        _providerMessages[providerId] = result.Message;

        if (result.IsSuccess)
        {
            await SettingsImported.InvokeAsync();
        }
    }

    private async Task DisconnectAsync(string providerId)
    {
        await CloudStorageStore.RemoveCredentialsAsync(providerId);
        ResetProvider(providerId);
        _providerMessages[providerId] = "Provider disconnected.";
        await PersistPreferencesAsync();
    }

    private Task PersistPreferencesAsync() => CloudStorageStore.SavePreferencesAsync(_preferences);

    private async Task EnsureProviderCardOpenAsync(string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId) || IsCardOpen(providerId))
        {
            return;
        }

        await ToggleCard.InvokeAsync(providerId);
    }

    private async Task PersistProviderAsync(string providerId)
    {
        await PersistPreferencesAsync();

        switch (providerId)
        {
            case CloudStorageProviderIds.Dropbox:
                await CloudStorageStore.SaveDropboxCredentialsAsync(_dropboxCredentials);
                break;
            case CloudStorageProviderIds.OneDrive:
                await CloudStorageStore.SaveOneDriveCredentialsAsync(_oneDriveCredentials);
                break;
            case CloudStorageProviderIds.GoogleDrive:
                await CloudStorageStore.SaveGoogleDriveCredentialsAsync(_googleDriveCredentials);
                break;
            case CloudStorageProviderIds.GoogleCloudStorage:
                await CloudStorageStore.SaveGoogleCloudStorageCredentialsAsync(_googleCloudStorageCredentials);
                break;
            case CloudStorageProviderIds.CloudKit:
                await CloudStorageStore.SaveCloudKitCredentialsAsync(_cloudKitCredentials);
                break;
            default:
                throw new InvalidOperationException($"Unknown cloud storage provider '{providerId}'.");
        }
    }

    private void ResetProvider(string providerId)
    {
        switch (providerId)
        {
            case CloudStorageProviderIds.Dropbox:
                _preferences.Dropbox = new DropboxCloudStorageProfile();
                _dropboxCredentials = new DropboxCloudStorageCredentials();
                break;
            case CloudStorageProviderIds.OneDrive:
                _preferences.OneDrive = new OneDriveCloudStorageProfile();
                _oneDriveCredentials = new OneDriveCloudStorageCredentials();
                break;
            case CloudStorageProviderIds.GoogleDrive:
                _preferences.GoogleDrive = new GoogleDriveCloudStorageProfile();
                _googleDriveCredentials = new GoogleDriveCloudStorageCredentials();
                break;
            case CloudStorageProviderIds.GoogleCloudStorage:
                _preferences.GoogleCloudStorage = new GoogleCloudStorageProfile();
                _googleCloudStorageCredentials = new GoogleCloudStorageCredentials();
                break;
            case CloudStorageProviderIds.CloudKit:
                _preferences.CloudKit = new CloudKitStorageProfile();
                _cloudKitCredentials = new CloudKitStorageCredentials();
                break;
            default:
                throw new InvalidOperationException($"Unknown cloud storage provider '{providerId}'.");
        }
    }

    private CloudStorageConnectionState GetConnection(string providerId) => providerId switch
    {
        CloudStorageProviderIds.CloudKit => _preferences.CloudKit.Connection,
        CloudStorageProviderIds.Dropbox => _preferences.Dropbox.Connection,
        CloudStorageProviderIds.GoogleCloudStorage => _preferences.GoogleCloudStorage.Connection,
        CloudStorageProviderIds.GoogleDrive => _preferences.GoogleDrive.Connection,
        CloudStorageProviderIds.OneDrive => _preferences.OneDrive.Connection,
        _ => throw new InvalidOperationException($"Unknown cloud storage provider '{providerId}'.")
    };

    private static string BuildToggleCssClass(bool isOn) =>
        isOn ? $"{SetToggleCssClass} {OnCssClass}" : SetToggleCssClass;

    private static string GetStatusClass(CloudStorageConnectionState connection) =>
        connection.IsConnected ? ConnectedStatusClass : string.Empty;

    private static string GetStatusLabel(CloudStorageConnectionState connection) =>
        connection.IsConnected ? ConnectedStatusLabel : DisconnectedStatusLabel;

    private static string GetSubtitle(CloudStorageConnectionState connection) =>
        string.IsNullOrWhiteSpace(connection.AccountLabel) ? DisconnectedSubtitle : connection.AccountLabel;

    private RenderFragment ProviderActions(string providerId) => builder =>
    {
        var isConnected = GetConnection(providerId).IsConnected;
        var message = _providerMessages.GetValueOrDefault(providerId) ?? GetConnection(providerId).LastError;

        builder.AddMarkupContent(0, $"<div class=\"set-path-field\" style=\"margin-top:12px\">");
        BuildActionButton(builder, 1, "set-btn-golden", UiTestIds.Settings.CloudProviderConnect(providerId), "Save & Test", () => SaveAndValidateAsync(providerId));
        BuildActionButton(builder, 2, "set-btn-outline set-btn-sm", UiTestIds.Settings.CloudProviderExport(providerId), "Export", () => ExportAsync(providerId), !isConnected);
        BuildActionButton(builder, 3, "set-btn-outline set-btn-sm", UiTestIds.Settings.CloudProviderImport(providerId), "Import", () => ImportAsync(providerId), !isConnected);
        BuildActionButton(builder, 4, "set-btn-outline set-btn-sm set-danger-action", UiTestIds.Settings.CloudProviderDisconnect(providerId), "Disconnect", () => DisconnectAsync(providerId), !isConnected);
        builder.AddMarkupContent(5, "</div>");

        if (!string.IsNullOrWhiteSpace(message))
        {
            builder.OpenElement(6, "p");
            builder.AddAttribute(7, "class", "set-card-copy");
            builder.AddAttribute(8, "data-testid", UiTestIds.Settings.CloudProviderMessage(providerId));
            builder.AddContent(9, message);
            builder.CloseElement();
        }
    };

    private void BuildActionButton(RenderTreeBuilder builder, int sequence, string cssClass, string testId, string label, Func<Task> callback, bool disabled = false)
    {
        builder.OpenElement(sequence, "button");
        builder.AddAttribute(sequence + 1, "type", "button");
        builder.AddAttribute(sequence + 2, "class", cssClass);
        builder.AddAttribute(sequence + 3, "data-testid", testId);
        builder.AddAttribute(sequence + 4, "disabled", disabled);
        builder.AddAttribute(sequence + 5, "onclick", EventCallback.Factory.Create(this, callback));
        builder.AddContent(sequence + 6, label);
        builder.CloseElement();
    }
}
