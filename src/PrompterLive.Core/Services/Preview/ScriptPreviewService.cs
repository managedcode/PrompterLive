using PrompterLive.Core.Models.CompiledScript;
using PrompterLive.Core.Models.Tps;

namespace PrompterLive.Core.Services.Preview;

public interface IScriptPreviewService
{
    Task<IReadOnlyList<SegmentPreviewModel>> BuildPreviewAsync(string? tpsContent, CancellationToken cancellationToken = default);
}

public class ScriptPreviewService(TpsParser parser, ScriptCompiler compiler) : IScriptPreviewService
{
    private const int DEFAULT_WPM = 120;

    private readonly TpsParser _parser = parser;
    private readonly ScriptCompiler _compiler = compiler;

    public async Task<IReadOnlyList<SegmentPreviewModel>> BuildPreviewAsync(string? tpsContent, CancellationToken cancellationToken = default)
    {
        var segments = new List<SegmentPreviewModel>();

        if (string.IsNullOrWhiteSpace(tpsContent))
        {
            return segments;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var document = await _parser.ParseAsync(tpsContent).ConfigureAwait(false);

            ApplyDefaults(document);

            cancellationToken.ThrowIfCancellationRequested();

            var compiled = await _compiler.CompileAsync(document).ConfigureAwait(false);

            foreach (var segment in compiled.Segments)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var segmentModel = new SegmentPreviewModel
                {
                    Title = segment.Name,
                    EmotionKey = segment.Emotion,
                    Emotion = AppText.Emotion(segment.Emotion),
                    TargetWpm = segment.TargetWPM ?? DEFAULT_WPM,
                    BackgroundColor = !string.IsNullOrWhiteSpace(segment.BackgroundColor)
                        ? segment.BackgroundColor!
                        : "#FF3B82F6",
                    TextColor = !string.IsNullOrWhiteSpace(segment.TextColor)
                        ? segment.TextColor!
                        : "#FFFFFFFF",
                    AccentColor = segment.AccentColor
                };

                if (segment.Blocks != null && segment.Blocks.Count > 0)
                {
                    foreach (var block in segment.Blocks)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var blockModel = new BlockPreviewModel
                        {
                            Title = block.Name,
                            EmotionKey = block.Emotion,
                            Emotion = AppText.Emotion(block.Emotion),
                            TargetWpm = block.TargetWPM > 0 ? block.TargetWPM : segmentModel.TargetWpm
                        };

                        if (block.Phrases != null && block.Phrases.Count > 0)
                        {
                            foreach (var phrase in block.Phrases)
                            {
                                blockModel.Words.AddRange(BuildWordModels(phrase.Words));
                            }
                        }
                        else
                        {
                            blockModel.Words.AddRange(BuildWordModels(block.Words));
                        }

                        blockModel.Text = blockModel.Words.Count > 0
                            ? BuildPlainText(blockModel.Words)
                            : JoinWords(block.Words);

                        segmentModel.Blocks.Add(blockModel);
                    }
                }
                else
                {
                    segmentModel.SegmentWords.AddRange(BuildWordModels(segment.Words));
                    segmentModel.Content = segmentModel.SegmentWords.Count > 0
                        ? BuildPlainText(segmentModel.SegmentWords)
                        : JoinWords(segment.Words);
                }

                segments.Add(segmentModel);
            }
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("ScriptPreviewService.BuildPreview cancelled");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"ScriptPreviewService.BuildPreview failed: {ex.Message}");
            }
        }

        return segments;
    }

    private static void ApplyDefaults(TpsDocument document)
    {
        foreach (var segment in document.Segments)
        {
            segment.TargetWPM ??= DEFAULT_WPM;

            if (segment.Blocks == null)
            {
                continue;
            }

            foreach (var block in segment.Blocks)
            {
                block.TargetWPM ??= segment.TargetWPM ?? DEFAULT_WPM;
            }
        }
    }

    private static string JoinWords(IEnumerable<CompiledWord> words)
    {
        if (words == null)
        {
            return string.Empty;
        }

        var tokens = words
            .Where(word => word?.Metadata == null || !word.Metadata.IsPause)
            .Select(word => word?.CleanText ?? string.Empty)
            .Where(text => !string.IsNullOrWhiteSpace(text));

        return string.Join(' ', tokens);
    }

    private static IEnumerable<WordPreviewModel> BuildWordModels(IEnumerable<CompiledWord> words)
    {
        if (words == null)
        {
            yield break;
        }

        foreach (var word in words)
        {
            if (word == null)
            {
                continue;
            }

            if (word.Metadata?.IsPause == true)
            {
                yield return new WordPreviewModel
                {
                    Text = "⏸",
                    Color = "#6B7280",
                    IsPause = true
                };
                continue;
            }

            var text = word.CleanText;
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            yield return new WordPreviewModel
            {
                Text = text,
                Color = string.IsNullOrWhiteSpace(word.Metadata?.Color) ? null : word.Metadata!.Color,
                IsEmphasis = word.Metadata?.IsEmphasis == true
            };
        }
    }

    private static string BuildPlainText(IEnumerable<WordPreviewModel> words)
    {
        if (words == null)
        {
            return string.Empty;
        }

        return string.Join(' ', words
            .Where(w => w != null && !w.IsPause)
            .Select(w => w!.Text)
            .Where(text => !string.IsNullOrWhiteSpace(text)));
    }
}
