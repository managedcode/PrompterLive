using System.Globalization;
using System.Text.RegularExpressions;
using PrompterOne.Core.Models.CompiledScript;
using PrompterOne.Core.Models.Editor;
using PrompterOne.Shared.Components.Editor;
using PrompterOne.Shared.Services.Editor;

namespace PrompterOne.Shared.Pages;

public partial class EditorPage
{
    private static readonly Regex RenderedCarriageReturnRegex = new(@"\r\n?", RegexOptions.Compiled);
    private static readonly Regex RenderedExcessBlankLineRegex = new(@"\n{3,}", RegexOptions.Compiled);
    private static readonly Regex RenderedSourceHeadingRegex = new(@"^\s{0,3}#{1,6}\s*\[[^\r\n]+\]\s*$", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex RenderedSourceInlineTagRegex = new(@"\[[^\]\r\n]+\]", RegexOptions.Compiled);
    private static readonly Regex RenderedWhitespaceBeforePunctuationRegex = new(@"\s+([,.!?;:])", RegexOptions.Compiled);

    private IReadOnlyList<EditorRenderedSegmentViewModel> BuildRenderedSegments()
    {
        if (_segments.Count == 0)
        {
            return [];
        }

        var compiledSegments = SessionService.State.CompiledScript?.Segments;
        return _segments
            .Select(segment =>
            {
                var compiledSegment = ElementAtOrNull(compiledSegments, segment.Index);
                return new EditorRenderedSegmentViewModel(
                    segment.Index,
                    FormatRenderedCardNumber(segment.Index + 1),
                    segment.Name,
                    segment.EmotionLabel,
                    segment.TargetWpmLabel,
                    segment.DurationLabel,
                    segment.Blocks
                        .Select(block =>
                        {
                            var compiledBlock = ElementAtOrNull(compiledSegment?.Blocks, block.Index);
                            return new EditorRenderedBlockViewModel(
                                segment.Index,
                                block.Index,
                                $"{segment.Index + 1}.{block.Index + 1}",
                                block.Name,
                                block.EmotionLabel,
                                block.TargetWpmLabel,
                                BuildBlockDisplayText(block, compiledBlock));
                        })
                        .ToList());
            })
            .ToList();
    }

    private string BuildRenderedFallbackText() =>
        BuildSourceDisplayText(_sourceText);

    private async Task OnRenderedTextChangedAsync(EditorRenderedBlockTextChange change)
    {
        var replacementText = NormalizeRenderedInput(change.Text);
        var block = FindRenderedBlock(change);
        if (block is null)
        {
            var fallbackCaret = Math.Clamp(replacementText.Length, 0, replacementText.Length);
            await ApplyMutationAsync(replacementText, new EditorSelectionRange(fallbackCaret, fallbackCaret));
            return;
        }

        var range = ResolveBlockContentRange(_sourceText, block);
        var insertion = BuildRenderedBlockInsertion(_sourceText, range.Start, replacementText);
        var nextText = string.Concat(
            _sourceText.AsSpan(0, range.Start),
            insertion,
            _sourceText.AsSpan(range.End));
        var caret = Math.Clamp(range.Start + insertion.Length, 0, nextText.Length);
        await ApplyMutationAsync(nextText, new EditorSelectionRange(caret, caret));
    }

    private async Task OnRenderedBlockReorderRequestedAsync(EditorRenderedBlockReorderRequest request)
    {
        var result = EditorRenderedBlockReorderService.Reorder(_sourceText, _segments, request);
        if (result is null)
        {
            return;
        }

        await ApplyMutationAsync(result.Text, result.Selection);
    }

    private EditorOutlineBlockViewModel? FindRenderedBlock(EditorRenderedBlockTextChange change) =>
        _segments
            .FirstOrDefault(segment => segment.Index == change.SegmentIndex)
            ?.Blocks
            .FirstOrDefault(block => block.Index == change.BlockIndex);

    private string BuildBlockDisplayText(EditorOutlineBlockViewModel block, CompiledBlock? compiledBlock)
    {
        var compiledText = BuildCompiledBlockText(compiledBlock);
        return string.IsNullOrWhiteSpace(compiledText)
            ? BuildSourceDisplayText(ExtractBlockSourceContent(_sourceText, block))
            : compiledText;
    }

    private static string BuildCompiledBlockText(CompiledBlock? block)
    {
        if (block?.Words is not { Count: > 0 } words)
        {
            return string.Empty;
        }

        var text = string.Join(
            ' ',
            words
                .Where(static word =>
                    word.Metadata is not { IsPause: true } &&
                    word.Metadata is not { IsEditPoint: true } &&
                    !string.IsNullOrWhiteSpace(word.CleanText))
                .Select(static word => word.CleanText.Trim()));

        return CleanRenderedText(text);
    }

    private static string ExtractBlockSourceContent(string source, EditorOutlineBlockViewModel block)
    {
        var range = ResolveBlockContentRange(source, block);
        return range.End <= range.Start
            ? string.Empty
            : source[range.Start..range.End];
    }

    private static (int Start, int End) ResolveBlockContentRange(string source, EditorOutlineBlockViewModel block)
    {
        if (string.IsNullOrEmpty(source))
        {
            return (0, 0);
        }

        var safeStart = Math.Clamp(block.StartIndex, 0, source.Length);
        var safeEndExclusive = Math.Clamp(block.EndIndex + 1, safeStart, source.Length);
        if (safeEndExclusive <= safeStart)
        {
            return (safeStart, safeStart);
        }

        var newlineIndex = source.IndexOf('\n', safeStart, safeEndExclusive - safeStart);
        var contentStart = newlineIndex >= 0 ? newlineIndex + 1 : safeEndExclusive;
        while (contentStart < safeEndExclusive && (source[contentStart] == '\r' || source[contentStart] == '\n'))
        {
            contentStart++;
        }

        var contentEnd = safeEndExclusive;
        while (contentEnd > contentStart && char.IsWhiteSpace(source[contentEnd - 1]))
        {
            contentEnd--;
        }

        return (contentStart, contentEnd);
    }

    private static string BuildSourceDisplayText(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return string.Empty;
        }

        var normalized = RenderedCarriageReturnRegex.Replace(source, "\n");
        var withoutHeadings = RenderedSourceHeadingRegex.Replace(normalized, string.Empty);
        var withoutInlineTags = RenderedSourceInlineTagRegex.Replace(withoutHeadings, string.Empty);
        return CleanRenderedText(withoutInlineTags);
    }

    private static string NormalizeRenderedInput(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var normalized = RenderedCarriageReturnRegex.Replace(text, "\n").Trim();
        return RenderedExcessBlankLineRegex.Replace(normalized, "\n\n");
    }

    private static string BuildRenderedBlockInsertion(string source, int start, string replacementText)
    {
        if (string.IsNullOrEmpty(replacementText) || start <= 0 || start > source.Length || source[start - 1] == '\n')
        {
            return replacementText;
        }

        return "\n" + replacementText;
    }

    private static string CleanRenderedText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var normalized = RenderedCarriageReturnRegex.Replace(text, "\n");
        normalized = RenderedWhitespaceBeforePunctuationRegex.Replace(normalized, "$1");
        normalized = RenderedExcessBlankLineRegex.Replace(normalized, "\n\n");
        return normalized.Trim();
    }

    private static string FormatRenderedCardNumber(int number) =>
        number.ToString(CultureInfo.InvariantCulture);

    private static T? ElementAtOrNull<T>(IReadOnlyList<T>? values, int index) where T : class =>
        values is not null && index >= 0 && index < values.Count
            ? values[index]
            : null;
}
