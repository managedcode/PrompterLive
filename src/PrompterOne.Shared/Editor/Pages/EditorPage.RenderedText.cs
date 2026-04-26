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
        var attachmentsByBlock = _blockAttachments
            .GroupBy(attachment => attachment.BlockKey, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(MapAttachment).ToList(),
                StringComparer.Ordinal);

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
                    BuildRenderedSegmentCues(segment),
                    segment.Blocks
                        .Select(block =>
                        {
                            var compiledBlock = ElementAtOrNull(compiledSegment?.Blocks, block.Index);
                            var blockKey = EditorBlockAttachmentKeyBuilder.Build(
                                _sourceText,
                                block.StartIndex,
                                block.EndIndex,
                                segment.Index,
                                block.Index,
                                block.Name);
                            return new EditorRenderedBlockViewModel(
                                segment.Index,
                                block.Index,
                                blockKey,
                                $"{segment.Index + 1}.{block.Index + 1}",
                                block.Name,
                                block.EmotionLabel,
                                block.TargetWpmLabel,
                                BuildBlockDisplayText(block, compiledBlock),
                                BuildRenderedBlockCues(block, compiledBlock),
                                attachmentsByBlock.GetValueOrDefault(blockKey) ?? []);
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

    private static IReadOnlyList<EditorRenderedCueViewModel> BuildRenderedSegmentCues(EditorOutlineSegmentViewModel segment)
    {
        var cues = new List<EditorRenderedCueViewModel>();
        AddCue(cues, "emotion", "●", segment.EmotionLabel, ResolveCueTone(segment.EmotionLabel));
        AddCue(cues, "pace", "×", segment.TargetWpmLabel, "pace");
        AddCue(cues, "timing", "⌁", segment.DurationLabel, "timing");
        return cues;
    }

    private static IReadOnlyList<EditorRenderedCueViewModel> BuildRenderedBlockCues(
        EditorOutlineBlockViewModel block,
        CompiledBlock? compiledBlock)
    {
        var cues = new List<EditorRenderedCueViewModel>();
        AddCue(cues, "emotion", "●", block.EmotionLabel, ResolveCueTone(block.EmotionLabel));
        AddCue(cues, "pace", "×", block.TargetWpmLabel, "pace");

        if (compiledBlock?.Words is not { Count: > 0 } words)
        {
            return cues;
        }

        foreach (var metadata in words.Select(static word => word.Metadata))
        {
            if (metadata.IsBreath)
            {
                AddCue(cues, "pause", "⏸", "Breath", "pause");
            }

            if (metadata.IsPause)
            {
                AddCue(cues, "pause", "⏸", FormatDurationCue(metadata.PauseDuration), "pause");
            }

            if (metadata.SpeedMultiplier is { } multiplier)
            {
                AddCue(cues, "pace", "×", FormattableString.Invariant($"x{multiplier:0.##}"), "pace");
            }

            if (metadata.SpeedOverride is { } speedOverride)
            {
                AddCue(cues, "pace", "×", FormattableString.Invariant($"{speedOverride}WPM"), "pace");
            }

            AddCue(cues, "emotion", "●", metadata.InlineEmotionHint ?? metadata.EmotionHint, ResolveCueTone(metadata.InlineEmotionHint ?? metadata.EmotionHint));
            AddCue(cues, "volume", "◖", metadata.VolumeLevel, "volume");
            AddCue(cues, "delivery", "↗", metadata.DeliveryMode, "delivery");
            AddCue(cues, "articulation", "·", metadata.ArticulationStyle, "articulation");
            AddCue(cues, "emphasis", "!", metadata.IsHighlight ? "Highlight" : null, "emphasis");
            AddCue(cues, "emphasis", "!", metadata.IsEmphasis ? FormatEmphasisCue(metadata) : null, "emphasis");
            AddCue(cues, "energy", "⚡", metadata.EnergyLevel is { } energy ? FormattableString.Invariant($"Energy {energy:+0;-0;0}") : null, "energy");
            AddCue(cues, "melody", "♪", metadata.MelodyLevel is { } melody ? FormattableString.Invariant($"Melody {melody:+0;-0;0}") : null, "melody");
            AddCue(cues, "pronunciation", "◌", metadata.PronunciationGuide, "speech");
            AddCue(cues, "phonetic", "IPA", metadata.PhoneticGuide, "speech");
            AddCue(cues, "stress", "ˈ", metadata.StressGuide ?? metadata.StressText, "speech");
            AddCue(cues, "edit", "✦", metadata.IsEditPoint ? FormatDisplayCue(metadata.EditPointPriority, "Edit") : null, "edit");
            AddCue(cues, "head", "◇", metadata.HeadCue, "delivery");
        }

        return cues;
    }

    private static void AddCue(
        List<EditorRenderedCueViewModel> cues,
        string kind,
        string icon,
        string? label,
        string tone)
    {
        var displayLabel = FormatDisplayCue(label, string.Empty);
        if (string.IsNullOrWhiteSpace(displayLabel) ||
            cues.Any(candidate =>
                string.Equals(candidate.Kind, kind, StringComparison.Ordinal) &&
                string.Equals(candidate.Label, displayLabel, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        cues.Add(new EditorRenderedCueViewModel(kind, icon, displayLabel, tone));
    }

    private static string FormatDurationCue(int? durationMs) =>
        durationMs is > 0
            ? FormattableString.Invariant($"{durationMs.Value}ms")
            : "Pause";

    private static string FormatEmphasisCue(WordMetadata metadata) =>
        !string.IsNullOrWhiteSpace(metadata.EmphasisStyle)
            ? FormatDisplayCue(metadata.EmphasisStyle, "Emphasis")
            : metadata.EmphasisLevel > 0
                ? FormattableString.Invariant($"Emphasis {metadata.EmphasisLevel}")
                : "Emphasis";

    private static string FormatDisplayCue(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var normalized = value.Trim().Replace('-', ' ').Replace('_', ' ');
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(normalized);
    }

    private static string ResolveCueTone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "neutral";
        }

        var normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            "warm" or "happy" or "excited" or "motivational" => "warm",
            "urgent" or "energetic" or "concerned" => "urgent",
            "sad" or "calm" or "focused" or "neutral" => normalized,
            _ => "emotion"
        };
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

    private async Task OnRenderedBlockAttachmentRequestedAsync(EditorRenderedBlockAttachmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(SessionService.State.ScriptId))
        {
            return;
        }

        var file = request.File;
        await using var stream = file.OpenReadStream(MaxBlockAttachmentBytes);
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        var bytes = memory.ToArray();
        var previewDataUrl = file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            ? $"data:{file.ContentType};base64,{Convert.ToBase64String(bytes)}"
            : null;

        var attachment = new EditorBlockAttachment(
            Id: Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
            BlockKey: request.BlockKey,
            FileName: string.IsNullOrWhiteSpace(file.Name) ? request.BlockName : file.Name,
            ContentType: string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            Size: file.Size,
            PreviewDataUrl: previewDataUrl,
            AddedAt: DateTimeOffset.UtcNow);

        _blockAttachments = _blockAttachments
            .Concat([attachment])
            .OrderBy(candidate => candidate.AddedAt)
            .ToList();

        await EditorBlockAttachmentStore.SaveAsync(SessionService.State.ScriptId, _blockAttachments);
        StateHasChanged();
    }

    private async Task LoadRenderedBlockAttachmentsAsync(string? scriptId)
    {
        _blockAttachments = await EditorBlockAttachmentStore.LoadAsync(scriptId);
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

    private static EditorBlockAttachmentViewModel MapAttachment(EditorBlockAttachment attachment) =>
        new(
            attachment.Id,
            attachment.FileName,
            FormatAttachmentKind(attachment.ContentType),
            FormatAttachmentSize(attachment.Size),
            attachment.PreviewDataUrl);

    private static string FormatAttachmentKind(string contentType)
    {
        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return "Image";
        }

        if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
        {
            return "Video";
        }

        if (contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
        {
            return "Audio";
        }

        return "File";
    }

    private static string FormatAttachmentSize(long size)
    {
        const double Kilobyte = 1024d;
        const double Megabyte = Kilobyte * 1024d;

        return size >= Megabyte
            ? string.Create(CultureInfo.InvariantCulture, $"{size / Megabyte:0.#} MB")
            : string.Create(CultureInfo.InvariantCulture, $"{Math.Max(1d, size / Kilobyte):0.#} KB");
    }

    private static T? ElementAtOrNull<T>(IReadOnlyList<T>? values, int index) where T : class =>
        values is not null && index >= 0 && index < values.Count
            ? values[index]
            : null;
}
