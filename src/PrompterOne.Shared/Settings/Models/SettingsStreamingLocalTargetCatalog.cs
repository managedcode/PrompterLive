using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Contracts;

namespace PrompterOne.Shared.Settings.Models;

public sealed record SettingsStreamingLocalTargetDefinition(
    string TargetId,
    string CardId,
    string NameKey,
    string AccountLabelKey,
    string DescriptionKey,
    string ToggleTestId);

public static class SettingsStreamingLocalTargetCatalog
{
    public static IReadOnlyList<SettingsStreamingLocalTargetDefinition> All { get; } =
    [
        new(
            GoLiveTargetCatalog.TargetIds.Recording,
            SettingsStreamingCardIds.Recording,
            "SettingsStreamingLocalRecordingTitle",
            "SettingsStreamingLocalRecordingAccountLabel",
            "SettingsStreamingLocalRecordingDescription",
            UiTestIds.Settings.StreamingRecordingToggle)
    ];
}
