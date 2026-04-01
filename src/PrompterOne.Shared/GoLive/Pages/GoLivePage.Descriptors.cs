using PrompterOne.Core.Models.Media;
using PrompterOne.Shared.GoLive.Models;

namespace PrompterOne.Shared.Pages;

public partial class GoLivePage
{
    private static string FormatRouteTarget(AudioRouteTarget routeTarget) =>
        routeTarget switch
        {
            AudioRouteTarget.Monitor => GoLiveText.Audio.MonitorOnlyLabel,
            AudioRouteTarget.Stream => GoLiveText.Audio.StreamOnlyLabel,
            _ => GoLiveText.Audio.DefaultMicrophoneRouteLabel
        };

    private sealed record GoLiveDestinationState(
        bool IsEnabled,
        bool IsReady,
        string StatusLabel,
        string Summary);
}
