using ManagedCode.Storage.Browser.Extensions;
using ManagedCode.Storage.Core.Extensions;
using ManagedCode.Storage.VirtualFileSystem.Extensions;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Core.Abstractions;
using PrompterOne.Core.AI.Abstractions;
using PrompterOne.Core.AI.Agents;
using PrompterOne.Core.AI.Providers;
using PrompterOne.Core.AI.Services;
using PrompterOne.Core.AI.Workflows;
using PrompterOne.Core.Services;
using PrompterOne.Core.Services.Editor;
using PrompterOne.Core.Services.Media;
using PrompterOne.Core.Services.Preview;
using PrompterOne.Core.Services.Rsvp;
using PrompterOne.Core.Services.Streaming;
using PrompterOne.Core.Services.Workspace;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Services.Diagnostics;
using PrompterOne.Shared.Services.Editor;
using PrompterOne.Shared.Settings.Services;
using PrompterOne.Shared.Storage;
using PrompterOne.Shared.Storage.Cloud;

namespace PrompterOne.Shared.Services;

public static class PrompterOneServiceCollectionExtensions
{
    public static IServiceCollection AddPrompterOneShared(
        this IServiceCollection services,
        RuntimeTelemetryOptions? runtimeTelemetryOptions = null)
    {
        services.AddStorageFactory();
        services.AddBrowserStorageAsDefault(options =>
        {
            options.ContainerName = PrompterStorageDefaults.LocalBrowserContainerName;
            options.DatabaseName = PrompterStorageDefaults.LocalBrowserDatabaseName;
            options.ChunkSizeBytes = PrompterStorageDefaults.BrowserChunkSizeBytes;
            options.ChunkBatchSize = PrompterStorageDefaults.BrowserChunkBatchSize;
        });
        services.AddVirtualFileSystem(options =>
        {
            options.DefaultContainer = PrompterStorageDefaults.LocalBrowserContainerName;
        });

        services.AddScoped<TpsDocumentReader>();
        services.AddScoped<TpsScriptDataFactory>();
        services.AddScoped<ScriptCompiler>();
        services.AddScoped<TpsExporter>();
        services.AddScoped<TpsFrontMatterDocumentService>();
        services.AddScoped<TpsDocumentSplitService>();
        services.AddScoped<ScriptDocumentImportService>();
        services.AddScoped<ScriptImportDescriptorService>();
        services.AddScoped<EditorDroppedScriptMergeService>();
        services.AddScoped<TpsTextEditor>();
        services.AddScoped<TpsStructureEditor>();
        services.AddScoped<ScriptDocumentEditService>();
        services.AddScoped<ScriptKnowledgeGraphService>();
        services.AddScoped<ScriptKnowledgeGraphTokenizerSimilarityExtractor>();
        services.AddSingleton<ScriptAgent, AssistantScriptAgent>();
        services.AddSingleton<ScriptAgent, WriterScriptAgent>();
        services.AddSingleton<ScriptAgent, ReviewerScriptAgent>();
        services.AddSingleton<ScriptWorkflow, AssistantScriptWorkflow>();
        services.AddSingleton<ScriptWorkflow, WriterReviewSequentialWorkflow>();
        services.AddSingleton<ScriptWorkflow, WriterReviewGroupChatWorkflow>();
        services.AddScoped<RsvpOrpCalculator>();
        services.AddScoped<RsvpTextProcessor>();
        services.AddScoped<RsvpPlaybackEngine>();
        services.AddScoped<IScriptPreviewService, ScriptPreviewService>();
        services.AddScoped<EditorOutlineBuilder>();
        services.AddScoped<EditorInterop>();
        services.AddScoped<EditorMonacoInterop>();
        services.AddScoped<ScriptGraphViewerInterop>();
        services.AddScoped<EditorToolbarInterop>();
        services.AddScoped<AppShellFilePickerInterop>();
        services.AddScoped<EditorDocumentSaveCoordinator>();
        services.AddScoped<EditorLocalRevisionStore>();
        services.AddScoped<EditorBlockAttachmentStore>();
        services.AddScoped<GoLiveBlockTakeStore>();

        services.AddScoped<ILibraryFolderRepository, BrowserLibraryFolderRepository>();
        services.AddScoped<IScriptRepository, BrowserScriptRepository>();
        services.AddScoped<IScriptSessionService, ScriptSessionService>();
        services.AddScoped<IMediaPermissionService, BrowserMediaPermissionService>();
        services.AddScoped<IMediaDeviceService, BrowserMediaDeviceService>();
        services.AddScoped<IMediaSceneService, MediaSceneService>();
        services.AddScoped<CrossTabMessageBus>();
        services.AddScoped<BrowserSettingsStore>();
        services.AddScoped<IUserSettingsStore>(serviceProvider => serviceProvider.GetRequiredService<BrowserSettingsStore>());
        services.AddScoped<IBrowserSettingsChangeNotifier>(serviceProvider => serviceProvider.GetRequiredService<BrowserSettingsStore>());
        services.AddScoped<AiProviderSettingsStore>();
        services.AddScoped<IAgentRuntimeSettingsSource, AiProviderAgentRuntimeSettingsSource>();
        services.AddScoped<IScriptKnowledgeGraphSemanticExtractor, ScriptKnowledgeGraphLlmExtractor>();
        services.AddSingleton<EmbeddedAgentSkillsProvider>();
        services.AddScoped<ScriptAgentToolProvider>();
        services.AddScoped<IScriptAgentFactory, ScriptAgentFactory>();
        services.AddScoped<ScriptAgentRuntime>();
        services.AddScoped<IScriptAgentRuntime>(serviceProvider => serviceProvider.GetRequiredService<ScriptAgentRuntime>());
        services.AddScoped<BrowserCloudStorageStore>();
        services.AddScoped<BrowserFileStorageStore>();
        services.AddScoped<BrowserThemeService>();
        services.AddScoped<AppCulturePreferenceService>();
        services.AddScoped<CloudStorageProviderFactory>();
        services.AddScoped<CloudStorageTransferService>();
        services.AddScoped<StudioSettingsStore>();
        services.AddScoped<MediaRuntimeContractService>();
        services.AddScoped<ThemeRuntimeContractService>();
        services.AddScoped<BrowserMediaCaptureCapabilitiesService>();
        services.AddScoped<CameraPreviewInterop>();
        services.AddScoped<LearnRsvpLayoutInterop>();
        services.AddScoped<MicrophoneLevelInterop>();
        services.AddScoped<TeleprompterReaderInterop>();
        services.AddScoped<KineticReaderInterop>();
        services.AddScoped<AppBootstrapper>();
        services.AddScoped<AiSpotlightHotkeyInterop>();
        services.AddScoped<AiSpotlightService>();
        services.AddScoped<AppShellService>();
        services.AddSingleton(runtimeTelemetryOptions ?? RuntimeTelemetryOptions.Disabled);
        services.AddSingleton<ISentryRuntimeClient, SentryRuntimeClient>();
        services.AddScoped<RuntimeTelemetryService>();
        services.AddScoped<SentryUserFeedbackService>();
        services.AddScoped<BrowserConnectivityInterop>();
        services.AddScoped<BrowserConnectivityService>();
        services.AddScoped<GoLiveSessionService>();
        services.AddScoped<GoLiveOutputInterop>();
        services.AddScoped<GoLiveOutputRuntimeService>();
        services.AddScoped<GoLiveRemoteSourceInterop>();
        services.AddScoped<GoLiveRemoteSourceRuntimeService>();
        services.AddScoped<StreamingPublishDescriptorResolver>();
        services.AddScoped<UiDiagnosticsService>();

        services.AddSingleton<IGoLiveSourceModule, LiveKitSourceModule>();
        services.AddSingleton<IGoLiveSourceModule, VdoNinjaSourceModule>();
        services.AddSingleton<IGoLiveOutputModule, LocalRecordingOutputModule>();
        services.AddSingleton<IGoLiveOutputModule, LiveKitOutputModule>();
        services.AddSingleton<IGoLiveOutputModule, VdoNinjaOutputModule>();
        services.AddSingleton<IGoLiveModuleRegistry, GoLiveModuleRegistry>();

        return services;
    }
}
