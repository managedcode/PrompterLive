using PrompterOne.Core.AI.Models;
using PrompterOne.Core.AI.Services;

namespace PrompterOne.Shared.Services;

internal static class AiSpotlightApprovalBuilder
{
    public static bool TryCreate(
        AiSpotlightState state,
        ScriptDocumentEditService documentEditService,
        out AiSpotlightApprovalRequest? approvalRequest)
    {
        approvalRequest = null;
        var editor = state.Context.Editor;
        if (editor?.SelectedRange is not { } range ||
            string.IsNullOrWhiteSpace(editor.Content) ||
            !IsMutationPrompt(state.Prompt))
        {
            return false;
        }

        string currentText;
        try
        {
            currentText = documentEditService.ReadRange(editor.Content, range);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(currentText))
        {
            return false;
        }

        var proposedText = CreateDeterministicReplacement(state.Prompt, currentText);
        if (string.Equals(currentText, proposedText, StringComparison.Ordinal))
        {
            return false;
        }

        var revision = editor.Revision ?? ScriptDocumentRevision.Create(editor.Content);
        var plan = new ScriptDocumentEditPlan(
            revision,
            [ScriptDocumentEditOperation.Replace(range, proposedText)],
            editor.DocumentId);

        approvalRequest = new AiSpotlightApprovalRequest(
            "Replace only the selected editor range; the rest of the script stays untouched.",
            plan,
            range,
            ScriptDocumentPosition.FromOffset(editor.Content, range.Start),
            ScriptDocumentPosition.FromOffset(editor.Content, range.End),
            currentText,
            proposedText);
        return true;
    }

    private static string CreateDeterministicReplacement(string prompt, string currentText)
    {
        var tag = ContainsAny(prompt, "tps", "тпс", "format", "convert", "конверт")
            ? "focused"
            : "warm";
        var trimmed = currentText.Trim();
        if (trimmed.StartsWith($"[{tag}]", StringComparison.OrdinalIgnoreCase))
        {
            return currentText;
        }

        var leadingLength = currentText.Length - currentText.TrimStart().Length;
        var trailingLength = currentText.Length - currentText.TrimEnd().Length;
        var leading = leadingLength == 0 ? string.Empty : currentText[..leadingLength];
        var trailing = trailingLength == 0 ? string.Empty : currentText[^trailingLength..];
        return $"{leading}[{tag}]{trimmed}[/{tag}]{trailing}";
    }

    private static bool IsMutationPrompt(string prompt) =>
        ContainsAny(
            prompt,
            "rewrite",
            "revise",
            "edit",
            "replace",
            "convert",
            "tps",
            "переп",
            "змін",
            "замін",
            "конверт",
            "покращ",
            "виправ",
            "тпс");

    private static bool ContainsAny(string value, params string[] needles)
    {
        foreach (var needle in needles)
        {
            if (value.Contains(needle, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
