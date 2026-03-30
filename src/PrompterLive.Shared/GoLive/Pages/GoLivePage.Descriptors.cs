using PrompterLive.Core.Abstractions;
using PrompterLive.Core.Models.Media;
using PrompterLive.Core.Models.Streaming;
using PrompterLive.Core.Services.Streaming;

namespace PrompterLive.Shared.Pages;

public partial class GoLivePage
{
    private IReadOnlyList<string> GetDestinationSourceIds(string targetId) =>
        GoLiveDestinationRouting.GetSelectedSourceIds(_studioSettings.Streaming, targetId, SceneCameras);

    private static string FormatRouteTarget(AudioRouteTarget routeTarget) =>
        routeTarget switch
        {
            AudioRouteTarget.Monitor => "Monitor only",
            AudioRouteTarget.Stream => "Stream only",
            _ => DefaultMicRouteLabel
        };

    private static string BuildSelectedSourceSummary(int selectedSourceCount)
    {
        return string.Concat(
            selectedSourceCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            " ",
            selectedSourceCount == 1 ? SelectedCameraSingularLabel : SelectedCameraPluralLabel);
    }

    private static string BuildDisabledSummary(int selectedSourceCount)
    {
        return selectedSourceCount == 0
            ? DisabledSummary
            : string.Concat(DisabledReadyPrefix, " ", BuildSelectedSourceSummary(selectedSourceCount), ".");
    }

    private static string BuildReadySummary(int selectedSourceCount, string readySummary) =>
        string.Concat(BuildSelectedSourceSummary(selectedSourceCount), ". ", readySummary);

    private sealed record GoLiveDestinationState(
        bool IsEnabled,
        bool IsReady,
        string StatusLabel,
        string Summary);
}
