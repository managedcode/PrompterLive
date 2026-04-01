using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using PrompterOne.App.Tests;
using PrompterOne.Core.Abstractions;
using PrompterOne.Core.Models.Documents;
using PrompterOne.Core.Models.Library;
using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Services;
using PrompterOne.Core.Services.Editor;
using PrompterOne.Core.Services.Media;
using PrompterOne.Core.Services.Preview;
using PrompterOne.Core.Services.Rsvp;
using PrompterOne.Core.Services.Streaming;
using PrompterOne.Core.Services.Workspace;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Services.Diagnostics;
using PrompterOne.Shared.Services.Editor;
using PrompterOne.Shared.Settings.Models;
using PrompterOne.Shared.Settings.Services;
using PrompterOne.Shared.Storage.Cloud;

namespace PrompterOne.Shared.Tests;

internal static class TestHarnessFactory
{
    public static AppHarness Create(
        BunitContext context,
        IReadOnlyList<MediaDeviceInfo>? devices = null,
        Action<ILoggingBuilder>? configureLogging = null,
        TimeSpan? jsInvocationDelay = null,
        bool seedLibraryData = true)
    {
        var jsRuntime = new TestJsRuntime(jsInvocationDelay);
        var repository = new InMemoryScriptRepository();
        var folderRepository = new InMemoryLibraryFolderRepository();
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.SetMinimumLevel(LogLevel.Trace);
            configureLogging?.Invoke(builder);
        });
        var parser = new TpsParser();
        var compiler = new ScriptCompiler();
        var frontMatter = new TpsFrontMatterDocumentService();
        var textEditor = new TpsTextEditor();
        var structureEditor = new TpsStructureEditor();
        var localAssistant = new EditorLocalAssistant();
        var previewService = new ScriptPreviewService(parser, compiler);
        var session = new ScriptSessionService(
            repository,
            parser,
            compiler,
            previewService,
            loggerFactory.CreateLogger<ScriptSessionService>());
        var sceneService = new MediaSceneService();
        var permissionService = new FakeMediaPermissionService();
        var deviceService = new FakeMediaDeviceService(devices ?? DefaultDevices);
        var settingsStore = new BrowserSettingsStore(
            jsRuntime,
            loggerFactory.CreateLogger<BrowserSettingsStore>());
        var shell = new AppShellService();
        var bootstrapper = new AppBootstrapper(
            session,
            repository,
            folderRepository,
            sceneService,
            settingsStore,
            loggerFactory.CreateLogger<AppBootstrapper>());

        if (seedLibraryData)
        {
            folderRepository.InitializeAsync(AppTestLibrarySeedData.CreateFolders()).GetAwaiter().GetResult();
            repository.InitializeAsync(AppTestLibrarySeedData.CreateDocuments()).GetAwaiter().GetResult();
        }

        context.Services.AddLocalization();
        context.Services.AddSingleton<IJSRuntime>(jsRuntime);
        context.Services.AddSingleton<ILoggerFactory>(loggerFactory);
        context.Services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        context.Services.AddSingleton<ILibraryFolderRepository>(folderRepository);
        context.Services.AddSingleton<IScriptRepository>(repository);
        context.Services.AddSingleton<IScriptSessionService>(session);
        context.Services.AddSingleton(parser);
        context.Services.AddSingleton(compiler);
        context.Services.AddSingleton(frontMatter);
        context.Services.AddSingleton(textEditor);
        context.Services.AddSingleton(structureEditor);
        context.Services.AddSingleton(localAssistant);
        context.Services.AddSingleton<IScriptPreviewService>(previewService);
        context.Services.AddSingleton<EditorOutlineBuilder>();
        context.Services.AddSingleton<EditorInterop>();
        context.Services.AddSingleton<IMediaSceneService>(sceneService);
        context.Services.AddSingleton<IMediaPermissionService>(permissionService);
        context.Services.AddSingleton<IMediaDeviceService>(deviceService);
        context.Services.AddSingleton<RsvpOrpCalculator>();
        context.Services.AddSingleton<RsvpTextProcessor>();
        context.Services.AddSingleton<RsvpEmotionAnalyzer>();
        context.Services.AddSingleton<RsvpPlaybackEngine>();
        context.Services.AddSingleton(settingsStore);
        context.Services.AddSingleton<IUserSettingsStore>(settingsStore);
        context.Services.AddSingleton<BrowserCloudStorageStore>();
        context.Services.AddSingleton<BrowserThemeService>();
        context.Services.AddSingleton<CloudStorageProviderFactory>();
        context.Services.AddSingleton<CloudStorageTransferService>();
        context.Services.AddSingleton<IAppVersionProvider>(
            new StaticAppVersionProvider(
                new AppVersionInfo(
                    AppTestData.About.Version,
                    AppTestData.About.BuildNumber)));
        context.Services.AddSingleton(shell);
        context.Services.AddSingleton<StudioSettingsStore>();
        context.Services.AddSingleton<CameraPreviewInterop>();
        context.Services.AddSingleton<LearnRsvpLayoutInterop>();
        context.Services.AddSingleton<MicrophoneLevelInterop>();
        context.Services.AddSingleton<TeleprompterReaderInterop>();
        context.Services.AddSingleton<GoLiveOutputInterop>();
        context.Services.AddSingleton<GoLiveOutputRuntimeService>();
        context.Services.AddSingleton(bootstrapper);
        context.Services.AddSingleton<GoLiveSessionService>();
        context.Services.AddSingleton<BrowserConnectivityService>();
        context.Services.AddSingleton<UiDiagnosticsService>();
        context.Services.AddSingleton<IStreamingOutputProvider, LiveKitOutputProvider>();
        context.Services.AddSingleton<IStreamingOutputProvider, VdoNinjaOutputProvider>();
        context.Services.AddSingleton<IStreamingOutputProvider, RtmpStreamingOutputProvider>();

        return new AppHarness(
            jsRuntime,
            repository,
            folderRepository,
            session,
            sceneService,
            permissionService,
            deviceService,
            loggerFactory);
    }

    private static IReadOnlyList<MediaDeviceInfo> DefaultDevices =>
    [
        new("cam-1", "Front camera", MediaDeviceKind.Camera, true),
        new("mic-1", "Broadcast mic", MediaDeviceKind.Microphone, true)
    ];
}

internal sealed record AppHarness(
    TestJsRuntime JsRuntime,
    InMemoryScriptRepository Repository,
    InMemoryLibraryFolderRepository FolderRepository,
    ScriptSessionService Session,
    MediaSceneService SceneService,
    FakeMediaPermissionService PermissionService,
    FakeMediaDeviceService DeviceService,
    ILoggerFactory LoggerFactory);

internal sealed record JsInvocationRecord(
    string Identifier,
    IReadOnlyList<object?> Arguments);

internal sealed class TestJsRuntime(TimeSpan? invocationDelay = null) : IJSRuntime
{
    private const string EvaluateIdentifier = "eval";
    private const string LoadSettingJsonIdentifier = "localStorage.getItem";
    private const string RemoveStorageValueIdentifier = "localStorage.removeItem";
    private const string SaveSettingJsonIdentifier = "localStorage.setItem";
    private const string NavigatorOnlineExpression = "navigator.onLine";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly TimeSpan _invocationDelay = invocationDelay ?? TimeSpan.Zero;
    public Dictionary<string, object?> SavedValues { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> SavedJsonValues { get; } = new(StringComparer.Ordinal);
    public List<string> Invocations { get; } = [];
    public List<JsInvocationRecord> InvocationRecords { get; } = [];

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        if (_invocationDelay > TimeSpan.Zero)
        {
            return new ValueTask<TValue>(InvokeDelayedAsync<TValue>(identifier, cancellationToken, args));
        }

        return ValueTask.FromResult(GetResult<TValue>(identifier, args));
    }

    private async Task<TValue> InvokeDelayedAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        await Task.Delay(_invocationDelay, cancellationToken);
        return GetResult<TValue>(identifier, args);
    }

    private TValue GetResult<TValue>(string identifier, object?[]? args)
    {
        Invocations.Add(identifier);
        InvocationRecords.Add(new JsInvocationRecord(identifier, args?.ToArray() ?? []));

        var result = identifier switch
        {
            EvaluateIdentifier => Evaluate(args),
            LoadSettingJsonIdentifier => LoadJson(args),
            SaveSettingJsonIdentifier => SaveJson(args),
            RemoveStorageValueIdentifier => Remove(args),
            _ => null
        };

        if (result is null)
        {
            return default!;
        }

        if (result is TValue typed)
        {
            return typed;
        }

        return (TValue)result;
    }

    public T GetSavedValue<T>(string key)
    {
        foreach (var storageKey in EnumerateStorageKeys(key))
        {
            if (SavedValues.TryGetValue(storageKey, out var value) && value is T typedValue)
            {
                return typedValue;
            }

            if (SavedJsonValues.TryGetValue(storageKey, out var json))
            {
                return JsonSerializer.Deserialize<T>(json, JsonOptions)
                    ?? throw new InvalidOperationException($"Saved JSON for '{storageKey}' could not be deserialized as {typeof(T).Name}.");
            }
        }

        throw new KeyNotFoundException($"No saved value was found for '{key}'.");
    }

    private object? LoadJson(object?[]? args)
    {
        var key = args?.FirstOrDefault()?.ToString() ?? string.Empty;

        foreach (var storageKey in EnumerateStorageKeys(key))
        {
            if (SavedJsonValues.TryGetValue(storageKey, out var savedJson))
            {
                return savedJson;
            }

            if (SavedValues.TryGetValue(storageKey, out var value))
            {
                return JsonSerializer.Serialize(value, JsonOptions);
            }
        }

        return null;
    }

    private object? SaveJson(object?[]? args)
    {
        var key = args?.FirstOrDefault()?.ToString() ?? string.Empty;
        var json = args?.Skip(1).FirstOrDefault()?.ToString() ?? string.Empty;
        SavedJsonValues[key] = json;
        return null;
    }

    private object? Remove(object?[]? args)
    {
        var key = args?.FirstOrDefault()?.ToString() ?? string.Empty;
        foreach (var storageKey in EnumerateStorageKeys(key))
        {
            SavedJsonValues.Remove(storageKey);
            SavedValues.Remove(storageKey);
        }

        return null;
    }

    private static IEnumerable<string> EnumerateStorageKeys(string key)
    {
        if (key.StartsWith(BrowserStorageKeys.SettingsPrefix, StringComparison.Ordinal))
        {
            yield return key;
            yield return key[BrowserStorageKeys.SettingsPrefix.Length..];
            yield break;
        }

        yield return string.Concat(BrowserStorageKeys.SettingsPrefix, key);
        yield return key;
    }

    private static object? Evaluate(object?[]? args)
    {
        var expression = args?.FirstOrDefault()?.ToString() ?? string.Empty;
        return expression switch
        {
            NavigatorOnlineExpression => true,
            _ when expression.Contains("navigator.languages", StringComparison.Ordinal) => new[] { "en" },
            _ => null
        };
    }
}

internal sealed class FakeMediaPermissionService : IMediaPermissionService
{
    public MediaPermissionsState Current { get; private set; } = new(false, false);

    public bool Requested { get; private set; }

    public Task<MediaPermissionsState> QueryAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Current);

    public Task<MediaPermissionsState> RequestAsync(CancellationToken cancellationToken = default)
    {
        Requested = true;
        Current = new MediaPermissionsState(true, true);
        return Task.FromResult(Current);
    }
}

internal sealed class FakeMediaDeviceService(IReadOnlyList<MediaDeviceInfo> devices) : IMediaDeviceService
{
    private readonly IReadOnlyList<MediaDeviceInfo> _devices = devices;

    public Task<IReadOnlyList<MediaDeviceInfo>> GetDevicesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_devices);
}

internal sealed class InMemoryScriptRepository : IScriptRepository
{
    private readonly Dictionary<string, StoredScriptDocument> _documents = new(StringComparer.Ordinal);

    public Task InitializeAsync(IEnumerable<StoredScriptDocument> seedDocuments, CancellationToken cancellationToken = default)
    {
        foreach (var document in seedDocuments)
        {
            _documents.TryAdd(document.Id, document);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<StoredScriptSummary>> ListAsync(CancellationToken cancellationToken = default)
    {
        var summaries = _documents.Values
            .OrderByDescending(document => document.UpdatedAt)
            .Select(document => new StoredScriptSummary(
                document.Id,
                document.Title,
                document.DocumentName,
                document.UpdatedAt,
                CountWords(document.Text),
                document.FolderId))
            .ToList();

        return Task.FromResult<IReadOnlyList<StoredScriptSummary>>(summaries);
    }

    public Task<StoredScriptDocument?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        _documents.TryGetValue(id, out var document);
        return Task.FromResult(document);
    }

    public Task<StoredScriptDocument> SaveAsync(
        string title,
        string text,
        string? documentName = null,
        string? existingId = null,
        string? folderId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedTitle = string.IsNullOrWhiteSpace(title) ? "Untitled Script" : title.Trim();
        var normalizedDocumentName = string.IsNullOrWhiteSpace(documentName)
            ? $"{Slugify(normalizedTitle)}.tps"
            : documentName;
        var id = string.IsNullOrWhiteSpace(existingId)
            ? Slugify(Path.GetFileNameWithoutExtension(normalizedDocumentName))
            : existingId;
        var persistedFolderId = ResolveFolderId(existingId, folderId);

        var document = new StoredScriptDocument(
            id,
            normalizedTitle,
            text ?? string.Empty,
            normalizedDocumentName,
            DateTimeOffset.UtcNow,
            persistedFolderId);

        _documents[id] = document;
        return Task.FromResult(document);
    }

    public Task MoveToFolderAsync(string id, string? folderId, CancellationToken cancellationToken = default)
    {
        if (_documents.TryGetValue(id, out var document))
        {
            _documents[id] = document with
            {
                UpdatedAt = DateTimeOffset.UtcNow,
                FolderId = string.IsNullOrWhiteSpace(folderId) ? null : folderId
            };
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _documents.Remove(id);
        return Task.CompletedTask;
    }

    private static int CountWords(string text) =>
        string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;

    private static string Slugify(string value)
    {
        var slug = new string(value
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray());

        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        slug = slug.Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "untitled-script" : slug;
    }

    private string? ResolveFolderId(string? existingId, string? folderId)
    {
        if (!string.IsNullOrWhiteSpace(folderId))
        {
            return folderId;
        }

        if (string.IsNullOrWhiteSpace(existingId))
        {
            return null;
        }

        return _documents.TryGetValue(existingId, out var existingDocument)
            ? existingDocument.FolderId
            : null;
    }
}

internal sealed class InMemoryLibraryFolderRepository : ILibraryFolderRepository
{
    private readonly Dictionary<string, StoredLibraryFolder> _folders = new(StringComparer.Ordinal);

    public Task InitializeAsync(IEnumerable<StoredLibraryFolder> seedFolders, CancellationToken cancellationToken = default)
    {
        foreach (var folder in seedFolders)
        {
            _folders[folder.Id] = folder;
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<StoredLibraryFolder>> ListAsync(CancellationToken cancellationToken = default)
    {
        var folders = _folders.Values
            .OrderBy(folder => folder.DisplayOrder)
            .ThenBy(folder => folder.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult<IReadOnlyList<StoredLibraryFolder>>(folders);
    }

    public Task<StoredLibraryFolder> CreateAsync(
        string name,
        string? parentId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        var folder = new StoredLibraryFolder(
            Id: BuildUniqueId(normalizedName),
            Name: normalizedName,
            ParentId: string.IsNullOrWhiteSpace(parentId) ? null : parentId,
            DisplayOrder: ResolveDisplayOrder(parentId),
            UpdatedAt: DateTimeOffset.UtcNow);

        _folders[folder.Id] = folder;
        return Task.FromResult(folder);
    }

    private string BuildUniqueId(string name)
    {
        var baseId = Slugify(name);
        if (!_folders.ContainsKey(baseId))
        {
            return baseId;
        }

        var suffix = 2;
        var candidate = baseId;
        while (_folders.ContainsKey(candidate))
        {
            candidate = $"{baseId}-{suffix}";
            suffix++;
        }

        return candidate;
    }

    private int ResolveDisplayOrder(string? parentId) =>
        _folders.Values
            .Where(folder => string.Equals(folder.ParentId, parentId, StringComparison.Ordinal))
            .Select(folder => folder.DisplayOrder)
            .DefaultIfEmpty(-1)
            .Max() + 1;

    private static string Slugify(string value)
    {
        var slug = new string(value
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray());

        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        slug = slug.Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "untitled-folder" : slug;
    }
}
