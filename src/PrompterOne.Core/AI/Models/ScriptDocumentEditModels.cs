using System.Security.Cryptography;
using System.Text;

namespace PrompterOne.Core.AI.Models;

public readonly record struct ScriptDocumentRevision(string Value)
{
    public static ScriptDocumentRevision Empty => Create(string.Empty);

    public static ScriptDocumentRevision Create(string? text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text ?? string.Empty));
        return new ScriptDocumentRevision(Convert.ToHexString(bytes));
    }
}

public readonly record struct ScriptDocumentPosition(int Line, int Column, int Offset)
{
    public static ScriptDocumentPosition FromOffset(string? text, int offset)
    {
        var safeText = text ?? string.Empty;
        var clampedOffset = Math.Clamp(offset, 0, safeText.Length);
        var line = 1;
        var lineStart = 0;

        for (var index = 0; index < clampedOffset; index++)
        {
            if (safeText[index] != '\n')
            {
                continue;
            }

            line++;
            lineStart = index + 1;
        }

        return new ScriptDocumentPosition(line, clampedOffset - lineStart + 1, clampedOffset);
    }
}

public readonly record struct ScriptDocumentRange(int Start, int End)
{
    public int Length => End - Start;

    public bool IsEmpty => Length == 0;

    public void ValidateWithin(int textLength)
    {
        if (Start < 0 || End < Start || End > textLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(textLength),
                $"Range [{Start}, {End}) is outside a document with length {textLength}.");
        }
    }
}

public enum ScriptDocumentEditKind
{
    Read,
    Insert,
    Replace,
    Delete,
}

public sealed record ScriptDocumentEditOperation(
    ScriptDocumentEditKind Kind,
    ScriptDocumentRange Range,
    string? Text = null)
{
    public static ScriptDocumentEditOperation Read(ScriptDocumentRange range) =>
        new(ScriptDocumentEditKind.Read, range);

    public static ScriptDocumentEditOperation Insert(int offset, string text) =>
        new(ScriptDocumentEditKind.Insert, new ScriptDocumentRange(offset, offset), text);

    public static ScriptDocumentEditOperation Replace(ScriptDocumentRange range, string text) =>
        new(ScriptDocumentEditKind.Replace, range, text);

    public static ScriptDocumentEditOperation Delete(ScriptDocumentRange range) =>
        new(ScriptDocumentEditKind.Delete, range);
}

public sealed record ScriptDocumentEditPlan(
    ScriptDocumentRevision Revision,
    IReadOnlyList<ScriptDocumentEditOperation> Operations,
    string? DocumentId = null);

public sealed record ScriptDocumentAppliedEdit(
    ScriptDocumentEditKind Kind,
    ScriptDocumentRange OriginalRange,
    ScriptDocumentRange UpdatedRange);

public sealed record ScriptDocumentEditResult(
    string Text,
    ScriptDocumentRevision Revision,
    IReadOnlyList<ScriptDocumentAppliedEdit> AppliedEdits);
