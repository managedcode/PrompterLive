using ManagedCode.Storage.VirtualFileSystem.Core;
using Microsoft.Extensions.Localization;
using PrompterOne.Core.Abstractions;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Settings.Models;
using PrompterOne.Shared.Storage;

namespace PrompterOne.Shared.Settings.Services;

public sealed class BrowserFileStorageStore(
    IUserSettingsStore settingsStore,
    IScriptRepository scriptRepository,
    ILibraryFolderRepository libraryFolderRepository,
    IStringLocalizer<SharedResource> localizer,
    IVirtualFileSystem? virtualFileSystem = null)
{
    private const string ScriptsStorageKeyLabel = $"{BrowserStorageKeys.DocumentLibrary} / {BrowserStorageKeys.FolderLibrary}";

    private readonly ILibraryFolderRepository _libraryFolderRepository = libraryFolderRepository;
    private readonly IStringLocalizer<SharedResource> _localizer = localizer;
    private readonly IScriptRepository _scriptRepository = scriptRepository;
    private readonly IUserSettingsStore _settingsStore = settingsStore;
    private readonly IVirtualFileSystem? _virtualFileSystem = virtualFileSystem;

    public async Task<BrowserFileStorageSettings> LoadSettingsAsync(CancellationToken cancellationToken = default)
    {
        return await _settingsStore.LoadAsync<BrowserFileStorageSettings>(BrowserFileStorageSettings.StorageKey, cancellationToken)
            ?? BrowserFileStorageSettings.Default;
    }

    public async Task<BrowserFileStorageViewState> LoadViewStateAsync(CancellationToken cancellationToken = default)
    {
        var scripts = await _scriptRepository.ListAsync(cancellationToken);
        var folders = await _libraryFolderRepository.ListAsync(cancellationToken);
        var recordingsUsage = await LoadDirectoryUsageAsync(PrompterStorageDefaults.RecordingsDirectoryPath, cancellationToken);
        var exportsUsage = await LoadDirectoryUsageAsync(PrompterStorageDefaults.ExportDirectoryPath, cancellationToken);

        return new BrowserFileStorageViewState(
            Scripts: new FileStorageCardState(
                Subtitle: BuildScriptsSubtitle(scripts.Count, folders.Count),
                ScopeLabel: Text(UiTextKey.SettingsFilesScopeBrowserJsonLibrary),
                LocationLabel: ScriptsStorageKeyLabel,
                DetailLabel: Text(UiTextKey.SettingsFilesScriptsDetail)),
            Recordings: new FileStorageCardState(
                Subtitle: BuildVfsSubtitle(PrompterStorageDefaults.RecordingsDirectoryPath, recordingsUsage),
                ScopeLabel: Text(UiTextKey.SettingsFilesScopeManagedCodeBrowserContainer),
                LocationLabel: BuildDisplayPath(PrompterStorageDefaults.RecordingsDirectoryPath),
                DetailLabel: Text(UiTextKey.SettingsFilesRecordingsDetail)),
            Exports: new FileStorageCardState(
                Subtitle: BuildVfsSubtitle(PrompterStorageDefaults.ExportDirectoryPath, exportsUsage),
                ScopeLabel: Text(UiTextKey.SettingsFilesScopeManagedCodeBrowserVfs),
                LocationLabel: BuildDisplayPath(PrompterStorageDefaults.ExportDirectoryPath),
                DetailLabel: Text(UiTextKey.SettingsFilesExportsDetail)));
    }

    public Task SaveSettingsAsync(BrowserFileStorageSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return _settingsStore.SaveAsync(BrowserFileStorageSettings.StorageKey, settings, cancellationToken);
    }

    private async Task<DirectoryUsage> LoadDirectoryUsageAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            if (_virtualFileSystem is null)
            {
                return DirectoryUsage.Empty;
            }

            await VfsDirectoryProvisioner.EnsureDirectoryAsync(_virtualFileSystem, path, cancellationToken);
            var directory = await _virtualFileSystem.GetDirectoryAsync(path, cancellationToken);
            var stats = await directory.GetStatsAsync(true, cancellationToken);
            return new DirectoryUsage(stats.FileCount, stats.TotalSize);
        }
        catch
        {
            return DirectoryUsage.Empty;
        }
    }

    private static string BuildDisplayPath(string path) =>
        string.Concat(PrompterStorageDefaults.BrowserContainerDisplayPrefix, path);

    private string BuildScriptsSubtitle(int scriptCount, int folderCount) =>
        string.Concat(
            Pluralize(scriptCount, UiTextKey.SettingsFilesSingularScript, UiTextKey.SettingsFilesPluralScript),
            " · ",
            Pluralize(folderCount, UiTextKey.SettingsFilesSingularFolder, UiTextKey.SettingsFilesPluralFolder));

    private string BuildVfsSubtitle(string path, DirectoryUsage usage) =>
        string.Concat(
            BuildDisplayPath(path),
            " · ",
            usage.FileCount == 0 ? Text(UiTextKey.SettingsFilesEmptyUsage) : usage.ToDisplayString(this));

    private string Pluralize(int count, UiTextKey singularKey, UiTextKey pluralKey) =>
        count == 1
            ? $"1 {Text(singularKey)}"
            : $"{count} {Text(pluralKey)}";

    private readonly record struct DirectoryUsage(int FileCount, long TotalSizeBytes)
    {
        public static DirectoryUsage Empty { get; } = new(0, 0);

        public string ToDisplayString(BrowserFileStorageStore owner) =>
            string.Concat(
                owner.Pluralize(FileCount, UiTextKey.SettingsFilesSingularFile, UiTextKey.SettingsFilesPluralFile),
                " · ",
                FormatSize(TotalSizeBytes));

        private static string FormatSize(long bytes)
        {
            const double Kilobyte = 1024d;
            const double Megabyte = Kilobyte * 1024d;

            if (bytes >= Megabyte)
            {
                return $"{bytes / Megabyte:0.0} MB";
            }

            if (bytes >= Kilobyte)
            {
                return $"{bytes / Kilobyte:0.0} KB";
            }

            return $"{bytes} B";
        }
    }

    private string Text(UiTextKey key) => _localizer[key.ToString()];
}
