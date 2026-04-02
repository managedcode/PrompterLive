using ManagedCode.Storage.VirtualFileSystem.Core;
using PrompterOne.Core.Abstractions;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Settings.Models;
using PrompterOne.Shared.Storage;

namespace PrompterOne.Shared.Settings.Services;

public sealed class BrowserFileStorageStore(
    IUserSettingsStore settingsStore,
    IScriptRepository scriptRepository,
    ILibraryFolderRepository libraryFolderRepository,
    IVirtualFileSystem? virtualFileSystem = null)
{
    private const string BrowserJsonLibraryScopeLabel = "Browser JSON library store";
    private const string EmptyUsageLabel = "No files yet";
    private const string RecordingsScopeLabel = "ManagedCode browser container";
    private const string ScriptsStorageKeyLabel = $"{BrowserStorageKeys.DocumentLibrary} / {BrowserStorageKeys.FolderLibrary}";
    private const string VfsScopeLabel = "ManagedCode.Storage browser VFS";

    private readonly ILibraryFolderRepository _libraryFolderRepository = libraryFolderRepository;
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
                ScopeLabel: BrowserJsonLibraryScopeLabel,
                LocationLabel: ScriptsStorageKeyLabel,
                DetailLabel: "Authoritative day-to-day script and folder persistence stays in browser storage, not on a desktop filesystem path."),
            Recordings: new FileStorageCardState(
                Subtitle: BuildVfsSubtitle(PrompterStorageDefaults.RecordingsDirectoryPath, recordingsUsage),
                ScopeLabel: RecordingsScopeLabel,
                LocationLabel: BuildDisplayPath(PrompterStorageDefaults.RecordingsDirectoryPath),
                DetailLabel: "PrompterOne provisions this browser-local container path for recording artifacts."),
            Exports: new FileStorageCardState(
                Subtitle: BuildVfsSubtitle(PrompterStorageDefaults.ExportDirectoryPath, exportsUsage),
                ScopeLabel: VfsScopeLabel,
                LocationLabel: BuildDisplayPath(PrompterStorageDefaults.ExportDirectoryPath),
                DetailLabel: "Exports are written to the browser-local container instead of a fake desktop Downloads folder."));
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

    private static string BuildScriptsSubtitle(int scriptCount, int folderCount) =>
        string.Concat(
            Pluralize(scriptCount, "script"),
            " · ",
            Pluralize(folderCount, "folder"));

    private static string BuildVfsSubtitle(string path, DirectoryUsage usage) =>
        string.Concat(
            BuildDisplayPath(path),
            " · ",
            usage.FileCount == 0 ? EmptyUsageLabel : usage.ToDisplayString());

    private static string Pluralize(int count, string noun) =>
        count == 1 ? $"1 {noun}" : $"{count} {noun}s";

    private readonly record struct DirectoryUsage(int FileCount, long TotalSizeBytes)
    {
        public static DirectoryUsage Empty { get; } = new(0, 0);

        public string ToDisplayString() =>
            string.Concat(
                Pluralize(FileCount, "file"),
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
}
