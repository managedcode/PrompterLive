namespace PrompterOne.Shared.Pages;

public partial class SettingsPage
{
    private const string DisplayNoneStyle = "display:none";
    private const string SetNavItemCssClass = "set-nav-item";

    private SettingsSection _activeSection = SettingsSection.Cloud;

    private string GetNavItemCssClass(SettingsSection section) =>
        section == _activeSection ? $"{SetNavItemCssClass} {ActiveCssClass}" : SetNavItemCssClass;

    private string GetSectionDisplayStyle(SettingsSection section) =>
        section == _activeSection ? string.Empty : DisplayNoneStyle;

    private void ShowSection(SettingsSection section) => _activeSection = section;
}
