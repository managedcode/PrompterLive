using PrompterLive.Core.Models.Editor;

namespace PrompterLive.Shared.Components.Editor;

public partial class EditorSourcePanel
{
    private const string FormatMenuId = "format";
    private const string ColorMenuId = "color";
    private const string EmotionMenuId = "emotion";
    private const string PauseMenuId = "pause";
    private const string SpeedMenuId = "speed";
    private const string InsertMenuId = "insert";

    private bool _isAiPanelOpen;
    private string? _openMenuId;

    private Task ExecuteAiActionAsync(EditorAiAssistAction action)
    {
        CloseToolbarPanels();
        return OnAiActionRequested.InvokeAsync(action);
    }

    private async Task ExecuteInsertCommandAsync(string token, int? caretOffset = null)
    {
        CloseToolbarPanels();
        await RequestInsertAsync(token, caretOffset);
    }

    private async Task ExecuteWrapCommandAsync(string openingToken, string closingToken, string placeholder = "text")
    {
        CloseToolbarPanels();
        await RequestWrapAsync(openingToken, closingToken, placeholder);
    }

    private void CloseToolbarPanels()
    {
        _isAiPanelOpen = false;
        _openMenuId = null;
    }

    private string GetToolbarSectionCss(string menuId) =>
        IsMenuOpen(menuId)
            ? "tb-section tb-dropdown-wrap open"
            : "tb-section tb-dropdown-wrap";

    private bool IsMenuOpen(string menuId) =>
        string.Equals(_openMenuId, menuId, StringComparison.Ordinal);

    private void ToggleAiPanel()
    {
        _isAiPanelOpen = !_isAiPanelOpen;
        _openMenuId = null;
    }

    private void ToggleMenu(string menuId)
    {
        _isAiPanelOpen = false;
        _openMenuId = IsMenuOpen(menuId) ? null : menuId;
    }
}
