using ManagedCode.Tps;

namespace PrompterOne.Core.Services.Rsvp;

public partial class RsvpTextProcessor(TpsScriptDataFactory scriptDataFactory)
{
    private readonly TpsScriptDataFactory _scriptDataFactory = scriptDataFactory;

    public RsvpTextProcessor()
        : this(new TpsScriptDataFactory())
    {
    }

    public ProcessedScript ParseScript(string content)
    {
        var normalizedContent = NormalizeSource(content);
        if (string.IsNullOrEmpty(normalizedContent))
        {
            return CreateEmptyProcessedScript();
        }

        var processed = RsvpProcessedScriptProjector.Build(
            _scriptDataFactory.Build(normalizedContent),
            usePlainTextReadingDefaults: IsPlainTextOnlySource(normalizedContent));
        RsvpPhraseGroupBuilder.Finalize(processed);
        return processed;
    }

    public List<string> PreprocessText(string text) =>
        ParseScript(text).AllWords.ToList();

    public List<int> FindSectionStarts(List<string> words)
    {
        var sectionStarts = new List<int> { 0 };

        for (var index = 0; index < words.Count - 1; index++)
        {
            var word = words[index];
            if (string.IsNullOrEmpty(word) || !RsvpWordHeuristics.HasSentenceEndingPunctuation(word))
            {
                continue;
            }

            var nextWordIndex = index + 1;
            while (nextWordIndex < words.Count && string.IsNullOrEmpty(words[nextWordIndex]))
            {
                nextWordIndex++;
            }

            if (nextWordIndex < words.Count)
            {
                sectionStarts.Add(nextWordIndex);
            }
        }

        return sectionStarts;
    }

    public bool IsImportantWord(string word)
        => RsvpWordHeuristics.IsImportantWord(word);

    public bool IsShortWord(string word) =>
        RsvpWordHeuristics.IsShortWord(word);

    public bool HasPunctuation(string word) =>
        RsvpWordHeuristics.HasPunctuation(word);

    public ProcessedScript EnhanceScript(ProcessedScript script)
    {
        if (script.AllWords.Count == 0)
        {
            script.PhraseGroups.Clear();
            script.UpcomingEmotionByStartIndex.Clear();
            return script;
        }

        RsvpPhraseGroupBuilder.Finalize(script);
        return script;
    }

    private static ProcessedScript CreateEmptyProcessedScript()
    {
        var result = new ProcessedScript();
        result.Segments.Add(new ProcessedSegment
        {
            Title = RsvpTextProcessorDefaults.DefaultSegmentTitle,
            Emotion = TpsSpec.DefaultEmotion,
            Speed = RsvpTextProcessorDefaults.DefaultSegmentSpeed,
            StartIndex = 0,
            EndIndex = 0
        });

        return result;
    }

    private static string NormalizeSource(string? content) =>
        TpsSourceNormalizer.NormalizeLineEndings(content);

    private static bool IsPlainTextOnlySource(string content)
    {
        return !content.Contains("---", StringComparison.Ordinal) &&
               !content.Contains("##", StringComparison.Ordinal) &&
               !content.Contains('[', StringComparison.Ordinal);
    }
}
