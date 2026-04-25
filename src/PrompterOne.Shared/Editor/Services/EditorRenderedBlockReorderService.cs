using PrompterOne.Core.Models.Editor;
using PrompterOne.Shared.Components.Editor;

namespace PrompterOne.Shared.Services.Editor;

public sealed record EditorRenderedBlockReorderResult(string Text, EditorSelectionRange Selection);

public static class EditorRenderedBlockReorderService
{
    public static EditorRenderedBlockReorderResult? Reorder(
        string source,
        IReadOnlyList<EditorOutlineSegmentViewModel> segments,
        EditorRenderedBlockReorderRequest request)
    {
        var blocks = FlattenBlocks(segments);
        var sourceBlock = blocks.FirstOrDefault(block =>
            block.SegmentIndex == request.SourceSegmentIndex &&
            block.BlockIndex == request.SourceBlockIndex);
        var targetBlock = blocks.FirstOrDefault(block =>
            block.SegmentIndex == request.TargetSegmentIndex &&
            block.BlockIndex == request.TargetBlockIndex);

        if (sourceBlock is null || targetBlock is null || sourceBlock.Order == targetBlock.Order)
        {
            return null;
        }

        var sourceRange = ResolveExclusiveRange(source, sourceBlock);
        var targetRange = ResolveExclusiveRange(source, targetBlock);
        if (sourceRange.Start == sourceRange.End || targetRange.Start == targetRange.End)
        {
            return null;
        }

        var movingText = source[sourceRange.Start..sourceRange.End];
        var remainingText = source.Remove(sourceRange.Start, sourceRange.End - sourceRange.Start);
        var rawInsertionIndex = request.InsertAfterTarget ? targetRange.End : targetRange.Start;
        var insertionIndex = rawInsertionIndex > sourceRange.Start
            ? rawInsertionIndex - movingText.Length
            : rawInsertionIndex;
        insertionIndex = Math.Clamp(insertionIndex, 0, remainingText.Length);

        var nextText = remainingText.Insert(insertionIndex, movingText);
        var selection = new EditorSelectionRange(insertionIndex, insertionIndex);
        return string.Equals(source, nextText, StringComparison.Ordinal)
            ? null
            : new EditorRenderedBlockReorderResult(nextText, selection);
    }

    private static IReadOnlyList<RenderedBlockRange> FlattenBlocks(
        IReadOnlyList<EditorOutlineSegmentViewModel> segments) =>
        segments
            .SelectMany(segment => segment.Blocks.Select(block => new RenderedBlockRange(
                segment.Index,
                block.Index,
                block.StartIndex,
                block.EndIndex)))
            .OrderBy(block => block.StartIndex)
            .Select((block, order) => block with { Order = order })
            .ToList();

    private static (int Start, int End) ResolveExclusiveRange(string source, RenderedBlockRange block)
    {
        if (string.IsNullOrEmpty(source))
        {
            return (0, 0);
        }

        var start = Math.Clamp(block.StartIndex, 0, source.Length);
        var end = Math.Clamp(block.EndIndex + 1, start, source.Length);
        return (start, end);
    }

    private sealed record RenderedBlockRange(
        int SegmentIndex,
        int BlockIndex,
        int StartIndex,
        int EndIndex)
    {
        public int Order { get; init; }
    }
}
