using PrompterOne.Core.AI.Models;

namespace PrompterOne.Core.AI.Services;

public sealed class ScriptDocumentEditService
{
    public string ReadRange(string? text, ScriptDocumentRange range)
    {
        var safeText = text ?? string.Empty;
        range.ValidateWithin(safeText.Length);
        return safeText[range.Start..range.End];
    }

    public ScriptDocumentEditResult Apply(string? text, ScriptDocumentEditPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var safeText = text ?? string.Empty;
        EnsureCurrentRevision(safeText, plan.Revision);
        var operations = ValidateMutatingOperations(safeText, plan.Operations);
        var updated = safeText;
        var applied = new List<ScriptDocumentAppliedEdit>(operations.Length);

        foreach (var operation in operations.OrderByDescending(static operation => operation.Range.Start))
        {
            var replacement = operation.Kind == ScriptDocumentEditKind.Delete
                ? string.Empty
                : operation.Text ?? string.Empty;
            updated = string.Concat(updated[..operation.Range.Start], replacement, updated[operation.Range.End..]);
            applied.Add(CreateAppliedEdit(operation, replacement));
        }

        applied.Reverse();
        return new ScriptDocumentEditResult(updated, ScriptDocumentRevision.Create(updated), applied);
    }

    private static ScriptDocumentAppliedEdit CreateAppliedEdit(ScriptDocumentEditOperation operation, string replacement)
    {
        var updatedEnd = operation.Range.Start + replacement.Length;
        return new ScriptDocumentAppliedEdit(
            operation.Kind,
            operation.Range,
            new ScriptDocumentRange(operation.Range.Start, updatedEnd));
    }

    private static void EnsureCurrentRevision(string text, ScriptDocumentRevision expectedRevision)
    {
        var currentRevision = ScriptDocumentRevision.Create(text);
        if (currentRevision != expectedRevision)
        {
            throw new InvalidOperationException("The script document changed before the edit plan was applied.");
        }
    }

    private static ScriptDocumentEditOperation[] ValidateMutatingOperations(
        string text,
        IReadOnlyList<ScriptDocumentEditOperation> operations)
    {
        var mutatingOperations = operations
            .Where(static operation => operation.Kind != ScriptDocumentEditKind.Read)
            .OrderBy(static operation => operation.Range.Start)
            .ToArray();

        foreach (var operation in mutatingOperations)
        {
            ValidateOperation(text, operation);
        }

        EnsureNonOverlapping(mutatingOperations);
        return mutatingOperations;
    }

    private static void ValidateOperation(string text, ScriptDocumentEditOperation operation)
    {
        operation.Range.ValidateWithin(text.Length);

        if (operation.Kind == ScriptDocumentEditKind.Insert && !operation.Range.IsEmpty)
        {
            throw new InvalidOperationException("Insert operations must use an empty range.");
        }

        if (operation.Kind is ScriptDocumentEditKind.Replace or ScriptDocumentEditKind.Delete &&
            operation.Range.IsEmpty)
        {
            throw new InvalidOperationException($"{operation.Kind} operations must use a non-empty range.");
        }
    }

    private static void EnsureNonOverlapping(IReadOnlyList<ScriptDocumentEditOperation> operations)
    {
        for (var index = 1; index < operations.Count; index++)
        {
            var previous = operations[index - 1];
            var current = operations[index];
            if (previous.Range.End > current.Range.Start)
            {
                throw new InvalidOperationException("Edit plan contains overlapping ranges.");
            }
        }
    }
}
