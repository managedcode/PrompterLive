namespace PrompterOne.Shared.Storage;

public static class PrompterStorageDefaults
{
    public const int BrowserChunkBatchSize = 4;
    public const int BrowserChunkSizeBytes = 4 * 1024 * 1024;
    public const string BrowserContainerDisplayPrefix = LocalBrowserContainerName + ":";
    public const string LocalBrowserContainerName = "prompterone-local";
    public const string LocalBrowserDatabaseName = "prompterone-storage";

    public const string ExportDirectoryPath = "/exports";
    public const string LibraryRootPath = "/library";
    public const string FolderDirectoryPath = LibraryRootPath + "/folders";
    public const string RecordingsDirectoryPath = "/recordings";
    public const string ScriptDirectoryPath = LibraryRootPath + "/scripts";
    public const string SettingsRootPath = "/settings";
    public const string SettingsSnapshotFilePath = SettingsRootPath + "/browser-settings.json";
}
