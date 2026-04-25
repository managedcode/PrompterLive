using System.Globalization;
using Microsoft.Extensions.Localization;
using PrompterOne.Core.Abstractions;
using PrompterOne.Core.Models.CompiledScript;
using PrompterOne.Core.Models.Documents;
using PrompterOne.Core.Services;
using PrompterOne.Core.Services.Editor;
using PrompterOne.Core.Services.Preview;
using PrompterOne.Shared.Components.Library;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Localization;

namespace PrompterOne.Shared.Services.Library;

internal static class LibraryCardFactory
{
    private const int FirstPreviewSegmentIndex = 0;
    private const string FallbackAccentColor = "#2563EB";
    private const string UpdatedLabelFormat = "MMM dd";
    private const int CssRgbHexLength = 6;
    private const int ArgbHexLength = 8;
    private const int AlphaHexComponentLength = 2;
    private const char HexPrefix = '#';

    public static async Task<IReadOnlyList<LibraryCardViewModel>> BuildAsync(
        IReadOnlyList<StoredScriptSummary> summaries,
        IScriptRepository scriptRepository,
        IScriptPreviewService previewService,
        TpsDocumentReader documentReader,
        ScriptCompiler compiler,
        IStringLocalizer<SharedResource> localizer,
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
                documentReader,
                compiler,
                localizer,
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
        TpsDocumentReader documentReader,
        ScriptCompiler compiler,
        IStringLocalizer<SharedResource> localizer,
        CancellationToken cancellationToken)
    {
        var document = await scriptRepository.GetAsync(summary.Id, cancellationToken);
        if (document is null)
        {
            return null;
        }

        var previewSegments = await previewService.BuildPreviewAsync(document.Text, cancellationToken);
        var parsed = await documentReader.ReadAsync(document.Text);
        var compiledScript = await compiler.CompileAsync(parsed);
        var metrics = CalculateMetrics(compiledScript);
        var firstSegment = previewSegments.Count > 0 ? previewSegments[FirstPreviewSegmentIndex] : null;
        var averageWpm = ResolveAverageWpm(previewSegments, compiledScript);
        var segmentCount = ResolveSegmentCount(previewSegments, compiledScript);

        return new LibraryCardViewModel(
            Id: summary.Id,
            Title: summary.Title,
            Emotion: firstSegment?.Emotion ?? DefaultEmotion(localizer),
            CoverClass: ResolveCoverClass(firstSegment?.EmotionKey),
            AccentColor: ResolveAccentColor(firstSegment?.AccentColor, firstSegment?.BackgroundColor),
            AverageWpm: averageWpm,
            WordCount: metrics.WordCount,
            SegmentCount: segmentCount,
            Author: ResolveAuthor(parsed.Metadata, localizer),
            UpdatedAt: summary.UpdatedAt,
            UpdatedLabel: summary.UpdatedAt.ToLocalTime().ToString(UpdatedLabelFormat, CultureInfo.CurrentCulture),
            ModeLabel: ResolveModeLabel(parsed.Metadata, averageWpm, localizer),
            Duration: metrics.Duration,
            DurationLabel: $"{(int)Math.Max(metrics.Duration.TotalMinutes, 0)}:{metrics.Duration.Seconds:00}",
            DocumentName: document.DocumentName,
            SearchText: BuildSearchText(summary.Title, ResolveAuthor(parsed.Metadata, localizer), document.DocumentName, document.Text),
            FolderId: summary.FolderId,
            IsFavorite: summary.IsFavorite,
            DisplayOrder: displayOrder,
            TestId: UiTestIds.Library.Card(summary.Id));
    }

    private static int ResolveAverageWpm(
        IReadOnlyList<SegmentPreviewModel> previewSegments,
        CompiledScript compiledScript)
    {
        if (previewSegments.Count > 0)
        {
            return (int)Math.Round(previewSegments.Average(segment => segment.TargetWpm));
        }

        if (compiledScript.Segments.Count > 0)
        {
            return (int)Math.Round(compiledScript.Segments.Average(segment => Math.Max(1, segment.TargetWPM ?? 140)));
        }

        return 140;
    }

    private static int ResolveSegmentCount(
        IReadOnlyList<SegmentPreviewModel> previewSegments,
        CompiledScript compiledScript)
    {
        if (previewSegments.Count > 0)
        {
            return previewSegments.Count;
        }

        return compiledScript.Segments.Count;
    }

    private static LibraryCardMetrics CalculateMetrics(CompiledScript compiledScript)
    {
        var totalDuration = TimeSpan.Zero;
        var wordCount = 0;

        foreach (var word in EnumerateWords(compiledScript))
        {
            totalDuration += word.DisplayDuration;

            if (word.Metadata?.IsPause == true || string.IsNullOrWhiteSpace(word.CleanText))
            {
                continue;
            }

            wordCount++;
        }

        return new LibraryCardMetrics(wordCount, totalDuration);
    }

    private static string ResolveAuthor(IReadOnlyDictionary<string, string> metadata, IStringLocalizer<SharedResource> localizer) =>
        metadata.TryGetValue(TpsFrontMatterDocumentService.MetadataKeys.Author, out var author) && !string.IsNullOrWhiteSpace(author)
            ? author
            : Text(localizer, UiTextKey.LibraryDefaultAuthor);

    private static string ResolveModeLabel(IReadOnlyDictionary<string, string> metadata, int averageWpm, IStringLocalizer<SharedResource> localizer) =>
        metadata.TryGetValue(TpsFrontMatterDocumentService.MetadataKeys.Profile, out var profile) && !string.IsNullOrWhiteSpace(profile)
            ? profile.Trim().Trim('"')
            : averageWpm >= 250
                ? Text(localizer, UiTextKey.CommonRsvp)
                : Text(localizer, UiTextKey.LibraryDefaultModeActor);

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
            ? NormalizeCssColor(accentColor)
            : !string.IsNullOrWhiteSpace(backgroundColor)
                ? NormalizeCssColor(backgroundColor)
                : FallbackAccentColor;

    private static string NormalizeCssColor(string color)
    {
        var normalized = color.Trim();
        var hex = normalized.TrimStart(HexPrefix);
        if (hex.Length != ArgbHexLength || !hex.All(Uri.IsHexDigit))
        {
            return normalized;
        }

        return string.Concat(HexPrefix, hex.Substring(AlphaHexComponentLength, CssRgbHexLength));
    }

    private static string NormalizeEmotion(string? emotionKey) =>
        string.IsNullOrWhiteSpace(emotionKey)
            ? "neutral"
            : emotionKey.Trim().ToLowerInvariant();

    private static string DefaultEmotion(IStringLocalizer<SharedResource> localizer) =>
        Text(localizer, UiTextKey.LibraryDefaultEmotion);

    private static IEnumerable<CompiledWord> EnumerateWords(CompiledScript compiledScript)
    {
        foreach (var segment in compiledScript.Segments)
        {
            if (segment.Blocks.Count > 0)
            {
                foreach (var block in segment.Blocks)
                {
                    foreach (var word in block.Words)
                    {
                        yield return word;
                    }
                }

                continue;
            }

            foreach (var word in segment.Words)
            {
                yield return word;
            }
        }
    }

    private readonly record struct LibraryCardMetrics(int WordCount, TimeSpan Duration);

    private static string BuildSearchText(
        string title,
        string author,
        string documentName,
        string bodyText) =>
        string.Join(
            ' ',
            new[]
            {
                title,
                author,
                documentName,
                bodyText
            }.Where(value => !string.IsNullOrWhiteSpace(value)));

    private static string Text(IStringLocalizer<SharedResource> localizer, UiTextKey key) =>
        localizer[key.ToString()];
}
