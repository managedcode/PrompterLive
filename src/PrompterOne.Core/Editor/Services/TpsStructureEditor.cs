using System.Globalization;
using ManagedCode.Tps;
using PrompterOne.Core.Models.Editor;

namespace PrompterOne.Core.Services.Editor;

public sealed class TpsStructureEditor
{
    private const string BlockPrefix = "### ";
    private const string SegmentPrefix = "## ";

    public bool TryRead(string? source, int lineStartIndex, out TpsStructureHeaderSnapshot snapshot)
    {
        snapshot = EmptySnapshot(TpsStructureHeaderKind.Segment);
        var safeSource = source ?? string.Empty;
        if (string.IsNullOrWhiteSpace(safeSource))
        {
            return false;
        }

        var lineRange = ResolveLineRange(safeSource, lineStartIndex);
        var line = safeSource[lineRange.Start..lineRange.End];

        if (TryReadHeader(line, lineRange.Start, TpsStructureHeaderKind.Segment, out snapshot))
        {
            return true;
        }

        return TryReadHeader(line, lineRange.Start, TpsStructureHeaderKind.Block, out snapshot);
    }

    public EditorTextMutationResult Update(string? source, TpsStructureHeaderSnapshot snapshot)
    {
        var safeSource = source ?? string.Empty;
        var lineRange = ResolveLineRange(safeSource, snapshot.LineStartIndex);
        var normalizedName = string.IsNullOrWhiteSpace(snapshot.Name)
            ? GetFallbackName(snapshot.Kind)
            : snapshot.Name.Trim();
        var replacement = BuildHeaderLine(snapshot with { Name = normalizedName });
        var updated = string.Concat(
            safeSource[..lineRange.Start],
            replacement,
            safeSource[lineRange.End..]);

        return new EditorTextMutationResult(
            updated,
            new EditorSelectionRange(lineRange.Start, lineRange.Start + replacement.Length));
    }

    private static bool TryReadHeader(string line, int lineStartIndex, TpsStructureHeaderKind kind, out TpsStructureHeaderSnapshot snapshot)
    {
        snapshot = EmptySnapshot(kind);
        var prefix = kind == TpsStructureHeaderKind.Segment ? SegmentPrefix : BlockPrefix;
        var trimmedLine = line.Trim();
        if (!trimmedLine.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var headerContent = trimmedLine[prefix.Length..].Trim();
        if (string.IsNullOrWhiteSpace(headerContent))
        {
            return false;
        }

        if (!headerContent.StartsWith("[", StringComparison.Ordinal) || !headerContent.EndsWith("]", StringComparison.Ordinal))
        {
            snapshot = new TpsStructureHeaderSnapshot(kind, lineStartIndex, lineStartIndex + line.Length, headerContent, null, string.Empty, string.Empty, string.Empty);
            return true;
        }

        var parts = TpsEscaping.SplitHeaderParts(TpsEscaping.Protect(headerContent[1..^1]));
        if (parts.Count == 0 || string.IsNullOrWhiteSpace(parts[0]))
        {
            return false;
        }

        int? wpm = null;
        string? emotion = null;
        string? speaker = null;
        string? timing = null;

        foreach (var rawPart in parts.Skip(1))
        {
            var part = string.IsNullOrWhiteSpace(rawPart) ? null : rawPart.Trim();
            if (part is null)
            {
                continue;
            }

            if (part.StartsWith(TpsSpec.SpeakerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                speaker = part[TpsSpec.SpeakerPrefix.Length..].Trim();
                continue;
            }

            if (TryParseWpm(part, out var parsedWpm))
            {
                wpm = parsedWpm;
                continue;
            }

            if (kind == TpsStructureHeaderKind.Segment && IsTiming(part))
            {
                timing = part;
                continue;
            }

            if (TpsSpec.Emotions.Contains(part))
            {
                emotion = part.ToLowerInvariant();
            }
        }

        snapshot = new TpsStructureHeaderSnapshot(
            kind,
            lineStartIndex,
            lineStartIndex + line.Length,
            parts[0],
            wpm,
            emotion ?? string.Empty,
            speaker ?? string.Empty,
            kind == TpsStructureHeaderKind.Segment ? timing ?? string.Empty : string.Empty);

        return true;
    }

    private static string BuildHeaderLine(TpsStructureHeaderSnapshot snapshot)
    {
        var parts = new List<string> { snapshot.Name.Trim() };
        if (!string.IsNullOrWhiteSpace(snapshot.Speaker))
        {
            parts.Add($"{TpsSpec.SpeakerPrefix}{snapshot.Speaker.Trim()}");
        }

        if (snapshot.TargetWpm is int wpm)
        {
            parts.Add(wpm.ToString(CultureInfo.InvariantCulture) + TpsSpec.WpmSuffix);
        }

        if (!string.IsNullOrWhiteSpace(snapshot.EmotionKey))
        {
            parts.Add(snapshot.EmotionKey.Trim().ToLowerInvariant());
        }

        if (snapshot.SupportsTiming && !string.IsNullOrWhiteSpace(snapshot.Timing))
        {
            parts.Add(snapshot.Timing.Trim());
        }

        var prefix = snapshot.Kind == TpsStructureHeaderKind.Segment ? SegmentPrefix : BlockPrefix;
        return $"{prefix}[{string.Join('|', parts)}]";
    }

    private static EditorSelectionRange ResolveLineRange(string source, int lineStartIndex)
    {
        var safeStart = Math.Clamp(lineStartIndex, 0, source.Length);
        var start = safeStart;
        while (start > 0 && source[start - 1] != '\n')
        {
            start--;
        }

        var end = safeStart;
        while (end < source.Length && source[end] != '\n')
        {
            end++;
        }

        return new EditorSelectionRange(start, end);
    }

    private static bool TryParseWpm(string value, out int? wpm)
    {
        wpm = null;
        var trimmed = value.Trim();
        if (trimmed.EndsWith(TpsSpec.WpmSuffix, StringComparison.OrdinalIgnoreCase))
        {
            var numberPart = trimmed[..^TpsSpec.WpmSuffix.Length];
            if (int.TryParse(numberPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                wpm = parsed;
            }

            return true;
        }

        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var direct))
        {
            wpm = direct;
            return true;
        }

        return false;
    }

    private static bool IsTiming(string value)
    {
        var rangeSeparatorIndex = value.IndexOf('-', StringComparison.Ordinal);
        if (rangeSeparatorIndex < 0)
        {
            return TimeSpan.TryParseExact(value.Trim(), ["m\\:ss", "mm\\:ss"], CultureInfo.InvariantCulture, out _);
        }

        return TimeSpan.TryParseExact(value[..rangeSeparatorIndex].Trim(), ["m\\:ss", "mm\\:ss"], CultureInfo.InvariantCulture, out _) &&
               TimeSpan.TryParseExact(value[(rangeSeparatorIndex + 1)..].Trim(), ["m\\:ss", "mm\\:ss"], CultureInfo.InvariantCulture, out _);
    }

    private static string GetFallbackName(TpsStructureHeaderKind kind) =>
        kind == TpsStructureHeaderKind.Segment ? "Segment" : "Block";

    private static TpsStructureHeaderSnapshot EmptySnapshot(TpsStructureHeaderKind kind) =>
        new(kind, 0, 0, string.Empty, null, string.Empty, string.Empty, string.Empty);
}
