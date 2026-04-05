using ManagedCode.Tps;

namespace PrompterOne.Core.Services.Rsvp;

internal static class RsvpPhraseGroupBuilder
{
    public static void Finalize(RsvpTextProcessor.ProcessedScript script)
    {
        if (script.AllWords.Count == 0)
        {
            script.PhraseGroups.Clear();
            script.UpcomingEmotionByStartIndex.Clear();
            return;
        }

        GeneratePhraseGroups(script);
        BuildUpcomingEmotionLookup(script);
    }

    private static void GeneratePhraseGroups(RsvpTextProcessor.ProcessedScript script)
    {
        script.PhraseGroups.Clear();

        var state = new PhraseGroupingState();
        for (var index = 0; index < script.AllWords.Count; index++)
        {
            var word = script.AllWords[index];
            if (string.IsNullOrEmpty(word))
            {
                HandlePauseToken(script, state, index);
                continue;
            }

            BeginWordAfterPendingPause(state);
            AccumulateWord(script, state, index, word);
            TryFinalizeCurrentPhrase(script, state, word);
        }

        FinalizeRemainingPhrase(script, state);
    }

    private static void HandlePauseToken(RsvpTextProcessor.ProcessedScript script, PhraseGroupingState state, int wordIndex)
    {
        if (state.CurrentIndices.Count == 0)
        {
            state.PendingPauseMs += GetPauseDuration(script, wordIndex, string.Empty);
            state.PendingPauseFlag = true;
            return;
        }

        state.PendingPauseMs += GetPauseDuration(script, wordIndex, state.CurrentWords[^1]);
        FinalizePhrase(script, state, containsPauseCue: true);
        state.Reset();
    }

    private static void BeginWordAfterPendingPause(PhraseGroupingState state)
    {
        if (state.CurrentIndices.Count == 0 && state.PendingPauseFlag)
        {
            state.PendingPauseMs = 0;
            state.PendingPauseFlag = false;
        }
    }

    private static void AccumulateWord(RsvpTextProcessor.ProcessedScript script, PhraseGroupingState state, int wordIndex, string word)
    {
        state.CurrentIndices.Add(wordIndex);
        state.CurrentWords.Add(word);
        state.LastEmotion = RsvpPhraseTimingEstimator.GetEmotionForWord(script, wordIndex, state.LastEmotion);
        state.AccumulatedDuration += RsvpPhraseTimingEstimator.EstimateWordDuration(script, wordIndex, word);
    }

    private static void TryFinalizeCurrentPhrase(
        RsvpTextProcessor.ProcessedScript script,
        PhraseGroupingState state,
        string word)
    {
        if (!ShouldEndPhrase(word, state.CurrentWords.Count))
        {
            return;
        }

        var containsPauseCue = state.PendingPauseFlag || RsvpWordHeuristics.EndsWithStrongPause(word);
        FinalizePhrase(script, state, containsPauseCue);
        state.Reset();
    }

    private static void FinalizeRemainingPhrase(RsvpTextProcessor.ProcessedScript script, PhraseGroupingState state)
    {
        if (state.CurrentIndices.Count > 0)
        {
            FinalizePhrase(script, state, state.PendingPauseFlag);
        }
    }

    private static void FinalizePhrase(
        RsvpTextProcessor.ProcessedScript script,
        PhraseGroupingState state,
        bool containsPauseCue)
    {
        if (state.CurrentIndices.Count == 0)
        {
            return;
        }

        script.PhraseGroups.Add(new RsvpTextProcessor.PhraseGroup
        {
            StartWordIndex = state.CurrentIndices[0],
            EndWordIndex = state.CurrentIndices[^1],
            Words = state.CurrentWords.ToArray(),
            EstimatedDurationMs = Math.Max(RsvpTextProcessorDefaults.MinimumPhraseDurationMs, state.AccumulatedDuration),
            PauseAfterMs = Math.Max(0, state.PendingPauseMs),
            EmotionHint = state.LastEmotion,
            ContainsPauseCue = containsPauseCue || state.PendingPauseMs > 0,
            ContainsEmphasis = state.CurrentWords.Any(RsvpWordHeuristics.IsImportantWord)
        });
    }

    private static void BuildUpcomingEmotionLookup(RsvpTextProcessor.ProcessedScript script)
    {
        script.UpcomingEmotionByStartIndex.Clear();
        foreach (var phrase in script.PhraseGroups.Where(phrase => !string.IsNullOrWhiteSpace(phrase.EmotionHint)))
        {
            script.UpcomingEmotionByStartIndex[phrase.StartWordIndex] = phrase.EmotionHint;
        }
    }

    private static bool ShouldEndPhrase(string word, int currentPhraseWordCount)
    {
        return currentPhraseWordCount >= 5 ||
               RsvpWordHeuristics.HasSentenceEndingPunctuation(word) ||
               (RsvpWordHeuristics.HasClausePunctuation(word) && currentPhraseWordCount >= 3);
    }

    private static int GetPauseDuration(RsvpTextProcessor.ProcessedScript script, int wordIndex, string previousWord)
    {
        return RsvpPhraseTimingEstimator.GetPauseDuration(script, wordIndex, previousWord);
    }

    private sealed class PhraseGroupingState
    {
        public List<int> CurrentIndices { get; } = [];
        public List<string> CurrentWords { get; } = [];
        public int AccumulatedDuration { get; set; }
        public int PendingPauseMs { get; set; }
        public bool PendingPauseFlag { get; set; }
        public string LastEmotion { get; set; } = TpsSpec.DefaultEmotion;

        public void Reset()
        {
            CurrentIndices.Clear();
            CurrentWords.Clear();
            AccumulatedDuration = 0;
            PendingPauseMs = 0;
            PendingPauseFlag = false;
        }
    }
}
