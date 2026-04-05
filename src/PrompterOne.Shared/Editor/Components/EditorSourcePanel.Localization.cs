using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using PrompterOne.Shared.Localization;

namespace PrompterOne.Shared.Components.Editor;

public partial class EditorSourcePanel
{
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

    private string StatusSegmentsLabel => LF(UiTextKey.EditorStatusSegmentsFormat, Status.SegmentCount);

    private string StatusVersionLabel => LF(UiTextKey.EditorStatusVersionFormat, Status.Version);

    private string StatusWordCountLabel => LF(UiTextKey.EditorStatusWordsFormat, Status.WordCount);

    private IReadOnlyList<EditorToolbarSectionDescriptor> ToolbarSections => _toolbarSections;

    private string LF(UiTextKey key, params object[] args) => Localizer[key.ToString(), args];

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
