using System.Text.Json;
using ManagedCode.Communication;
using ManagedCode.Storage.Core.Models;
using PrompterOne.Core.Abstractions;
using PrompterOne.Core.Models.Documents;
using PrompterOne.Core.Models.Library;
using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Settings.Models;
using PrompterOne.Shared.Settings.Services;

namespace PrompterOne.Shared.Storage.Cloud;

public sealed class CloudStorageTransferService(
    IUserSettingsStore settingsStore,
    BrowserThemeService themeService,
    CloudStorageProviderFactory providerFactory,
    ILibraryFolderRepository libraryFolderRepository,
    IScriptRepository scriptRepository,
    IScriptSessionService scriptSessionService,
    IMediaSceneService mediaSceneService,
    StudioSettingsStore studioSettingsStore,
    AiProviderSettingsStore aiProviderSettingsStore,
    BrowserFileStorageStore browserFileStorageStore)
{
    private readonly AiProviderSettingsStore _aiProviderSettingsStore = aiProviderSettingsStore;
    private readonly BrowserFileStorageStore _browserFileStorageStore = browserFileStorageStore;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly ILibraryFolderRepository _libraryFolderRepository = libraryFolderRepository;
    private readonly IMediaSceneService _mediaSceneService = mediaSceneService;
    private readonly CloudStorageProviderFactory _providerFactory = providerFactory;
    private readonly IScriptRepository _scriptRepository = scriptRepository;
    private readonly IScriptSessionService _scriptSessionService = scriptSessionService;
    private readonly IUserSettingsStore _settingsStore = settingsStore;
    private readonly StudioSettingsStore _studioSettingsStore = studioSettingsStore;
    private readonly BrowserThemeService _themeService = themeService;

    public async Task<CloudStorageOperationResult> ExportAsync(
        CloudStoragePreferences preferences,
        string providerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var storage = await _providerFactory.CreateAsync(preferences, providerId, cancellationToken);
            var createResult = await storage.CreateContainerAsync(cancellationToken);
            if (!createResult.IsSuccess)
            {
                return CloudStorageOperationResult.Failure(ToMessage(createResult.Problem, "Unable to initialize the selected provider."));
            }

            var backup = await BuildBackupAsync(cancellationToken);
            var payload = JsonSerializer.Serialize(backup, JsonOptions);
            var uploadResult = await storage.UploadAsync(
                payload,
                CreateUploadOptions,
                cancellationToken);

            return uploadResult.IsSuccess
                ? CloudStorageOperationResult.Success($"Exported {backup.Scripts.Count} scripts and settings.")
                : CloudStorageOperationResult.Failure(ToMessage(uploadResult.Problem, "Unable to upload the backup snapshot."));
        }
        catch (Exception exception)
        {
            return CloudStorageOperationResult.Failure(exception.Message);
        }
    }

    public async Task<CloudStorageOperationResult> ImportAsync(
        CloudStoragePreferences preferences,
        string providerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var storage = await _providerFactory.CreateAsync(preferences, providerId, cancellationToken);
            var createResult = await storage.CreateContainerAsync(cancellationToken);
            if (!createResult.IsSuccess)
            {
                return CloudStorageOperationResult.Failure(ToMessage(createResult.Problem, "Unable to initialize the selected provider."));
            }

            var streamResult = await storage.GetStreamAsync(CloudStorageStoreKeys.BackupPath, cancellationToken);
            if (!streamResult.IsSuccess || streamResult.Value is null)
            {
                return CloudStorageOperationResult.Failure(ToMessage(streamResult.Problem, "No backup snapshot was found for this provider."));
            }

            using var stream = streamResult.Value;
            using var reader = new StreamReader(stream);
            var payload = await reader.ReadToEndAsync(cancellationToken);
            var backup = JsonSerializer.Deserialize<CloudStorageBackupEnvelope>(payload, JsonOptions);

            if (backup is null)
            {
                return CloudStorageOperationResult.Failure("The backup snapshot is empty or invalid.");
            }

            await RestoreBackupAsync(backup, cancellationToken);
            return CloudStorageOperationResult.Success($"Imported {backup.Scripts.Count} scripts and settings.");
        }
        catch (Exception exception)
        {
            return CloudStorageOperationResult.Failure(exception.Message);
        }
    }

    private async Task<CloudStorageBackupEnvelope> BuildBackupAsync(CancellationToken cancellationToken)
    {
        var documents = await LoadScriptsAsync(cancellationToken);
        var folders = await _libraryFolderRepository.ListAsync(cancellationToken);
        var settingsBundle = await BuildSettingsBundleAsync(cancellationToken);

        return new CloudStorageBackupEnvelope
        {
            ExportedAt = DateTimeOffset.UtcNow,
            Folders = folders,
            Scripts = documents,
            Settings = settingsBundle
        };
    }

    private async Task<IReadOnlyList<StoredScriptDocument>> LoadScriptsAsync(CancellationToken cancellationToken)
    {
        var summaries = await _scriptRepository.ListAsync(cancellationToken);
        var documents = new List<StoredScriptDocument>(summaries.Count);

        foreach (var summary in summaries)
        {
            var document = await _scriptRepository.GetAsync(summary.Id, cancellationToken);
            if (document is not null)
            {
                documents.Add(document);
            }
        }

        return documents;
    }

    private async Task<CloudStorageSettingsBundle> BuildSettingsBundleAsync(CancellationToken cancellationToken)
    {
        return new CloudStorageSettingsBundle
        {
            AiProviderSettings = await _aiProviderSettingsStore.LoadAsync(cancellationToken),
            FileStorageSettings = await _browserFileStorageStore.LoadSettingsAsync(cancellationToken),
            LearnSettings = await _settingsStore.LoadAsync<LearnSettings>(BrowserAppSettingsKeys.LearnSettings, cancellationToken) ?? new LearnSettings(),
            ReaderSettings = await _settingsStore.LoadAsync<ReaderSettings>(BrowserAppSettingsKeys.ReaderSettings, cancellationToken) ?? new ReaderSettings(),
            SceneState = await _settingsStore.LoadAsync<MediaSceneState>(BrowserAppSettingsKeys.SceneSettings, cancellationToken) ?? MediaSceneState.Empty,
            SettingsPagePreferences = await _settingsStore.LoadAsync<SettingsPagePreferences>(SettingsPagePreferences.StorageKey, cancellationToken) ?? SettingsPagePreferences.Default,
            StudioSettings = await _studioSettingsStore.LoadAsync(cancellationToken)
        };
    }

    private async Task RestoreBackupAsync(CloudStorageBackupEnvelope backup, CancellationToken cancellationToken)
    {
        var folderMap = await RestoreFoldersAsync(backup.Folders, cancellationToken);
        await RestoreScriptsAsync(backup.Scripts, folderMap, cancellationToken);
        await RestoreSettingsAsync(backup.Settings, cancellationToken);
    }

    private async Task<Dictionary<string, string>> RestoreFoldersAsync(
        IReadOnlyList<StoredLibraryFolder> importedFolders,
        CancellationToken cancellationToken)
    {
        var existingFolders = (await _libraryFolderRepository.ListAsync(cancellationToken)).ToList();
        var importedToLocal = new Dictionary<string, string>(StringComparer.Ordinal);
        var pending = importedFolders.OrderBy(folder => folder.DisplayOrder).ToList();

        while (pending.Count > 0)
        {
            var processedInPass = 0;

            for (var index = pending.Count - 1; index >= 0; index--)
            {
                var importedFolder = pending[index];
                var parentLocalId = ResolveParentLocalId(importedFolder.ParentId, importedToLocal);
                if (importedFolder.ParentId is not null && parentLocalId is null)
                {
                    continue;
                }

                var existingFolder = existingFolders.FirstOrDefault(folder =>
                    string.Equals(folder.Name, importedFolder.Name, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(folder.ParentId, parentLocalId, StringComparison.Ordinal));

                if (existingFolder is null)
                {
                    existingFolder = await _libraryFolderRepository.CreateAsync(
                        importedFolder.Name,
                        parentLocalId,
                        cancellationToken);

                    existingFolders.Add(existingFolder);
                }

                importedToLocal[importedFolder.Id] = existingFolder.Id;
                pending.RemoveAt(index);
                processedInPass++;
            }

            if (processedInPass == 0)
            {
                break;
            }
        }

        return importedToLocal;
    }

    private async Task RestoreScriptsAsync(
        IReadOnlyList<StoredScriptDocument> importedScripts,
        IReadOnlyDictionary<string, string> folderMap,
        CancellationToken cancellationToken)
    {
        foreach (var importedScript in importedScripts)
        {
            var folderId = ResolveParentLocalId(importedScript.FolderId, folderMap);
            _ = await _scriptRepository.SaveAsync(
                importedScript.Title,
                importedScript.Text,
                importedScript.DocumentName,
                importedScript.Id,
                folderId,
                cancellationToken);
        }
    }

    private async Task RestoreSettingsAsync(
        CloudStorageSettingsBundle settings,
        CancellationToken cancellationToken)
    {
        var bundle = settings ?? new CloudStorageSettingsBundle();

        await _aiProviderSettingsStore.SaveAsync(bundle.AiProviderSettings, cancellationToken);
        await _browserFileStorageStore.SaveSettingsAsync(bundle.FileStorageSettings, cancellationToken);
        await _settingsStore.SaveAsync(SettingsPagePreferences.StorageKey, bundle.SettingsPagePreferences, cancellationToken);
        await _settingsStore.SaveAsync(BrowserAppSettingsKeys.ReaderSettings, bundle.ReaderSettings, cancellationToken);
        await _settingsStore.SaveAsync(BrowserAppSettingsKeys.LearnSettings, bundle.LearnSettings, cancellationToken);
        await _settingsStore.SaveAsync(BrowserAppSettingsKeys.SceneSettings, bundle.SceneState, cancellationToken);
        await _studioSettingsStore.SaveAsync(bundle.StudioSettings, cancellationToken);

        await _scriptSessionService.UpdateReaderSettingsAsync(bundle.ReaderSettings);
        await _scriptSessionService.UpdateLearnSettingsAsync(bundle.LearnSettings);
        _mediaSceneService.ApplyState(bundle.SceneState);
        await _themeService.ApplyAsync(bundle.SettingsPagePreferences, cancellationToken);
    }

    private static string? ResolveParentLocalId(
        string? importedParentId,
        IReadOnlyDictionary<string, string> folderMap) =>
        !string.IsNullOrWhiteSpace(importedParentId) && folderMap.TryGetValue(importedParentId, out var localId)
            ? localId
            : null;

    private static void CreateUploadOptions(UploadOptions options)
    {
        options.Directory = CloudStorageStoreKeys.BackupDirectory;
        options.FileName = CloudStorageStoreKeys.BackupFileName;
        options.MimeType = CloudStorageStoreKeys.JsonMimeType;
    }

    private static string ToMessage(Problem? problem, string fallbackMessage) =>
        problem?.Detail ?? problem?.Title ?? fallbackMessage;
}
