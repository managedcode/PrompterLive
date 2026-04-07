using System.Globalization;
using Microsoft.AspNetCore.Components;
using PrompterOne.Shared.Localization;

namespace PrompterOne.Shared.Components.Editor;

public partial class EditorSourcePanel
{
    private string _findQuery = string.Empty;
    private int _findMatchIndex = -1;
    private IReadOnlyList<EditorFindMatch> _findMatches = [];
    private bool _showFindBar;

    private bool CanNavigateFindMatches => _findMatches.Count > 0;

    private string FindResultLabel => _findMatches.Count == 0
        ? L(UiTextKey.EditorFindNoResults)
        : string.Format(CultureInfo.CurrentCulture, L(UiTextKey.EditorFindResultsFormat), _findMatchIndex + 1, _findMatches.Count);

    private bool ShouldRenderFindResult => !string.IsNullOrWhiteSpace(_findQuery);

    private Task ToggleFindBarAsync()
    {
        _showFindBar = !_showFindBar;
        CloseToolbarPanels();

        if (_showFindBar)
        {
            RefreshFindMatchesForCurrentText();
        }
        else
        {
            ClearFindState();
        }

        return InvokeAsync(StateHasChanged);
    }

    private Task CloseFindBarAsync()
    {
        _showFindBar = false;
        ClearFindState();
        return InvokeAsync(StateHasChanged);
    }

    private async Task OnFindQueryInputAsync(ChangeEventArgs args)
    {
        _findQuery = args.Value?.ToString() ?? string.Empty;
        RefreshFindMatchesForCurrentText();
        await FocusActiveFindMatchAsync(focusEditor: false);
        await InvokeAsync(StateHasChanged);
    }

    private Task NavigateToPreviousFindMatchAsync() =>
        NavigateFindMatchAsync(-1);

    private Task NavigateToNextFindMatchAsync() =>
        NavigateFindMatchAsync(1);

    private void RefreshFindMatchesForCurrentText()
    {
        if (!_showFindBar || string.IsNullOrWhiteSpace(_findQuery))
        {
            _findMatches = [];
            _findMatchIndex = -1;
            return;
        }

        var matches = new List<EditorFindMatch>();
        var searchStart = 0;

        while (searchStart < Text.Length)
        {
            var matchStart = Text.IndexOf(_findQuery, searchStart, StringComparison.OrdinalIgnoreCase);
            if (matchStart < 0)
            {
                break;
            }

            matches.Add(new EditorFindMatch(matchStart, _findQuery.Length));
            searchStart = matchStart + Math.Max(_findQuery.Length, 1);
        }

        _findMatches = matches;
        _findMatchIndex = matches.Count > 0 ? Math.Clamp(_findMatchIndex, 0, matches.Count - 1) : -1;
        if (_findMatchIndex < 0 && matches.Count > 0)
        {
            _findMatchIndex = 0;
        }
    }

    private async Task NavigateFindMatchAsync(int direction)
    {
        if (_findMatches.Count == 0)
        {
            return;
        }

        var nextIndex = (_findMatchIndex + direction + _findMatches.Count) % _findMatches.Count;
        _findMatchIndex = nextIndex;
        await FocusActiveFindMatchAsync(focusEditor: false);
        await InvokeAsync(StateHasChanged);
    }

    private async Task FocusActiveFindMatchAsync(bool focusEditor)
    {
        if (_findMatchIndex < 0 || _findMatchIndex >= _findMatches.Count)
        {
            return;
        }

        var activeMatch = _findMatches[_findMatchIndex];
        await FocusRangeAsync(activeMatch.Start, activeMatch.End, focusEditor: focusEditor);
    }

    private void ClearFindState()
    {
        _findQuery = string.Empty;
        _findMatches = [];
        _findMatchIndex = -1;
    }

    private readonly record struct EditorFindMatch(int Start, int Length)
    {
        public int End => Start + Length;
    }
}
