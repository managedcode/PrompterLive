using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using PrompterOne.Shared.Localization;

namespace PrompterOne.Shared.Components.Editor;

public partial class EditorSourcePanel
{
    private const string ApproximateValuePrefix = "~";
    private IReadOnlyList<IReadOnlyList<EditorToolbarActionDescriptor>> _floatingActionGroups = [];
    private IReadOnlyList<EditorFloatingMenuDescriptor> _floatingMenus = [];
    private IReadOnlyList<EditorToolbarSectionDescriptor> _toolbarSections = [];
    private string? _toolbarCultureName;

    [Inject] private IStringLocalizer<SharedResource> Localizer { get; set; } = null!;

    private IReadOnlyList<IReadOnlyList<EditorToolbarActionDescriptor>> FloatingActionGroups => _floatingActionGroups;

    private IReadOnlyList<EditorFloatingMenuDescriptor> FloatingMenus => _floatingMenus;

    private string PlaceholderText => L(UiTextKey.EditorPlaceholder);

    private string StatusLineShortLabel => L(UiTextKey.EditorStatusLineShort);

    private string StatusColumnShortLabel => L(UiTextKey.EditorStatusColumnShort);

    private string StatusProfileLabel => L(UiTextKey.CommonProfile);

    private string StatusBaseWpmLabel => L(UiTextKey.CommonBaseWpm);

    private string StatusBaseWpmValue => Status.BaseWpm.ToString(CultureInfo.CurrentCulture);

    private string StatusLineValue => Status.Line.ToString(CultureInfo.CurrentCulture);

    private string StatusColumnValue => Status.Column.ToString(CultureInfo.CurrentCulture);

    private string StatusSegmentsLabel => L(UiTextKey.CommonSegments);

    private string StatusSegmentsValue => Status.SegmentCount.ToString(CultureInfo.CurrentCulture);

    private string StatusWordCountLabel => L(UiTextKey.CommonWords);

    private string StatusWordCountValue => string.Concat(ApproximateValuePrefix, Status.WordCount.ToString(CultureInfo.CurrentCulture));

    private string StatusDurationLabel => L(UiTextKey.CommonDuration);

    private string StatusDurationValue => string.Concat(ApproximateValuePrefix, Status.Duration);

    private string StatusVersionLabel => L(UiTextKey.CommonVersion);

    private string StatusVersionValue => Status.Version;

    private IReadOnlyList<EditorToolbarSectionDescriptor> ToolbarSections => _toolbarSections;

    private string L(UiTextKey key) => Localizer[key.ToString()];

    private void EnsureToolbarCatalogs()
    {
        var cultureName = CultureInfo.CurrentUICulture.Name;
        if (string.Equals(_toolbarCultureName, cultureName, StringComparison.Ordinal))
        {
            return;
        }

        _toolbarSections = EditorToolbarCatalog.BuildSections(Localizer);
        _floatingMenus = EditorFloatingToolbarCatalog.BuildMenus(Localizer);
        _floatingActionGroups = EditorFloatingToolbarCatalog.BuildActionGroups(Localizer);
        _toolbarCultureName = cultureName;
    }
}
