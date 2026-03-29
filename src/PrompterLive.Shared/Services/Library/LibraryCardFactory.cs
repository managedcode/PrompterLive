using PrompterLive.Core.Abstractions;
using PrompterLive.Core.Models.Documents;
using PrompterLive.Core.Services;
using PrompterLive.Core.Services.Preview;
using PrompterLive.Shared.Components.Library;

using PrompterLive.Shared.Contracts;

namespace PrompterLive.Shared.Services.Library;

internal static class LibraryCardFactory
{
    private const string DefaultAuthor = "You";
    private const string DefaultEmotion = "Neutral";
    private const string DefaultModeLabel = "Actor";
    private const string FallbackAccentColor = "#2563EB";

    public static async Task<IReadOnlyList<LibraryCardViewModel>> BuildAsync(
        IReadOnlyList<StoredScriptSummary> summaries,
        IScriptRepository scriptRepository,
        IScriptPreviewService previewService,
        TpsParser parser,
        CancellationToken cancellationToken = default)
    {
        var cards = new List<LibraryCardViewModel>(summaries.Count);

        for (var index = 0; index < summaries.Count; index++)
        {
            var card = await BuildCardAsync(
                summaries[index],
                index,
                scriptRepository,
                previewService,
                parser,
                cancellationToken);
            if (card is not null)
            {
                cards.Add(card);
            }
        }

        return cards;
    }

    private static async Task<LibraryCardViewModel?> BuildCardAsync(
        StoredScriptSummary summary,
        int displayOrder,
        IScriptRepository scriptRepository,
        IScriptPreviewService previewService,
        TpsParser parser,
        CancellationToken cancellationToken)
    {
        var document = await scriptRepository.GetAsync(summary.Id, cancellationToken);
        if (document is null)
        {
            return null;
        }

        var previewSegments = await previewService.BuildPreviewAsync(document.Text, cancellationToken);
        var parsed = await parser.ParseAsync(document.Text);
        var firstSegment = previewSegments.FirstOrDefault();
        var averageWpm = ResolveAverageWpm(previewSegments, parsed.Metadata);
        var duration = ResolveDuration(parsed.Metadata, summary.WordCount, averageWpm);

        return new LibraryCardViewModel(
            Id: summary.Id,
            Title: summary.Title,
            Emotion: firstSegment?.Emotion ?? DefaultEmotion,
            CoverClass: ResolveCoverClass(firstSegment?.EmotionKey),
            AccentColor: ResolveAccentColor(firstSegment?.AccentColor, firstSegment?.BackgroundColor),
            AverageWpm: averageWpm,
            WordCount: ResolveDisplayInt(parsed.Metadata, "display_word_count", summary.WordCount),
            SegmentCount: ResolveDisplayInt(parsed.Metadata, "display_segment_count", previewSegments.Count > 0 ? previewSegments.Count : 1),
            Author: ResolveAuthor(parsed.Metadata),
            UpdatedAt: summary.UpdatedAt,
            UpdatedLabel: summary.UpdatedAt.ToLocalTime().ToString("MMM dd"),
            ModeLabel: ResolveModeLabel(parsed.Metadata, averageWpm),
            Duration: duration,
            DurationLabel: $"{(int)Math.Max(duration.TotalMinutes, 0)}:{duration.Seconds:00}",
            FolderId: summary.FolderId,
            DisplayOrder: displayOrder,
            TestId: UiTestIds.Library.Card(summary.Id));
    }

    private static int ResolveAverageWpm(
        IReadOnlyList<SegmentPreviewModel> previewSegments,
        IReadOnlyDictionary<string, string> metadata)
    {
        var computedWpm = previewSegments.Count > 0
            ? (int)Math.Round(previewSegments.Average(segment => segment.TargetWpm))
            : 140;

        return ResolveDisplayInt(metadata, "display_wpm", computedWpm);
    }

    private static TimeSpan ResolveDuration(
        IReadOnlyDictionary<string, string> metadata,
        int wordCount,
        int averageWpm)
    {
        var fallbackDuration = TimeSpan.FromMinutes(wordCount / (double)Math.Max(averageWpm, 1));
        if (!metadata.TryGetValue("display_duration", out var rawValue))
        {
            return fallbackDuration;
        }

        var normalized = rawValue.Trim().Trim('"');
        var parts = normalized.Split(':', StringSplitOptions.TrimEntries);
        return parts.Length switch
        {
            2 when int.TryParse(parts[0], out var minutes) && int.TryParse(parts[1], out var seconds)
                => TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds),
            3 when int.TryParse(parts[0], out var hours)
                && int.TryParse(parts[1], out var parsedMinutes)
                && int.TryParse(parts[2], out var parsedSeconds)
                => new TimeSpan(hours, parsedMinutes, parsedSeconds),
            _ => fallbackDuration
        };
    }

    private static string ResolveAuthor(IReadOnlyDictionary<string, string> metadata) =>
        metadata.TryGetValue("author", out var author) && !string.IsNullOrWhiteSpace(author)
            ? author
            : DefaultAuthor;

    private static string ResolveModeLabel(IReadOnlyDictionary<string, string> metadata, int averageWpm) =>
        metadata.TryGetValue("profile", out var profile) && !string.IsNullOrWhiteSpace(profile)
            ? profile.Trim().Trim('"')
            : averageWpm >= 250
                ? "RSVP"
                : DefaultModeLabel;

    private static string ResolveCoverClass(string? emotionKey) =>
        NormalizeEmotion(emotionKey) switch
        {
            "warm" => "dcover-warm",
            "urgent" => "dcover-urgent",
            "motivational" => "dcover-motivational",
            "excited" => "dcover-motivational",
            "calm" => "dcover-calm",
            "focused" => "dcover-calm",
            _ => "dcover-neutral"
        };

    private static string ResolveAccentColor(string? accentColor, string? backgroundColor) =>
        !string.IsNullOrWhiteSpace(accentColor)
            ? accentColor
            : !string.IsNullOrWhiteSpace(backgroundColor)
                ? backgroundColor
                : FallbackAccentColor;

    private static string NormalizeEmotion(string? emotionKey) =>
        string.IsNullOrWhiteSpace(emotionKey)
            ? "neutral"
            : emotionKey.Trim().ToLowerInvariant();

    private static int ResolveDisplayInt(IReadOnlyDictionary<string, string> metadata, string key, int fallback)
    {
        if (metadata.TryGetValue(key, out var rawValue)
            && int.TryParse(rawValue.Trim().Trim('"'), out var parsedValue)
            && parsedValue > 0)
        {
            return parsedValue;
        }

        return fallback;
    }
}
