using PrompterOne.Shared.Components;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Settings.Models;

namespace PrompterOne.Shared.Pages;

public partial class SettingsPage
{
    private const string DisplayNoneStyle = "display:none";

    private static readonly IReadOnlyList<SettingsSidebarNavigationItem> NavigationItems =
    [
        new(SettingsSection.Cloud, UiIconKind.Cloud, UiTextKey.SettingsNavCloud, UiTestIds.Settings.NavCloud),
        new(SettingsSection.Files, UiIconKind.Folder, UiTextKey.SettingsNavFiles, UiTestIds.Settings.NavFiles),
        new(SettingsSection.Cameras, UiIconKind.Camera, UiTextKey.SettingsNavCameras, UiTestIds.Settings.NavCameras),
        new(SettingsSection.Mics, UiIconKind.Microphone, UiTextKey.SettingsNavMicrophones, UiTestIds.Settings.NavMics),
        new(SettingsSection.Streaming, UiIconKind.ArrowRight, UiTextKey.SettingsNavStreaming, UiTestIds.Settings.NavStreaming),
        new(SettingsSection.Recording, UiIconKind.RecordTarget, UiTextKey.SettingsNavRecording, UiTestIds.Settings.NavRecording),
        new(SettingsSection.Ai, UiIconKind.Spark, UiTextKey.SettingsNavAi, UiTestIds.Settings.NavAi),
        new(SettingsSection.Appearance, UiIconKind.Theme, UiTextKey.SettingsNavAppearance, UiTestIds.Settings.NavAppearance),
        new(SettingsSection.Language, UiIconKind.Message, UiTextKey.SettingsNavLanguage, UiTestIds.Settings.NavLanguage),
        new(SettingsSection.Help, UiIconKind.BookOpen, UiTextKey.SettingsNavHelp, UiTestIds.Settings.NavHelp),
        new(SettingsSection.Shortcuts, UiIconKind.Keyboard, UiTextKey.SettingsNavShortcuts, UiTestIds.Settings.NavShortcuts),
        new(SettingsSection.Feedback, UiIconKind.Message, UiTextKey.SettingsNavFeedback, UiTestIds.Settings.NavFeedback),
        new(SettingsSection.About, UiIconKind.HelpCircle, UiTextKey.SettingsNavAbout, UiTestIds.Settings.NavAbout)
    ];

    private SettingsSection _activeSection = SettingsSection.Cloud;

    private string GetSectionDisplayStyle(SettingsSection section) =>
        section == _activeSection ? string.Empty : DisplayNoneStyle;

    private void ShowSection(SettingsSection section) => _activeSection = section;
}
