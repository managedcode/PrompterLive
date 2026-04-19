using System.Text.Json;
using Bunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using PrompterOne.Core.Abstractions;
using PrompterOne.Core.AI.Abstractions;
using PrompterOne.Core.AI.Models;
using PrompterOne.Core.AI.Services;
using PrompterOne.Core.Localization;
using PrompterOne.Core.Models.Documents;
using PrompterOne.Core.Models.Library;
using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Streaming;
using PrompterOne.Core.Services;
using PrompterOne.Core.Services.Editor;
using PrompterOne.Core.Services.Media;
using PrompterOne.Core.Services.Preview;
using PrompterOne.Core.Services.Rsvp;
using PrompterOne.Core.Services.Streaming;
using PrompterOne.Core.Services.Workspace;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Services.Diagnostics;
using PrompterOne.Shared.Services.Editor;
using PrompterOne.Shared.Settings.Models;
using PrompterOne.Shared.Settings.Services;
using PrompterOne.Shared.Storage.Cloud;
using PrompterOne.Web.Tests;

namespace PrompterOne.Shared.Tests;

internal static class TestHarnessFactory
{
    public static AppHarness Create(
        BunitContext context,
        IReadOnlyList<MediaDeviceInfo>? devices = null,
        Action<ILoggingBuilder>? configureLogging = null,
        TimeSpan? jsInvocationDelay = null,
        bool seedLibraryData = true,
        RuntimeTelemetryOptions? runtimeTelemetryOptions = null)
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
        var documentReader = new TpsDocumentReader();
        var scriptDataFactory = new TpsScriptDataFactory();
        var compiler = new ScriptCompiler();
        var frontMatter = new TpsFrontMatterDocumentService();
        var documentSplitService = new TpsDocumentSplitService();
        var scriptImportDescriptorService = new ScriptImportDescriptorService();
        var scriptDocumentImportService = new ScriptDocumentImportService(scriptImportDescriptorService);
        var droppedScriptMergeService = new EditorDroppedScriptMergeService();
        var textEditor = new TpsTextEditor();
        var structureEditor = new TpsStructureEditor();
        var previewService = new ScriptPreviewService(documentReader, compiler);
        var session = new ScriptSessionService(
            repository,
            documentReader,
            scriptDataFactory,
            compiler,
            previewService,
            loggerFactory.CreateLogger<ScriptSessionService>());
        var sceneService = new MediaSceneService();
        var permissionService = new FakeMediaPermissionService();
        var deviceService = new FakeMediaDeviceService(devices ?? DefaultDevices);
        var crossTabMessageBus = new CrossTabMessageBus(
            jsRuntime,
            loggerFactory.CreateLogger<CrossTabMessageBus>());
        var sentryClient = new FakeSentryRuntimeClient();
        var settingsStore = new BrowserSettingsStore(
            jsRuntime,
            crossTabMessageBus,
            loggerFactory.CreateLogger<BrowserSettingsStore>());
        var shell = new AppShellService();
        var scriptAgentRuntime = new FakeScriptAgentRuntime();
        var bootstrapper = new AppBootstrapper(
            session,
            repository,
            folderRepository,
            sceneService,
            settingsStore,
            loggerFactory.CreateLogger<AppBootstrapper>());

        jsRuntime.SavedValues[SettingsPagePreferences.StorageKey] = SettingsPagePreferences.Default with
        {
            HasSeenOnboarding = true
        };

        if (seedLibraryData)
        {
            folderRepository.InitializeAsync(AppTestLibrarySeedData.CreateFolders()).GetAwaiter().GetResult();
            repository.InitializeAsync(AppTestLibrarySeedData.CreateDocuments()).GetAwaiter().GetResult();
        }

        context.Services.AddLocalization();
        context.Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        context.Services.AddSingleton(jsRuntime);
        context.Services.AddSingleton<IJSRuntime>(jsRuntime);
        context.Services.AddSingleton<ILoggerFactory>(loggerFactory);
        context.Services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        context.Services.AddSingleton<ILibraryFolderRepository>(folderRepository);
        context.Services.AddSingleton<IScriptRepository>(repository);
        context.Services.AddSingleton<IScriptSessionService>(session);
        context.Services.AddSingleton(documentReader);
        context.Services.AddSingleton(scriptDataFactory);
        context.Services.AddSingleton(compiler);
        context.Services.AddSingleton(frontMatter);
        context.Services.AddSingleton(documentSplitService);
        context.Services.AddSingleton(scriptDocumentImportService);
        context.Services.AddSingleton(scriptImportDescriptorService);
        context.Services.AddSingleton(droppedScriptMergeService);
        context.Services.AddSingleton(textEditor);
        context.Services.AddSingleton(structureEditor);
        context.Services.AddSingleton<ScriptDocumentEditService>();
        context.Services.AddSingleton<ScriptKnowledgeGraphService>();
        context.Services.AddSingleton<ScriptAgentToolProvider>();
        context.Services.AddSingleton(scriptAgentRuntime);
        context.Services.AddSingleton<IScriptAgentRuntime>(scriptAgentRuntime);
        context.Services.AddSingleton<IScriptPreviewService>(previewService);
        context.Services.AddSingleton<EditorOutlineBuilder>();
        context.Services.AddSingleton<EditorInterop>();
        context.Services.AddSingleton<EditorMonacoInterop>();
        context.Services.AddSingleton<ScriptGraphViewerInterop>();
        context.Services.AddSingleton<EditorToolbarInterop>();
        context.Services.AddSingleton<AppShellFilePickerInterop>();
        context.Services.AddSingleton<EditorDocumentSaveCoordinator>();
        context.Services.AddSingleton<EditorLocalRevisionStore>();
        context.Services.AddSingleton<IMediaSceneService>(sceneService);
        context.Services.AddSingleton<IMediaPermissionService>(permissionService);
        context.Services.AddSingleton<IMediaDeviceService>(deviceService);
        context.Services.AddSingleton(crossTabMessageBus);
        context.Services.AddSingleton<RsvpOrpCalculator>();
        context.Services.AddSingleton<RsvpTextProcessor>();
        context.Services.AddSingleton<RsvpPlaybackEngine>();
        context.Services.AddSingleton(settingsStore);
        context.Services.AddSingleton<IUserSettingsStore>(settingsStore);
        context.Services.AddSingleton<IBrowserSettingsChangeNotifier>(settingsStore);
        context.Services.AddSingleton<AiProviderSettingsStore>();
        context.Services.AddSingleton<BrowserCloudStorageStore>();
        context.Services.AddSingleton<BrowserFileStorageStore>();
        context.Services.AddSingleton<AppCulturePreferenceService>();
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
        context.Services.AddSingleton<BrowserMediaCaptureCapabilitiesService>();
        context.Services.AddSingleton<CameraPreviewInterop>();
        context.Services.AddSingleton<LearnRsvpLayoutInterop>();
        context.Services.AddSingleton<MicrophoneLevelInterop>();
        context.Services.AddSingleton<TeleprompterReaderInterop>();
        context.Services.AddSingleton<KineticReaderInterop>();
        context.Services.AddSingleton<GoLiveOutputInterop>();
        context.Services.AddSingleton<GoLiveOutputRuntimeService>();
        context.Services.AddSingleton<GoLiveRemoteSourceInterop>();
        context.Services.AddSingleton<GoLiveRemoteSourceRuntimeService>();
        context.Services.AddSingleton(bootstrapper);
        context.Services.AddSingleton<AiSpotlightHotkeyInterop>();
        context.Services.AddSingleton<AiSpotlightService>();
        context.Services.AddSingleton<GoLiveSessionService>();
        context.Services.AddSingleton(runtimeTelemetryOptions ?? RuntimeTelemetryOptions.Disabled);
        context.Services.AddSingleton<ISentryRuntimeClient>(sentryClient);
        context.Services.AddSingleton<RuntimeTelemetryService>();
        context.Services.AddSingleton<SentryUserFeedbackService>();
        context.Services.AddSingleton<BrowserConnectivityInterop>();
        context.Services.AddSingleton<BrowserConnectivityService>();
        context.Services.AddSingleton<UiDiagnosticsService>();
        context.Services.AddScoped<MediaRuntimeContractService>();
        context.Services.AddScoped<ThemeRuntimeContractService>();
        context.Services.AddSingleton<IGoLiveSourceModule, LiveKitSourceModule>();
        context.Services.AddSingleton<IGoLiveSourceModule, VdoNinjaSourceModule>();
        context.Services.AddSingleton<IGoLiveOutputModule, LocalRecordingOutputModule>();
        context.Services.AddSingleton<IGoLiveOutputModule, LiveKitOutputModule>();
        context.Services.AddSingleton<IGoLiveOutputModule, VdoNinjaOutputModule>();
        context.Services.AddSingleton<IGoLiveModuleRegistry, GoLiveModuleRegistry>();
        context.Services.AddScoped<StreamingPublishDescriptorResolver>();

        return new AppHarness(
            jsRuntime,
            repository,
            folderRepository,
            session,
            sceneService,
            permissionService,
            deviceService,
            crossTabMessageBus,
            sentryClient,
            scriptAgentRuntime,
            loggerFactory);
    }

    private static IReadOnlyList<MediaDeviceInfo> DefaultDevices =>
    [
        new(AppTestData.Camera.FirstDeviceId, AppTestData.Camera.FrontCamera, MediaDeviceKind.Camera, true),
        new(AppTestData.Camera.SecondDeviceId, AppTestData.Camera.SideCamera, MediaDeviceKind.Camera),
        new(AppTestData.Microphone.PrimaryDeviceId, AppTestData.Scripts.BroadcastMic, MediaDeviceKind.Microphone, true)
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
    CrossTabMessageBus CrossTabMessageBus,
    FakeSentryRuntimeClient SentryClient,
    FakeScriptAgentRuntime ScriptAgentRuntime,
    ILoggerFactory LoggerFactory);

internal sealed class FakeScriptAgentRuntime : IScriptAgentRuntime
{
    public ScriptAgentRunResult? NextResult { get; set; }

    public Exception? NextException { get; set; }

    public int Calls { get; private set; }

    public ScriptAgentRunRequest? LastRequest { get; private set; }

    public Task<ScriptAgentRunResult> RunAsync(
        ScriptAgentRunRequest request,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        LastRequest = request;

        if (NextException is not null)
        {
            throw NextException;
        }

        return NextResult is not null
            ? Task.FromResult(NextResult)
            : throw new InvalidOperationException("No configured AI provider is available for script agents.");
    }
}

internal sealed record JsInvocationRecord(
    string Identifier,
    IReadOnlyList<object?> Arguments);

internal sealed class TestJsRuntime(TimeSpan? invocationDelay = null) : IJSRuntime
{
    private const string JSImportIdentifier = "import";
    private const string BrowserCultureGetLanguagesIdentifier = BrowserCultureInteropMethodNames.GetBrowserLanguages;
    private const string BrowserCultureSetDocumentLanguageIdentifier = BrowserCultureInteropMethodNames.SetDocumentLanguage;
    private const string BrowserMediaGetCaptureCapabilitiesIdentifier = BrowserMediaInteropMethodNames.GetCaptureCapabilities;
    private const string CrossTabDisposeIdentifier = "PrompterOneCrossTabInterop.dispose";
    private const string CrossTabInitializeIdentifier = "PrompterOneCrossTabInterop.initialize";
    private const string CrossTabPublishIdentifier = "PrompterOneCrossTabInterop.publish";
    private const string GoLiveGetSessionStateIdentifier = GoLiveOutputInteropMethodNames.GetSessionState;
    private const string GoLiveStartLiveKitIdentifier = GoLiveOutputInteropMethodNames.StartLiveKitSession;
    private const string GoLiveStartLocalRecordingIdentifier = GoLiveOutputInteropMethodNames.StartLocalRecording;
    private const string GoLiveRemoteGetSessionStateIdentifier = GoLiveRemoteSourceInteropMethodNames.GetSessionState;
    private const string GoLiveRemoteStopSessionIdentifier = GoLiveRemoteSourceInteropMethodNames.StopSession;
    private const string GoLiveRemoteSyncConnectionsIdentifier = GoLiveRemoteSourceInteropMethodNames.SyncConnections;
    private const string GoLiveStartVdoNinjaIdentifier = GoLiveOutputInteropMethodNames.StartVdoNinjaSession;
    private const string GoLiveStopLiveKitIdentifier = GoLiveOutputInteropMethodNames.StopLiveKitSession;
    private const string GoLiveStopLocalRecordingIdentifier = GoLiveOutputInteropMethodNames.StopLocalRecording;
    private const string GoLiveStopVdoNinjaIdentifier = GoLiveOutputInteropMethodNames.StopVdoNinjaSession;
    private const string GoLiveUpdateSessionDevicesIdentifier = GoLiveOutputInteropMethodNames.UpdateSessionDevices;
    private const string LoadSettingJsonIdentifier = "localStorage.getItem";
    private const string RemoveStorageValueIdentifier = "localStorage.removeItem";
    private const string RuntimeTelemetryModulePath = "./_content/PrompterOne.Shared/app/runtime-telemetry.js";
    private const string SaveSettingJsonIdentifier = "localStorage.setItem";
    private const int ActiveAudioLevelPercent = 64;
    private const int IdleAudioLevelPercent = 0;
    private const long RecordingSizeBytes = 4096;
    private const string RecordingFileName = "go-live-recording.webm";
    private const string RecordingMimeType = "video/webm";
    private const string RecordingSaveMode = "file-system";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly TimeSpan _invocationDelay = invocationDelay ?? TimeSpan.Zero;
    public IReadOnlyList<string> BrowserLanguages { get; private set; } = [AppCultureCatalog.EnglishCultureName];
    public BrowserMediaCaptureCapabilities CaptureCapabilities { get; set; } = BrowserMediaCaptureCapabilities.Default;
    public string DocumentLanguage { get; private set; } = AppCultureCatalog.DefaultCultureName;
    public Dictionary<string, object?> SavedValues { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> SavedJsonValues { get; } = new(StringComparer.Ordinal);
    public List<string> Invocations { get; } = [];
    public List<JsInvocationRecord> InvocationRecords { get; } = [];
    public Dictionary<string, Exception> ImportFailures { get; } = new(StringComparer.Ordinal);
    private Dictionary<string, GoLiveOutputRuntimeSnapshot> GoLiveSessions { get; } = new(StringComparer.Ordinal);
    private Dictionary<string, GoLiveRemoteSourceRuntimeSnapshot> GoLiveRemoteSessions { get; } = new(StringComparer.Ordinal);

    public void SetBrowserLanguages(params string[] languages)
    {
        BrowserLanguages = languages
            .Where(language => !string.IsNullOrWhiteSpace(language))
            .ToArray();
    }

    public void SeedRemoteSources(
        string sessionId,
        params (string ConnectionId, string SourceId, string Label, StreamingPlatformKind PlatformKind, string RoomName, string ServerUrl, bool IsConnected)[] sources)
    {
        var connectionSnapshots = sources
            .GroupBy(source => source.ConnectionId, StringComparer.Ordinal)
            .Select(group =>
            {
                var first = group.First();
                return new GoLiveRemoteConnectionSnapshot(
                    ConnectionId: first.ConnectionId,
                    Connected: first.IsConnected,
                    RoomName: first.RoomName,
                    ServerUrl: first.ServerUrl,
                    PlatformKind: (int)first.PlatformKind,
                    Sources: group.Select(source => new GoLiveRemoteSourceSnapshot(
                        ConnectionId: source.ConnectionId,
                        DeviceId: source.SourceId,
                        IsConnected: source.IsConnected,
                        Label: source.Label,
                        PlatformKind: (int)source.PlatformKind,
                        SourceId: source.SourceId)).ToArray());
            })
            .ToArray();

        GoLiveRemoteSessions[sessionId] = new GoLiveRemoteSourceRuntimeSnapshot(
            Connections: connectionSnapshots,
            Sources: connectionSnapshots.SelectMany(connection => connection.Sources ?? []).ToArray());
    }

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

        if (TryHandleGoLiveOutputInvocation<TValue>(identifier, args, out var goLiveResult))
        {
            return goLiveResult;
        }

        if (TryHandleGoLiveRemoteInvocation<TValue>(identifier, args, out var goLiveRemoteResult))
        {
            return goLiveRemoteResult;
        }

        var result = identifier switch
        {
            JSImportIdentifier => ImportModule(args),
            BrowserCultureGetLanguagesIdentifier => BrowserLanguages.ToArray(),
            BrowserCultureSetDocumentLanguageIdentifier => SetDocumentLanguage(args),
            BrowserMediaGetCaptureCapabilitiesIdentifier => CaptureCapabilities,
            CrossTabDisposeIdentifier => null,
            CrossTabInitializeIdentifier => true,
            CrossTabPublishIdentifier => null,
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

    private object? SetDocumentLanguage(object?[]? args)
    {
        DocumentLanguage = args?.FirstOrDefault()?.ToString() ?? AppCultureCatalog.DefaultCultureName;
        return null;
    }

    private object? ImportModule(object?[]? args)
    {
        var modulePath = args?.FirstOrDefault()?.ToString() ?? string.Empty;
        if (ImportFailures.TryGetValue(modulePath, out var exception))
        {
            throw exception;
        }

        return string.Equals(modulePath, RuntimeTelemetryModulePath, StringComparison.Ordinal)
            ? new TestJsModule(this)
            : null;
    }

    private bool TryHandleGoLiveOutputInvocation<TValue>(string identifier, object?[]? args, out TValue result)
    {
        result = default!;

        if (string.Equals(identifier, GoLiveGetSessionStateIdentifier, StringComparison.Ordinal))
        {
            var sessionId = args?.FirstOrDefault()?.ToString() ?? string.Empty;
            GoLiveSessions.TryGetValue(sessionId, out var snapshot);
            result = snapshot is TValue typedSnapshot
                ? typedSnapshot
                : default!;
            return true;
        }

        var invocationArgs = args ?? [];
        if (invocationArgs.Length < 1)
        {
            return false;
        }

        var targetSessionId = invocationArgs[0]?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(targetSessionId))
        {
            return false;
        }

        if (string.Equals(identifier, GoLiveStartLiveKitIdentifier, StringComparison.Ordinal))
        {
            GoLiveSessions[targetSessionId] = BuildGoLiveSnapshot(args, liveKitActive: true);
            return true;
        }

        if (string.Equals(identifier, GoLiveStartVdoNinjaIdentifier, StringComparison.Ordinal))
        {
            GoLiveSessions[targetSessionId] = BuildGoLiveSnapshot(args, vdoNinjaActive: true);
            return true;
        }

        if (string.Equals(identifier, GoLiveStartLocalRecordingIdentifier, StringComparison.Ordinal))
        {
            var existingSnapshot = GoLiveSessions.GetValueOrDefault(targetSessionId);
            GoLiveSessions[targetSessionId] = BuildGoLiveSnapshot(
                args,
                liveKitActive: existingSnapshot?.LiveKit?.Active == true,
                recordingActive: true,
                vdoNinjaActive: existingSnapshot?.VdoNinja?.Active == true);
            return true;
        }

        if (string.Equals(identifier, GoLiveUpdateSessionDevicesIdentifier, StringComparison.Ordinal))
        {
            var existingSnapshot = GoLiveSessions.GetValueOrDefault(targetSessionId);
            GoLiveSessions[targetSessionId] = BuildGoLiveSnapshot(
                args,
                liveKitActive: existingSnapshot?.LiveKit?.Active == true,
                recordingActive: existingSnapshot?.Recording?.Active == true,
                vdoNinjaActive: existingSnapshot?.VdoNinja?.Active == true);
            return true;
        }

        if (string.Equals(identifier, GoLiveStopLiveKitIdentifier, StringComparison.Ordinal))
        {
            UpdateGoLiveSession(targetSessionId, snapshot => snapshot with
            {
                Audio = GetAudioSnapshot(snapshot) with
                {
                    ProgramLevelPercent = snapshot.VdoNinja?.Active == true ? ActiveAudioLevelPercent : IdleAudioLevelPercent
                },
                LiveKit = GetProviderSnapshot(snapshot.LiveKit) with
                {
                    Active = false,
                    Connected = false,
                    RoomName = string.Empty,
                    ServerUrl = string.Empty
                }
            });
            return true;
        }

        if (string.Equals(identifier, GoLiveStopVdoNinjaIdentifier, StringComparison.Ordinal))
        {
            UpdateGoLiveSession(targetSessionId, snapshot => snapshot with
            {
                Audio = GetAudioSnapshot(snapshot) with
                {
                    ProgramLevelPercent = snapshot.LiveKit?.Active == true ? ActiveAudioLevelPercent : IdleAudioLevelPercent
                },
                VdoNinja = GetVdoNinjaSnapshot(snapshot) with
                {
                    Active = false,
                    Connected = false,
                    LastPeerLatencyMs = 0,
                    PeerCount = 0,
                    PublishUrl = string.Empty,
                    RoomName = string.Empty,
                    StreamId = string.Empty
                }
            });
            return true;
        }

        if (string.Equals(identifier, GoLiveStopLocalRecordingIdentifier, StringComparison.Ordinal))
        {
            UpdateGoLiveSession(targetSessionId, snapshot => snapshot with
            {
                Audio = GetAudioSnapshot(snapshot) with
                {
                    RecordingLevelPercent = IdleAudioLevelPercent
                },
                Recording = GetRecordingSnapshot(snapshot) with
                {
                    Active = false,
                    FileName = string.Empty,
                    MimeType = string.Empty,
                    RequestedAudioCodec = string.Empty,
                    RequestedContainer = string.Empty,
                    RequestedVideoCodec = string.Empty,
                    SaveMode = string.Empty,
                    SizeBytes = 0
                }
            });
            return true;
        }

        return false;
    }

    private bool TryHandleGoLiveRemoteInvocation<TValue>(string identifier, object?[]? args, out TValue result)
    {
        result = default!;

        if (string.Equals(identifier, GoLiveRemoteGetSessionStateIdentifier, StringComparison.Ordinal))
        {
            var sessionId = args?.FirstOrDefault()?.ToString() ?? string.Empty;
            GoLiveRemoteSessions.TryGetValue(sessionId, out var snapshot);
            result = snapshot is TValue typedSnapshot
                ? typedSnapshot
                : default!;
            return true;
        }

        var targetSessionId = args?.FirstOrDefault()?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(targetSessionId))
        {
            return false;
        }

        if (string.Equals(identifier, GoLiveRemoteStopSessionIdentifier, StringComparison.Ordinal))
        {
            GoLiveRemoteSessions.Remove(targetSessionId);
            return true;
        }

        if (string.Equals(identifier, GoLiveRemoteSyncConnectionsIdentifier, StringComparison.Ordinal))
        {
            if (!GoLiveRemoteSessions.ContainsKey(targetSessionId))
            {
                GoLiveRemoteSessions[targetSessionId] = new GoLiveRemoteSourceRuntimeSnapshot(
                    Connections: [],
                    Sources: []);
            }

            return true;
        }

        return false;
    }

    private void UpdateGoLiveSession(string sessionId, Func<GoLiveOutputRuntimeSnapshot, GoLiveOutputRuntimeSnapshot> update)
    {
        if (!GoLiveSessions.TryGetValue(sessionId, out var snapshot))
        {
            return;
        }

        GoLiveSessions[sessionId] = update(snapshot);
    }

    private static GoLiveOutputAudioSnapshot GetAudioSnapshot(GoLiveOutputRuntimeSnapshot snapshot) =>
        snapshot.Audio ?? new GoLiveOutputAudioSnapshot(IdleAudioLevelPercent, IdleAudioLevelPercent);

    private static GoLiveOutputProviderSnapshot GetProviderSnapshot(GoLiveOutputProviderSnapshot? snapshot) =>
        snapshot ?? new GoLiveOutputProviderSnapshot(false, false, string.Empty, string.Empty);

    private static GoLiveOutputRecordingSnapshot GetRecordingSnapshot(GoLiveOutputRuntimeSnapshot snapshot) =>
        snapshot.Recording ?? new GoLiveOutputRecordingSnapshot(
            Active: false,
            AudioBitrateKbps: 0,
            FileName: string.Empty,
            MimeType: string.Empty,
            RequestedAudioCodec: string.Empty,
            RequestedContainer: string.Empty,
            RequestedVideoCodec: string.Empty,
            SaveMode: string.Empty,
            SizeBytes: 0,
            VideoBitrateKbps: 0);

    private static GoLiveOutputVdoNinjaSnapshot GetVdoNinjaSnapshot(GoLiveOutputRuntimeSnapshot snapshot) =>
        snapshot.VdoNinja ?? new GoLiveOutputVdoNinjaSnapshot(
            Active: false,
            Connected: false,
            LastPeerLatencyMs: 0,
            PeerCount: 0,
            PublishUrl: string.Empty,
            RoomName: string.Empty,
            StreamId: string.Empty);

    private static GoLiveOutputRuntimeSnapshot BuildGoLiveSnapshot(
        IReadOnlyList<object?>? args,
        bool liveKitActive = false,
        bool recordingActive = false,
        bool vdoNinjaActive = false)
    {
        var request = args?.Skip(1).FirstOrDefault() as GoLiveOutputRuntimeRequest
            ?? throw new InvalidOperationException("Go Live output interop request is required for the fake JS runtime.");
        var liveKitConnections = request.GetPublishableConnections(StreamingPlatformKind.LiveKit);
        var vdoNinjaConnections = request.GetPublishableConnections(StreamingPlatformKind.VdoNinja);
        var liveKitConnection = liveKitConnections.Count > 0 ? liveKitConnections[0] : null;
        var vdoNinjaConnection = vdoNinjaConnections.Count > 0 ? vdoNinjaConnections[0] : null;

        return new(
            AudioDeviceId: request.PrimaryMicrophoneDeviceId,
            Audio: new GoLiveOutputAudioSnapshot(
                ProgramLevelPercent: liveKitActive || vdoNinjaActive ? ActiveAudioLevelPercent : IdleAudioLevelPercent,
                RecordingLevelPercent: recordingActive ? ActiveAudioLevelPercent : IdleAudioLevelPercent),
            HasMediaStream: request.VideoSources.Any(source => source.IsRenderable),
            LiveKit: new GoLiveOutputProviderSnapshot(
                Active: liveKitActive,
                Connected: liveKitActive,
                RoomName: liveKitActive ? liveKitConnection?.RoomName ?? string.Empty : string.Empty,
                ServerUrl: liveKitActive ? liveKitConnection?.ServerUrl ?? string.Empty : string.Empty),
            Program: new GoLiveOutputProgramSnapshot(
                AudioInputCount: request.AudioInputs.Count,
                FrameRate: request.ProgramVideo.FrameRate,
                Height: request.ProgramVideo.Height,
                PrimarySourceId: request.PrimarySourceId,
                VideoSourceCount: request.VideoSources.Count,
                Width: request.ProgramVideo.Width),
            Recording: new GoLiveOutputRecordingSnapshot(
                Active: recordingActive,
                AudioBitrateKbps: request.Recording.AudioBitrateKbps,
                FileName: recordingActive ? RecordingFileName : string.Empty,
                MimeType: recordingActive ? RecordingMimeType : string.Empty,
                RequestedAudioCodec: recordingActive ? request.Recording.AudioCodecLabel : string.Empty,
                RequestedContainer: recordingActive ? request.Recording.ContainerLabel : string.Empty,
                RequestedVideoCodec: recordingActive ? request.Recording.VideoCodecLabel : string.Empty,
                SaveMode: recordingActive ? RecordingSaveMode : string.Empty,
                SizeBytes: recordingActive ? RecordingSizeBytes : 0,
                VideoBitrateKbps: request.Recording.VideoBitrateKbps),
            VdoNinja: new GoLiveOutputVdoNinjaSnapshot(
                Active: vdoNinjaActive,
                Connected: vdoNinjaActive,
                LastPeerLatencyMs: vdoNinjaActive ? 42 : 0,
                PeerCount: vdoNinjaActive ? 1 : 0,
                PublishUrl: vdoNinjaActive ? vdoNinjaConnection?.PublishUrl ?? string.Empty : string.Empty,
                RoomName: vdoNinjaActive ? vdoNinjaConnection?.RoomName ?? string.Empty : string.Empty,
                StreamId: vdoNinjaActive ? GoLiveOutputRuntimeContract.SessionId : string.Empty),
            VideoDeviceId: request.PrimaryCameraDeviceId);
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

    private sealed class TestJsModule(TestJsRuntime owner) : IJSObjectReference
    {
        private readonly TestJsRuntime _owner = owner;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            _owner.Invocations.Add(identifier);
            _owner.InvocationRecords.Add(new JsInvocationRecord(identifier, args?.ToArray() ?? []));
            return ValueTask.FromResult(default(TValue)!);
        }
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
