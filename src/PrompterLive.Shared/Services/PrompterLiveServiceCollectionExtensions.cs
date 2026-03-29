using Microsoft.Extensions.DependencyInjection;
using PrompterLive.Core.Abstractions;
using PrompterLive.Core.Services;
using PrompterLive.Core.Services.Media;
using PrompterLive.Core.Services.Preview;
using PrompterLive.Core.Services.Rsvp;
using PrompterLive.Core.Services.Streaming;
using PrompterLive.Core.Services.Workspace;

namespace PrompterLive.Shared.Services;

public static class PrompterLiveServiceCollectionExtensions
{
    public static IServiceCollection AddPrompterLiveShared(this IServiceCollection services)
    {
        services.AddScoped<TpsParser>();
        services.AddScoped<ScriptCompiler>();
        services.AddScoped<TpsExporter>();
        services.AddScoped<RsvpOrpCalculator>();
        services.AddScoped<RsvpEmotionAnalyzer>();
        services.AddScoped<RsvpTextProcessor>();
        services.AddScoped<RsvpPlaybackEngine>();
        services.AddScoped<IScriptPreviewService, ScriptPreviewService>();

        services.AddScoped<IScriptRepository, BrowserScriptRepository>();
        services.AddScoped<IScriptSessionService, ScriptSessionService>();
        services.AddScoped<IMediaPermissionService, BrowserMediaPermissionService>();
        services.AddScoped<IMediaDeviceService, BrowserMediaDeviceService>();
        services.AddScoped<IMediaSceneService, MediaSceneService>();
        services.AddScoped<BrowserSettingsStore>();
        services.AddScoped<StudioSettingsStore>();
        services.AddScoped<CameraPreviewInterop>();
        services.AddScoped<AppBootstrapper>();

        services.AddSingleton<IStreamingOutputProvider, LiveKitOutputProvider>();
        services.AddSingleton<IStreamingOutputProvider, VdoNinjaOutputProvider>();
        services.AddSingleton<IStreamingOutputProvider, RtmpStreamingOutputProvider>();

        return services;
    }
}
