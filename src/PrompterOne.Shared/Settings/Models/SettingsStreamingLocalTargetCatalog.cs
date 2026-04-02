using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Contracts;

namespace PrompterOne.Shared.Settings.Models;

public sealed record SettingsStreamingLocalTargetDefinition(
    string TargetId,
    string CardId,
    string Name,
    string AccountLabel,
    string Description,
    string ToggleTestId);

public static class SettingsStreamingLocalTargetCatalog
{
    public static IReadOnlyList<SettingsStreamingLocalTargetDefinition> All { get; } =
    [
        new(
            GoLiveTargetCatalog.TargetIds.Obs,
            SettingsStreamingCardIds.Obs,
            GoLiveTargetCatalog.TargetNames.Obs,
            "OBS Virtual Camera · Local browser output",
            "Keep the browser program available to OBS Virtual Camera without touching the live runtime screen.",
            UiTestIds.Settings.StreamingObsToggle),
        new(
            GoLiveTargetCatalog.TargetIds.Ndi,
            SettingsStreamingCardIds.Ndi,
            GoLiveTargetCatalog.TargetNames.Ndi,
            "NDI Output · Local network",
            "Publish the browser program as an NDI output target configured in local browser storage.",
            UiTestIds.Settings.StreamingNdiToggle),
        new(
            GoLiveTargetCatalog.TargetIds.Recording,
            SettingsStreamingCardIds.Recording,
            GoLiveTargetCatalog.TargetNames.Recording,
            "Local Recording · Browser workspace",
            "Arm local recording before going live, then control recording from the dedicated live runtime.",
            UiTestIds.Settings.StreamingRecordingToggle)
    ];
}
