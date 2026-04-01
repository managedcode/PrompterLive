using ManagedCode.Storage.Browser.Extensions;
using ManagedCode.Storage.Core.Extensions;
using ManagedCode.Storage.VirtualFileSystem.Extensions;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Core.Abstractions;
using PrompterOne.Core.Services;
using PrompterOne.Core.Services.Editor;
using PrompterOne.Core.Services.Media;
using PrompterOne.Core.Services.Preview;
using PrompterOne.Core.Services.Rsvp;
using PrompterOne.Core.Services.Streaming;
using PrompterOne.Core.Services.Workspace;
using PrompterOne.Shared.Services.Diagnostics;
using PrompterOne.Shared.Services.Editor;
using PrompterOne.Shared.Storage;
using PrompterOne.Shared.Storage.Cloud;

namespace PrompterOne.Shared.Services;

public static class PrompterOneServiceCollectionExtensions
{
    public static IServiceCollection AddPrompterOneShared(this IServiceCollection services)
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

        services.AddScoped<TpsParser>();
        services.AddScoped<ScriptCompiler>();
        services.AddScoped<TpsExporter>();
        services.AddScoped<TpsFrontMatterDocumentService>();
        services.AddScoped<TpsTextEditor>();
        services.AddScoped<TpsStructureEditor>();
        services.AddScoped<EditorLocalAssistant>();
        services.AddScoped<RsvpOrpCalculator>();
        services.AddScoped<RsvpEmotionAnalyzer>();
        services.AddScoped<RsvpTextProcessor>();
        services.AddScoped<RsvpPlaybackEngine>();
        services.AddScoped<IScriptPreviewService, ScriptPreviewService>();
        services.AddScoped<EditorOutlineBuilder>();
        services.AddScoped<EditorInterop>();

        services.AddScoped<ILibraryFolderRepository, BrowserLibraryFolderRepository>();
        services.AddScoped<IScriptRepository, BrowserScriptRepository>();
        services.AddScoped<IScriptSessionService, ScriptSessionService>();
        services.AddScoped<IMediaPermissionService, BrowserMediaPermissionService>();
        services.AddScoped<IMediaDeviceService, BrowserMediaDeviceService>();
        services.AddScoped<IMediaSceneService, MediaSceneService>();
        services.AddScoped<BrowserSettingsStore>();
        services.AddScoped<IUserSettingsStore>(serviceProvider => serviceProvider.GetRequiredService<BrowserSettingsStore>());
        services.AddScoped<BrowserCloudStorageStore>();
        services.AddScoped<BrowserThemeService>();
        services.AddScoped<CloudStorageProviderFactory>();
        services.AddScoped<CloudStorageTransferService>();
        services.AddScoped<StudioSettingsStore>();
        services.AddScoped<CameraPreviewInterop>();
        services.AddScoped<LearnRsvpLayoutInterop>();
        services.AddScoped<MicrophoneLevelInterop>();
        services.AddScoped<TeleprompterReaderInterop>();
        services.AddScoped<AppBootstrapper>();
        services.AddScoped<AppShellService>();
        services.AddScoped<BrowserConnectivityService>();
        services.AddScoped<GoLiveSessionService>();
        services.AddScoped<GoLiveOutputInterop>();
        services.AddScoped<GoLiveOutputRuntimeService>();
        services.AddScoped<UiDiagnosticsService>();

        services.AddSingleton<IStreamingOutputProvider, LiveKitOutputProvider>();
        services.AddSingleton<IStreamingOutputProvider, VdoNinjaOutputProvider>();
        services.AddSingleton<IStreamingOutputProvider, RtmpStreamingOutputProvider>();

        return services;
    }
}
