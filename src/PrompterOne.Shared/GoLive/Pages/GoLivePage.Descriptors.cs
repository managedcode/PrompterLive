using PrompterOne.Core.Models.Media;
using PrompterOne.Shared.GoLive.Models;

namespace PrompterOne.Shared.Pages;

public partial class GoLivePage
{
    private string FormatRouteTarget(AudioRouteTarget routeTarget) =>
        routeTarget switch
        {
            AudioRouteTarget.Monitor => Text(GoLiveText.Audio.MonitorOnlyLabel),
            AudioRouteTarget.Stream => Text(GoLiveText.Audio.StreamOnlyLabel),
            _ => Text(GoLiveText.Audio.DefaultMicrophoneRouteLabel)
        };

    private sealed record GoLiveDestinationState(
        bool IsEnabled,
        bool IsReady,
        string StatusLabel,
        string Summary);
}
