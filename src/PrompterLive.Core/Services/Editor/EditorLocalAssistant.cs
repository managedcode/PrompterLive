using System.Text.RegularExpressions;
using PrompterLive.Core.Models.Editor;

namespace PrompterLive.Core.Services.Editor;

public sealed class EditorLocalAssistant
{
    private const string ExpansionSentence = " This matters because the message should stay easy to follow.";
    private const string MarkerAlreadyPresent = "/";
    private const string PauseAfterComma = ", / ";
    private const string PauseAfterPeriod = ". // ";
    private const string PauseAfterSemicolon = "; / ";

    private static readonly IReadOnlyDictionary<string, string> SimplificationMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["approximately"] = "about",
            ["in order to"] = "to",
            ["transformative"] = "clear",
            ["utilize"] = "use"
        };

    public EditorTextMutationResult Apply(
        string? text,
        EditorSelectionRange selection,
        EditorAiAssistAction action)
    {
        var safeText = text ?? string.Empty;
        var range = ResolveTargetRange(selection, safeText.Length);
        var original = safeText[range.OrderedStart..range.OrderedEnd];
        var replacement = action switch
        {
            EditorAiAssistAction.Simplify => Simplify(original),
            EditorAiAssistAction.Expand => Expand(original),
            EditorAiAssistAction.AddDeliveryPauses => AddDeliveryPauses(original),
            _ => original
        };

        var updated = string.Concat(
            safeText[..range.OrderedStart],
            replacement,
            safeText[range.OrderedEnd..]);

        return new EditorTextMutationResult(
            updated,
            new EditorSelectionRange(range.OrderedStart, range.OrderedStart + replacement.Length));
    }

    private static string AddDeliveryPauses(string value)
    {
        var normalized = NormalizeWhitespace(value);
        if (normalized.Contains(MarkerAlreadyPresent, StringComparison.Ordinal))
        {
            return normalized;
        }

        return normalized
            .Replace(", ", PauseAfterComma, StringComparison.Ordinal)
            .Replace(". ", PauseAfterPeriod, StringComparison.Ordinal)
            .Replace("; ", PauseAfterSemicolon, StringComparison.Ordinal);
    }

    private static string Expand(string value)
    {
        var normalized = NormalizeWhitespace(value).TrimEnd();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return ExpansionSentence.TrimStart();
        }

        return normalized.EndsWith(".", StringComparison.Ordinal)
            ? string.Concat(normalized, ExpansionSentence)
            : string.Concat(normalized, ".", ExpansionSentence);
    }

    private static EditorSelectionRange ResolveTargetRange(EditorSelectionRange selection, int textLength)
    {
        if (selection.HasSelection)
        {
            var start = Math.Clamp(selection.OrderedStart, 0, textLength);
            var end = Math.Clamp(selection.OrderedEnd, 0, textLength);
            return new EditorSelectionRange(start, end);
        }

        return new EditorSelectionRange(0, textLength);
    }

    private static string Simplify(string value)
    {
        var simplified = NormalizeWhitespace(value);
        foreach (var replacement in SimplificationMap)
        {
            simplified = Regex.Replace(
                simplified,
                $@"\b{Regex.Escape(replacement.Key)}\b",
                replacement.Value,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        return simplified;
    }

    private static string NormalizeWhitespace(string value) =>
        Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim();
}
