using PrompterOne.Core.Models.Editor;

namespace PrompterOne.Shared.Components.Editor;

public partial class EditorSourcePanel
{
    private EditorSelectionViewModel _floatingBarAnchor = EditorSelectionViewModel.Empty;
    private string? _openFloatingMenuId;
    private string? _openMenuId;
    private bool _pendingFloatingBarReanchor = true;

    private EditorFloatingMenuDescriptor? OpenFloatingMenu =>
        FloatingMenus.FirstOrDefault(menu => string.Equals(menu.MenuId, _openFloatingMenuId, StringComparison.Ordinal));

    private Task ExecuteAiActionAsync(EditorAiAssistAction action)
    {
        PreserveFloatingBarAnchor();
        CloseToolbarPanels();
        return OnAiActionRequested.InvokeAsync(action);
    }

    private async Task ExecuteToolbarActionAsync(EditorToolbarActionDescriptor action)
    {
        if (GetActionDisabled(action))
        {
            return;
        }

        if (action.ActionType != EditorToolbarActionType.ToggleMenu)
        {
            await RefreshSelectionAsync(
                dismissMenus: false,
                requestComponentRender: false,
                preserveExistingSelectionWhenEmpty: true);
        }

        switch (action.ActionType)
        {
            case EditorToolbarActionType.ToggleMenu:
                ToggleMenu(action.MenuId ?? string.Empty);
                break;
            case EditorToolbarActionType.History:
                PreserveFloatingBarAnchor();
                CloseToolbarPanels();
                if (action.HistoryCommand is { } historyCommand)
                {
                    await RequestHistoryAsync(historyCommand);
                }

                break;
            case EditorToolbarActionType.Ai:
                PreserveFloatingBarAnchor();
                CloseToolbarPanels();
                if (action.AiAction is { } aiAction)
                {
                    await ExecuteAiActionAsync(aiAction);
                }

                break;
            default:
                PreserveFloatingBarAnchor();
                CloseToolbarPanels();
                if (action.Command is { } command)
                {
                    await OnCommandRequested.InvokeAsync(command);
                }

                break;
        }
    }

    private void CloseToolbarPanels()
    {
        _openMenuId = null;
        _openFloatingMenuId = null;
    }

    protected string FloatingBarStyle => BuildFloatingBarStyle(
        _floatingBarAnchor.HasSelection
            ? _floatingBarAnchor
            : Selection);

    private string GetToolbarSectionCss(EditorToolbarSectionDescriptor section)
    {
        if (!section.HasDropdown)
        {
            return "tb-section";
        }

        return IsMenuOpen(section.MenuId!)
            ? "tb-section tb-dropdown-wrap open"
            : "tb-section tb-dropdown-wrap";
    }

    private string? GetActionAriaExpanded(EditorToolbarActionDescriptor action) =>
        action.ActionType == EditorToolbarActionType.ToggleMenu && !string.IsNullOrWhiteSpace(action.MenuId)
            ? IsFloatingMenu(action.MenuId)
                ? IsFloatingMenuOpen(action.MenuId) ? "true" : "false"
                : IsMenuOpen(action.MenuId) ? "true" : "false"
            : null;

    private static string GetDropdownListCss(EditorToolbarSectionDescriptor section) =>
        string.Equals(section.Key, "color", StringComparison.Ordinal)
            ? "tb-color-grid"
            : "tb-emo-list";

    private static string GetToolbarDropdownCss(EditorToolbarSectionDescriptor section) =>
        string.IsNullOrWhiteSpace(section.DropdownPanelCssClass)
            ? "tb-dropdown"
            : $"tb-dropdown {section.DropdownPanelCssClass}";

    private static string GetFloatingMenuCss(EditorFloatingMenuDescriptor menu) =>
        string.IsNullOrWhiteSpace(menu.PanelCssClass)
            ? "efb-dropdown"
            : $"efb-dropdown {menu.PanelCssClass}";

    private bool HasOpenToolbarMenu => !string.IsNullOrWhiteSpace(_openMenuId);

    private bool ShouldRenderFloatingBar => CanRenderFloatingToolbar && _floatingBarAnchor.HasSelection && !HasOpenToolbarMenu && !_showFindBar;

    private bool GetActionDisabled(EditorToolbarActionDescriptor action) =>
        action.ActionType switch
        {
            EditorToolbarActionType.Ai => !_hasConfiguredAiProvider,
            _ when action.Command?.Kind == EditorCommandKind.Wrap && !CanApplyWrapCommands => true,
            _ => action.HistoryCommand switch
            {
                EditorHistoryCommand.Undo => !_visibleCanUndo,
                EditorHistoryCommand.Redo => !_visibleCanRedo,
                _ => false
            }
        };

    private bool IsMenuOpen(string menuId) =>
        string.Equals(_openMenuId, menuId, StringComparison.Ordinal);

    private bool IsFloatingMenuOpen(string menuId) =>
        string.Equals(_openFloatingMenuId, menuId, StringComparison.Ordinal);

    private bool IsFloatingMenu(string menuId) =>
        FloatingMenus.Any(menu => string.Equals(menu.MenuId, menuId, StringComparison.Ordinal));

    private void ToggleMenu(string menuId)
    {
        if (IsFloatingMenu(menuId))
        {
            _openMenuId = null;
            _openFloatingMenuId = IsFloatingMenuOpen(menuId) ? null : menuId;
            return;
        }

        _openFloatingMenuId = null;
        _openMenuId = IsMenuOpen(menuId) ? null : menuId;
    }

    private void PreserveFloatingBarAnchor()
    {
        _pendingFloatingBarReanchor = false;
    }

    private void RequestFloatingBarReanchor()
    {
        _pendingFloatingBarReanchor = true;
    }

    private void UpdateFloatingBarAnchor()
    {
        if (!Selection.HasSelection)
        {
            _floatingBarAnchor = EditorSelectionViewModel.Empty;
            _pendingFloatingBarReanchor = true;
            return;
        }

        if (!_floatingBarAnchor.HasSelection || _pendingFloatingBarReanchor)
        {
            _floatingBarAnchor = Selection;
            _pendingFloatingBarReanchor = false;
            return;
        }

        _floatingBarAnchor = _floatingBarAnchor with
        {
            Range = Selection.Range,
            Line = Selection.Line,
            Column = Selection.Column
        };
    }

    private static string BuildFloatingBarStyle(EditorSelectionViewModel selection) =>
        $"left:clamp({EditorSourcePanelStyleVariables.FloatingBarEdgePaddingExpression}, {selection.ToolbarLeft}px, calc(100% - {EditorSourcePanelStyleVariables.FloatingBarEdgePaddingExpression})); top:{selection.ToolbarTop}px; bottom:auto;";
}
