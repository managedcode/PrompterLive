using Microsoft.AspNetCore.Components;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Settings.Components;
using PrompterOne.Shared.Settings.Models;
using PrompterOne.Shared.Settings.Services;
using PrompterOne.Shared.Storage;

namespace PrompterOne.Shared.Components.Settings;

public partial class SettingsFilesSection : ComponentBase
{
    private const string ExportsCardId = "files-exports";
    private const string EmptyValue = "";
    private const string OnCssClass = "on";
    private const string RecordingsCardId = "files-recordings";
    private const string ScriptsCardId = "files-scripts";
    private const string SetToggleCssClass = "set-toggle";

    private BrowserFileStorageSettings _settings = BrowserFileStorageSettings.Default;
    private BrowserFileStorageViewState _viewState = BuildEmptyViewState();

    [Inject] private BrowserFileStorageStore FileStorageStore { get; set; } = null!;

    [Parameter] public string DisplayStyle { get; set; } = string.Empty;

    [Parameter] public Func<string, bool> IsCardOpen { get; set; } = static _ => false;

    [Parameter] public EventCallback<string> ToggleCard { get; set; }

    private IReadOnlyList<SettingsSelectOption> ExportFormatOptions =>
    [
        new(Text(UiTextKey.SettingsFilesExportFormatTpsNative), "TPS (Native)"),
        new(Text(UiTextKey.SettingsFilesExportFormatMarkdown), "Markdown"),
        new(Text(UiTextKey.SettingsFilesExportFormatPlainText), "Plain Text"),
        new(Text(UiTextKey.SettingsFilesExportFormatPdf), "PDF"),
    ];

    private string ScriptsAutoSaveLabel => Text(UiTextKey.SettingsFilesScriptsAutoSave);

    private string ScriptsHistoryLabel => Text(UiTextKey.SettingsFilesScriptsHistory);

    private IReadOnlyList<SettingsSelectOption> StorageLimitOptions =>
    [
        new(Text(UiTextKey.SettingsFilesStorageLimitNoLimit), "No limit"),
        new(Text(UiTextKey.SettingsFilesStorageLimit10Gb), "10 GB"),
        new(Text(UiTextKey.SettingsFilesStorageLimit50Gb), "50 GB"),
        new(Text(UiTextKey.SettingsFilesStorageLimit100Gb), "100 GB"),
    ];

    protected override void OnInitialized()
    {
        _viewState = BuildInitialViewState();
    }

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

    private BrowserFileStorageViewState BuildInitialViewState() =>
        new(
            Scripts: new FileStorageCardState(
                Subtitle: string.Concat("0 ", Text(UiTextKey.SettingsFilesPluralScript), " · 0 ", Text(UiTextKey.SettingsFilesPluralFolder)),
                ScopeLabel: Text(UiTextKey.SettingsFilesScopeBrowserJsonLibrary),
                LocationLabel: string.Concat(BrowserStorageKeys.DocumentLibrary, " / ", BrowserStorageKeys.FolderLibrary),
                DetailLabel: Text(UiTextKey.SettingsFilesScriptsDetail)),
            Recordings: new FileStorageCardState(
                Subtitle: string.Concat(PrompterStorageDefaults.BrowserContainerDisplayPrefix, PrompterStorageDefaults.RecordingsDirectoryPath, " · ", Text(UiTextKey.SettingsFilesEmptyUsage)),
                ScopeLabel: Text(UiTextKey.SettingsFilesScopeManagedCodeBrowserContainer),
                LocationLabel: string.Concat(PrompterStorageDefaults.BrowserContainerDisplayPrefix, PrompterStorageDefaults.RecordingsDirectoryPath),
                DetailLabel: Text(UiTextKey.SettingsFilesRecordingsDetail)),
            Exports: new FileStorageCardState(
                Subtitle: string.Concat(PrompterStorageDefaults.BrowserContainerDisplayPrefix, PrompterStorageDefaults.ExportDirectoryPath, " · ", Text(UiTextKey.SettingsFilesEmptyUsage)),
                ScopeLabel: Text(UiTextKey.SettingsFilesScopeManagedCodeBrowserVfs),
                LocationLabel: string.Concat(PrompterStorageDefaults.BrowserContainerDisplayPrefix, PrompterStorageDefaults.ExportDirectoryPath),
                DetailLabel: Text(UiTextKey.SettingsFilesExportsDetail)));

    private static BrowserFileStorageViewState BuildEmptyViewState() =>
        new(
            Scripts: new FileStorageCardState(EmptyValue, EmptyValue, EmptyValue, EmptyValue),
            Recordings: new FileStorageCardState(EmptyValue, EmptyValue, EmptyValue, EmptyValue),
            Exports: new FileStorageCardState(EmptyValue, EmptyValue, EmptyValue, EmptyValue));

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
