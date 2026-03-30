using PrompterLive.Core.Models.Editor;

namespace PrompterLive.Shared.Components.Editor;

public partial class EditorSourcePanel
{
    private EditorSelectionViewModel _floatingBarAnchor = EditorSelectionViewModel.Empty;
    private string? _openMenuId;
    private bool _pendingFloatingBarReanchor = true;

    private static IReadOnlyList<EditorToolbarSectionDescriptor> ToolbarSections => EditorToolbarCatalog.Sections;

    private static IReadOnlyList<IReadOnlyList<EditorToolbarActionDescriptor>> FloatingActionGroups => EditorToolbarCatalog.FloatingActionGroups;

    private Task ExecuteAiActionAsync(EditorAiAssistAction action)
    {
        PreserveFloatingBarAnchor();
        CloseToolbarPanels();
        return OnAiActionRequested.InvokeAsync(action);
    }

    private async Task ExecuteToolbarActionAsync(EditorToolbarActionDescriptor action)
    {
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
            ? IsMenuOpen(action.MenuId) ? "true" : "false"
            : null;

    private static string GetDropdownListCss(EditorToolbarSectionDescriptor section) =>
        string.Equals(section.Key, "color", StringComparison.Ordinal)
            ? "tb-color-grid"
            : "tb-emo-list";

    private bool HasOpenToolbarMenu => !string.IsNullOrWhiteSpace(_openMenuId);

    private bool ShouldRenderFloatingBar => _floatingBarAnchor.HasSelection && !HasOpenToolbarMenu;

    private bool GetActionDisabled(EditorToolbarActionDescriptor action) =>
        action.HistoryCommand switch
        {
            EditorHistoryCommand.Undo => !CanUndo,
            EditorHistoryCommand.Redo => !CanRedo,
            _ => false
        };

    private bool IsMenuOpen(string menuId) =>
        string.Equals(_openMenuId, menuId, StringComparison.Ordinal);

    private void ToggleMenu(string menuId)
    {
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
