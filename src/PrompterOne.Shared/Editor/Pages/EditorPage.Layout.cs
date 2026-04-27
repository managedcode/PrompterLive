using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using PrompterOne.Shared.Localization;

namespace PrompterOne.Shared.Pages;

public partial class EditorPage
{
    [Inject] private IStringLocalizer<SharedResource> Localizer { get; set; } = null!;

    private bool _isMetadataRailCollapsed;
    private bool _isStructureSidebarCollapsed;

    private Task OnMetadataRailToggleAsync()
    {
        _isMetadataRailCollapsed = !_isMetadataRailCollapsed;
        return Task.CompletedTask;
    }

    private Task OnStructureSidebarToggleAsync()
    {
        _isStructureSidebarCollapsed = !_isStructureSidebarCollapsed;
        return Task.CompletedTask;
    }

    private string GetMetadataRailToggleLabel() =>
        _isMetadataRailCollapsed
            ? Localizer[UiTextKey.EditorMetadataToggleExpand.ToString()]
            : Localizer[UiTextKey.EditorMetadataToggleCollapse.ToString()];

    private string GetStructureSidebarToggleLabel() =>
        _isStructureSidebarCollapsed
            ? Localizer[UiTextKey.LibraryOpenSidebar.ToString()]
            : Localizer[UiTextKey.LibraryCloseSidebar.ToString()];
}
