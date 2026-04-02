namespace PrompterOne.Shared.Settings.Models;

public sealed record BrowserFileStorageSettings(
    string ExportFormat,
    string RecordingsStorageLimit,
    bool FileAutoSaveEnabled,
    bool FileBackupCopiesEnabled)
{
    public const string StorageKey = "prompterone.file-storage";

    public static BrowserFileStorageSettings Default { get; } = new(
        ExportFormat: "TPS (Native)",
        RecordingsStorageLimit: "No limit",
        FileAutoSaveEnabled: true,
        FileBackupCopiesEnabled: true);
}

public sealed record FileStorageCardState(
    string Subtitle,
    string ScopeLabel,
    string LocationLabel,
    string DetailLabel);

public sealed record BrowserFileStorageViewState(
    FileStorageCardState Scripts,
    FileStorageCardState Recordings,
    FileStorageCardState Exports);
