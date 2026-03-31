using Microsoft.Extensions.DependencyInjection;
using PrompterLive.Core.Abstractions;
using PrompterLive.Core.Services;
using PrompterLive.Core.Services.Editor;
using PrompterLive.Core.Services.Media;
using PrompterLive.Core.Services.Preview;
using PrompterLive.Core.Services.Rsvp;
using PrompterLive.Core.Services.Streaming;
using PrompterLive.Core.Services.Workspace;
using PrompterLive.Shared.Services.Diagnostics;
using PrompterLive.Shared.Services.Editor;

namespace PrompterLive.Shared.Services;

public static class PrompterLiveServiceCollectionExtensions
{
    public static IServiceCollection AddPrompterLiveShared(this IServiceCollection services)
    {
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
        services.AddScoped<BrowserThemeService>();
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
