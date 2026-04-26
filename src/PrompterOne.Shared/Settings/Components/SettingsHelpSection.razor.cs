using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using PrompterOne.Shared.Localization;

namespace PrompterOne.Shared.Components.Settings;

public partial class SettingsHelpSection
{
    internal const string AppFlowCardId = "help-app-flow";
    internal const string LocalFilesCardId = "help-local-files";
    internal const string ModesCardId = "help-modes";
    internal const string TpsBasicsCardId = "help-tps-basics";

    private const string HelpSectionDescriptionKey = "SettingsHelpSectionDescription";
    private const string AppFlowTitleKey = "SettingsHelpAppFlowTitle";
    private const string AppFlowSubtitleKey = "SettingsHelpAppFlowSubtitle";
    private const string AppFlowCopyKey = "SettingsHelpAppFlowCopy";
    private const string TpsBasicsTitleKey = "SettingsHelpTpsBasicsTitle";
    private const string TpsBasicsSubtitleKey = "SettingsHelpTpsBasicsSubtitle";
    private const string TpsBasicsCopyKey = "SettingsHelpTpsBasicsCopy";
    private const string ModesTitleKey = "SettingsHelpModesTitle";
    private const string ModesSubtitleKey = "SettingsHelpModesSubtitle";
    private const string ModesCopyKey = "SettingsHelpModesCopy";
    private const string LocalFilesTitleKey = "SettingsHelpLocalFilesTitle";
    private const string LocalFilesSubtitleKey = "SettingsHelpLocalFilesSubtitle";
    private const string LocalFilesCopyKey = "SettingsHelpLocalFilesCopy";

    [Inject] private IStringLocalizer<SharedResource> Localizer { get; set; } = null!;

    [Parameter] public string DisplayStyle { get; set; } = string.Empty;
    [Parameter] public Func<string, bool> IsCardOpen { get; set; } = static _ => false;
    [Parameter] public EventCallback<string> ToggleCard { get; set; }

    private string AppFlowCopy => Text(AppFlowCopyKey);
    private string AppFlowSubtitle => Text(AppFlowSubtitleKey);
    private string AppFlowTitle => Text(AppFlowTitleKey);
    private string HelpSectionDescription => Text(HelpSectionDescriptionKey);
    private string HelpSectionTitle => Text(UiTextKey.SettingsNavHelp);
    private string LocalFilesCopy => Text(LocalFilesCopyKey);
    private string LocalFilesSubtitle => Text(LocalFilesSubtitleKey);
    private string LocalFilesTitle => Text(LocalFilesTitleKey);
    private string ModesCopy => Text(ModesCopyKey);
    private string ModesSubtitle => Text(ModesSubtitleKey);
    private string ModesTitle => Text(ModesTitleKey);
    private string TpsBasicsCopy => Text(TpsBasicsCopyKey);
    private string TpsBasicsSubtitle => Text(TpsBasicsSubtitleKey);
    private string TpsBasicsTitle => Text(TpsBasicsTitleKey);

    private string Text(string key) => Localizer[key];

    private string Text(UiTextKey key) => Localizer[key.ToString()];
}
