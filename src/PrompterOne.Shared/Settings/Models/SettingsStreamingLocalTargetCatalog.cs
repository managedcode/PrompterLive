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
            GoLiveTargetCatalog.TargetIds.Recording,
            SettingsStreamingCardIds.Recording,
            GoLiveTargetCatalog.TargetNames.Recording,
            "Browser recording · Program feed",
            "Arm local recording as a first-class sink of the same composed program feed used by the live transports.",
            UiTestIds.Settings.StreamingRecordingToggle)
    ];
}
