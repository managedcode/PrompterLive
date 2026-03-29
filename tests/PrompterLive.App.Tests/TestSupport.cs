using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Bunit;
using PrompterLive.Core.Abstractions;
using PrompterLive.Core.Models.Documents;
using PrompterLive.Core.Models.Library;
using PrompterLive.Core.Models.Media;
using PrompterLive.Core.Services;
using PrompterLive.Core.Services.Media;
using PrompterLive.Core.Services.Preview;
using PrompterLive.Core.Services.Rsvp;
using PrompterLive.Core.Services.Streaming;
using PrompterLive.Core.Services.Workspace;
using PrompterLive.Shared.Services;

namespace PrompterLive.Shared.Tests;

internal static class TestHarnessFactory
{
    public static AppHarness Create(BunitContext context, IReadOnlyList<MediaDeviceInfo>? devices = null)
    {
        var jsRuntime = new TestJsRuntime();
        var repository = new InMemoryScriptRepository();
        var folderRepository = new InMemoryLibraryFolderRepository();
        var parser = new TpsParser();
        var compiler = new ScriptCompiler();
        var previewService = new ScriptPreviewService(parser, compiler);
        var session = new ScriptSessionService(repository, parser, compiler, previewService);
        var sceneService = new MediaSceneService();
        var permissionService = new FakeMediaPermissionService();
        var deviceService = new FakeMediaDeviceService(devices ?? DefaultDevices);

        context.Services.AddSingleton<IJSRuntime>(jsRuntime);
        context.Services.AddSingleton<ILibraryFolderRepository>(folderRepository);
        context.Services.AddSingleton<IScriptRepository>(repository);
        context.Services.AddSingleton<IScriptSessionService>(session);
        context.Services.AddSingleton(parser);
        context.Services.AddSingleton(compiler);
        context.Services.AddSingleton<IScriptPreviewService>(previewService);
        context.Services.AddSingleton<IMediaSceneService>(sceneService);
        context.Services.AddSingleton<IMediaPermissionService>(permissionService);
        context.Services.AddSingleton<IMediaDeviceService>(deviceService);
        context.Services.AddSingleton<RsvpOrpCalculator>();
        context.Services.AddSingleton<RsvpTextProcessor>();
        context.Services.AddSingleton<RsvpEmotionAnalyzer>();
        context.Services.AddSingleton<RsvpPlaybackEngine>();
        context.Services.AddSingleton<BrowserSettingsStore>();
        context.Services.AddSingleton<StudioSettingsStore>();
        context.Services.AddSingleton<CameraPreviewInterop>();
        context.Services.AddSingleton<AppBootstrapper>();
        context.Services.AddSingleton<IStreamingOutputProvider, LiveKitOutputProvider>();
        context.Services.AddSingleton<IStreamingOutputProvider, VdoNinjaOutputProvider>();
        context.Services.AddSingleton<IStreamingOutputProvider, RtmpStreamingOutputProvider>();

        return new AppHarness(jsRuntime, repository, folderRepository, session, sceneService, permissionService, deviceService);
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
    FakeMediaDeviceService DeviceService);

internal sealed class TestJsRuntime : IJSRuntime
{
    public Dictionary<string, object?> SavedValues { get; } = new(StringComparer.Ordinal);
    public List<string> Invocations { get; } = [];

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        Invocations.Add(identifier);

        object? result = identifier switch
        {
            "PrompterLive.settings.load" => Load(args),
            "PrompterLive.settings.save" => Save(args),
            _ => null
        };

        if (result is null)
        {
            return ValueTask.FromResult(default(TValue)!);
        }

        if (result is TValue typed)
        {
            return ValueTask.FromResult(typed);
        }

        return ValueTask.FromResult((TValue)result);
    }

    private object? Load(object?[]? args)
    {
        var key = args?.FirstOrDefault()?.ToString() ?? string.Empty;
        return SavedValues.TryGetValue(key, out var value) ? value : null;
    }

    private object? Save(object?[]? args)
    {
        var key = args?.FirstOrDefault()?.ToString() ?? string.Empty;
        SavedValues[key] = args?.Skip(1).FirstOrDefault();
        return null;
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

internal sealed class FakeMediaDeviceService : IMediaDeviceService
{
    private readonly IReadOnlyList<MediaDeviceInfo> _devices;

    public FakeMediaDeviceService(IReadOnlyList<MediaDeviceInfo> devices)
    {
        _devices = devices;
    }

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
