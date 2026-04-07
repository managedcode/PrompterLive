using Microsoft.AspNetCore.Components.Web;
using PrompterOne.Shared.Contracts;

namespace PrompterOne.Shared.Pages;

public partial class TeleprompterPage
{
    private readonly HashSet<string> _visibleRailTooltips = [];
    private readonly HashSet<string> _suppressedRailTooltips = [];

    private void HandleRailTooltipFocusIn(string key)
    {
        if (_suppressedRailTooltips.Contains(key))
        {
            return;
        }

        _visibleRailTooltips.Add(key);
    }

    private void HandleRailTooltipFocusOut(string key) => _visibleRailTooltips.Remove(key);

    private void HandleRailTooltipKeyDown(string key, KeyboardEventArgs args)
    {
        var normalizedKey = UiKeyboardKeys.Normalize(args.Key);
        if (string.Equals(normalizedKey, UiKeyboardKeys.Enter, StringComparison.Ordinal) ||
            string.Equals(normalizedKey, UiKeyboardKeys.Escape, StringComparison.Ordinal) ||
            string.Equals(normalizedKey, UiKeyboardKeys.Space, StringComparison.Ordinal))
        {
            _visibleRailTooltips.Remove(key);
        }
    }

    private void HandleRailTooltipPointerDown(string key)
    {
        _visibleRailTooltips.Remove(key);
        _suppressedRailTooltips.Add(key);
    }

    private void HandleRailTooltipPointerEnter(string key)
    {
        if (_suppressedRailTooltips.Contains(key))
        {
            return;
        }

        _visibleRailTooltips.Add(key);
    }

    private void HandleRailTooltipPointerLeave(string key)
    {
        _visibleRailTooltips.Remove(key);
        _suppressedRailTooltips.Remove(key);
    }

    private bool IsRailTooltipVisible(string key) => _visibleRailTooltips.Contains(key);
}
