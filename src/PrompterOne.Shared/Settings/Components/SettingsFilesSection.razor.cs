using Microsoft.AspNetCore.Components;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Settings.Components;
using PrompterOne.Shared.Settings.Models;
using PrompterOne.Shared.Settings.Services;
using PrompterOne.Shared.Storage;

namespace PrompterOne.Shared.Components.Settings;

public partial class SettingsFilesSection : ComponentBase
{
    private static readonly IReadOnlyList<SettingsSelectOption> ExportFormatOptions =
    [
        new("TPS (Native)", "TPS (Native)"),
        new("Markdown", "Markdown"),
        new("Plain Text", "Plain Text"),
        new("PDF", "PDF"),
    ];

    private static readonly IReadOnlyList<SettingsSelectOption> StorageLimitOptions =
    [
        new("No limit", "No limit"),
        new("10 GB", "10 GB"),
        new("50 GB", "50 GB"),
        new("100 GB", "100 GB"),
    ];

    private const string ExportsCardId = "files-exports";
    private const string OnCssClass = "on";
    private const string RecordingsCardId = "files-recordings";
    private const string ScriptsCardId = "files-scripts";
    private const string SetToggleCssClass = "set-toggle";

    private BrowserFileStorageSettings _settings = BrowserFileStorageSettings.Default;
    private BrowserFileStorageViewState _viewState = new(
        Scripts: new FileStorageCardState(
            Subtitle: "0 scripts · 0 folders",
            ScopeLabel: "Browser JSON library store",
            LocationLabel: $"{BrowserStorageKeys.DocumentLibrary} / {BrowserStorageKeys.FolderLibrary}",
            DetailLabel: "Authoritative day-to-day script and folder persistence stays in browser storage, not on a desktop filesystem path."),
        Recordings: new FileStorageCardState(
            Subtitle: $"{PrompterStorageDefaults.BrowserContainerDisplayPrefix}{PrompterStorageDefaults.RecordingsDirectoryPath} · No files yet",
            ScopeLabel: "ManagedCode browser container",
            LocationLabel: $"{PrompterStorageDefaults.BrowserContainerDisplayPrefix}{PrompterStorageDefaults.RecordingsDirectoryPath}",
            DetailLabel: "PrompterOne provisions this browser-local container path for recording artifacts."),
        Exports: new FileStorageCardState(
            Subtitle: $"{PrompterStorageDefaults.BrowserContainerDisplayPrefix}{PrompterStorageDefaults.ExportDirectoryPath} · No files yet",
            ScopeLabel: "ManagedCode.Storage browser VFS",
            LocationLabel: $"{PrompterStorageDefaults.BrowserContainerDisplayPrefix}{PrompterStorageDefaults.ExportDirectoryPath}",
            DetailLabel: "Exports are written to the browser-local container instead of a fake desktop Downloads folder."));

    [Inject] private BrowserFileStorageStore FileStorageStore { get; set; } = null!;

    [Parameter] public string DisplayStyle { get; set; } = string.Empty;

    [Parameter] public Func<string, bool> IsCardOpen { get; set; } = static _ => false;

    [Parameter] public EventCallback<string> ToggleCard { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        _settings = await FileStorageStore.LoadSettingsAsync();
        _viewState = await FileStorageStore.LoadViewStateAsync();
        await InvokeAsync(StateHasChanged);
    }

    private static string BuildToggleCssClass(bool isOn) =>
        isOn ? $"{SetToggleCssClass} {OnCssClass}" : SetToggleCssClass;

    private Task OnExportFormatChanged(ChangeEventArgs args) =>
        UpdateExportFormatAsync(args.Value?.ToString() ?? string.Empty);

    private Task OnStorageLimitChanged(ChangeEventArgs args) =>
        UpdateRecordingsStorageLimitAsync(args.Value?.ToString() ?? string.Empty);

    private async Task ToggleAutoSaveAsync()
    {
        _settings = _settings with { FileAutoSaveEnabled = !_settings.FileAutoSaveEnabled };
        await FileStorageStore.SaveSettingsAsync(_settings);
    }

    private async Task ToggleBackupCopiesAsync()
    {
        _settings = _settings with { FileBackupCopiesEnabled = !_settings.FileBackupCopiesEnabled };
        await FileStorageStore.SaveSettingsAsync(_settings);
    }

    private async Task UpdateExportFormatAsync(string value)
    {
        _settings = _settings with { ExportFormat = value };
        await FileStorageStore.SaveSettingsAsync(_settings);
    }

    private async Task UpdateRecordingsStorageLimitAsync(string value)
    {
        _settings = _settings with { RecordingsStorageLimit = value };
        await FileStorageStore.SaveSettingsAsync(_settings);
    }
}
