namespace PrompterLive.Core.Services.Rsvp;

/// <summary>
/// Manages RSVP playback timing and word display calculations
/// Handles WPM settings, word timing, and playback state
/// Based on proven RSVP algorithms from Squirt/Spritz implementations
/// </summary>
public class RsvpPlaybackEngine(RsvpTextProcessor textProcessor)
{
    private int _wpm = 120; // Default comfortable reading speed
    private const int MIN_WPM = 50;
    private const int MAX_WPM = 1000;
    private const int MinimumWordDisplayMs = 150;
    private const int MaximumWordDisplayMs = 3200;
    private const int DefaultPauseMs = 400;
    private RsvpTextProcessor.ProcessedScript? _processedScript;
    private readonly Dictionary<int, int> _wordDurationsMs = new();
    private readonly Dictionary<int, int> _baselineWpmByWord = new();
    private readonly Dictionary<int, int> _pauseAfterMs = new();
    private readonly Dictionary<int, RsvpTextProcessor.PhraseGroup> _phraseLookup = new();

    // Delay multipliers based on Squirt.js research
    private const double WaitAfterShortWord = 1.2;
    private const double WaitAfterComma = 2.0;
    private const double WaitAfterPeriod = 3.0;
    private const double WaitAfterParagraph = 3.5;
    private const double WaitAfterLongWord = 1.5;

    /// <summary>
    /// Loads processed script timeline for phrase-aware pacing.
    /// </summary>
    /// <param name="processedScript">Processed script containing phrase groups and per-word metadata.</param>
    public void LoadTimeline(RsvpTextProcessor.ProcessedScript? processedScript)
    {
        _processedScript = processedScript;
        _wordDurationsMs.Clear();
        _baselineWpmByWord.Clear();
        _pauseAfterMs.Clear();
        _phraseLookup.Clear();

        if (processedScript == null || processedScript.PhraseGroups.Count == 0)
        {
            return;
        }

        foreach (var phrase in processedScript.PhraseGroups)
        {
            if (phrase.Words.Count == 0)
            {
                continue;
            }

            var phraseIndices = Enumerable.Range(phrase.StartWordIndex, phrase.EndWordIndex - phrase.StartWordIndex + 1).ToArray();
            var intrinsicDurations = new double[phraseIndices.Length];

            double sumIntrinsic = 0;
            for (var i = 0; i < phraseIndices.Length; i++)
            {
                var wordIndex = phraseIndices[i];
                var word = SafeGetWord(processedScript, wordIndex);
                var duration = EvaluateIntrinsicWordDuration(wordIndex, word);
                intrinsicDurations[i] = duration;
                sumIntrinsic += duration;
            }

            if (sumIntrinsic <= 0)
            {
                sumIntrinsic = phrase.EstimatedDurationMs;
            }

            var scale = sumIntrinsic > 0 ? phrase.EstimatedDurationMs / sumIntrinsic : 1.0;

            for (var i = 0; i < phraseIndices.Length; i++)
            {
                var wordIndex = phraseIndices[i];
                var scaledDuration = intrinsicDurations[i] * scale;
                var clampedDuration = (int)Math.Max(MinimumWordDisplayMs, Math.Round(scaledDuration));
                _wordDurationsMs[wordIndex] = clampedDuration;
                _baselineWpmByWord[wordIndex] = ResolveBaselineWpm(wordIndex);
                _phraseLookup[wordIndex] = phrase;

                var isLastWord = wordIndex == phrase.EndWordIndex;
                if (!isLastWord)
                {
                    continue;
                }

                if (phrase.PauseAfterMs > 0)
                {
                    _pauseAfterMs[wordIndex] = phrase.PauseAfterMs;
                }
                else if (phrase.ContainsPauseCue && !_pauseAfterMs.ContainsKey(wordIndex))
                {
                    _pauseAfterMs[wordIndex] = DefaultPauseMs;
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the words per minute
    /// </summary>
    public int WordsPerMinute
    {
        get => _wpm;
        set => _wpm = Math.Max(MIN_WPM, Math.Min(MAX_WPM, value));
    }

    /// <summary>
    /// Increases speed by 10 WPM
    /// </summary>
    public void IncreaseSpeed()
    {
        WordsPerMinute = _wpm + 10;
    }

    /// <summary>
    /// Decreases speed by 10 WPM
    /// </summary>
    public void DecreaseSpeed()
    {
        WordsPerMinute = _wpm - 10;
    }

    /// <summary>
    /// Gets display time for a word based on its characteristics
    /// Using proven delay multipliers from Squirt/Spritz algorithms
    /// </summary>
    /// <param name="word">The word to get timing for</param>
    /// <returns>TimeSpan for how long to display the word</returns>
    public TimeSpan GetWordDisplayTime(int wordIndex, string word)
    {
        var duration = GetWordDisplayMilliseconds(wordIndex, word);
        return TimeSpan.FromMilliseconds(duration);
    }

    public TimeSpan GetWordDisplayTime(string word)
    {
        var duration = GetWordDisplayMilliseconds(-1, word);
        return TimeSpan.FromMilliseconds(duration);
    }

    /// <summary>
    /// Calculate delay multiplier based on word characteristics
    /// Following Squirt.js algorithm for natural reading flow
    /// </summary>
    private static double GetDelayMultiplier(string word)
    {
        // Handle special abbreviations (Mr., Mrs., Ms., etc.)
        if (word is "Mr." or "Mrs." or "Ms." or "Dr." or "Jr." or "Sr.")
        {
            return 1.0;
        }

        // Get the last character, handling quotes
        var lastChar = word[^1];
        if (lastChar is '"' or '"' or '"' && word.Length > 1)
        {
            lastChar = word[^2];
        }

        // Check for paragraph break (newline)
        if (lastChar == '\n')
        {
            return WaitAfterParagraph;
        }

        // Check for sentence ending punctuation
        if (lastChar is '.' or '!' or '?')
        {
            return WaitAfterPeriod;
        }

        // Check for clause separators
        if (lastChar is ',' or ';' or ':' or '–' or '—')
        {
            return WaitAfterComma;
        }

        // Short words (< 4 chars) display slightly longer
        if (word.Length < 4)
        {
            return WaitAfterShortWord;
        }

        // Long words (> 11 chars) need more time
        if (word.Length > 11)
        {
            return WaitAfterLongWord;
        }

        // Normal words get standard timing
        return 1.0;
    }

    private double GetWordDisplayMilliseconds(int wordIndex, string word)
    {
        if (wordIndex >= 0 && _wordDurationsMs.TryGetValue(wordIndex, out var scriptedDuration))
        {
            var baselineWpm = _baselineWpmByWord.TryGetValue(wordIndex, out var baseWpm) ? baseWpm : _wpm;
            var scale = baselineWpm > 0 ? baselineWpm / (double)Math.Max(MIN_WPM, Math.Min(MAX_WPM, _wpm)) : 1.0;
            var adjusted = scriptedDuration * scale;
            return ClampDuration(adjusted);
        }

        if (string.IsNullOrEmpty(word))
        {
            return ClampDuration(60000.0 / Math.Max(MIN_WPM, Math.Min(MAX_WPM, _wpm)) * WaitAfterParagraph);
        }

        var effectiveWpm = wordIndex >= 0 ? ResolveCurrentWpm(wordIndex) : _wpm;
        var baseMs = 60000.0 / Math.Max(MIN_WPM, Math.Min(MAX_WPM, effectiveWpm));
        var delayMultiplier = GetDelayMultiplier(word);
        var finalTime = baseMs * delayMultiplier;
        return ClampDuration(finalTime);
    }

    private static double ClampDuration(double milliseconds)
    {
        if (milliseconds < MinimumWordDisplayMs)
        {
            return MinimumWordDisplayMs;
        }

        if (milliseconds > MaximumWordDisplayMs)
        {
            return MaximumWordDisplayMs;
        }

        return milliseconds;
    }

    private int ResolveCurrentWpm(int wordIndex)
    {
        if (_processedScript == null)
        {
            return _wpm;
        }

        if (_processedScript.WordSpeedOverrides.TryGetValue(wordIndex, out var overrideSpeed))
        {
            return overrideSpeed;
        }

        if (_processedScript.WordToSegmentMap.TryGetValue(wordIndex, out var segmentIndex) &&
            segmentIndex >= 0 && segmentIndex < _processedScript.Segments.Count)
        {
            return _processedScript.Segments[segmentIndex].Speed;
        }

        return _wpm;
    }

    private int ResolveBaselineWpm(int wordIndex)
    {
        if (_processedScript == null)
        {
            return _wpm;
        }

        if (_processedScript.WordSpeedOverrides.TryGetValue(wordIndex, out var overrideSpeed))
        {
            return overrideSpeed;
        }

        if (_processedScript.WordToSegmentMap.TryGetValue(wordIndex, out var segmentIndex) &&
            segmentIndex >= 0 && segmentIndex < _processedScript.Segments.Count)
        {
            return _processedScript.Segments[segmentIndex].Speed;
        }

        return _wpm;
    }

    private double EvaluateIntrinsicWordDuration(int wordIndex, string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return DefaultPauseMs;
        }

        var baselineWpm = ResolveBaselineWpm(wordIndex);
        var baseMs = 60000.0 / Math.Max(MIN_WPM, Math.Min(MAX_WPM, baselineWpm));
        var delayMultiplier = GetDelayMultiplier(word);
        return ClampDuration(baseMs * delayMultiplier);
    }

    private static string SafeGetWord(RsvpTextProcessor.ProcessedScript script, int wordIndex)
    {
        if (wordIndex >= 0 && wordIndex < script.AllWords.Count)
        {
            return script.AllWords[wordIndex];
        }

        return string.Empty;
    }

    /// <summary>
    /// Finds the next section start index from current position
    /// </summary>
    /// <param name="currentIndex">Current word index</param>
    /// <param name="sectionStarts">List of section start indices</param>
    /// <returns>Index of next section start, or -1 if none</returns>
    public int GetNextSectionIndex(int currentIndex, List<int> sectionStarts)
    {
        // Find current section
        var currentSection = 0;
        for (var i = 0; i < sectionStarts.Count; i++)
        {
            if (sectionStarts[i] <= currentIndex)
            {
                currentSection = i;
            }
            else
            {
                break;
            }
        }

        // Return next section if exists
        if (currentSection + 1 < sectionStarts.Count)
        {
            return sectionStarts[currentSection + 1];
        }

        return -1; // No next section
    }

    /// <summary>
    /// Finds the previous section start index from current position
    /// </summary>
    /// <param name="currentIndex">Current word index</param>
    /// <param name="sectionStarts">List of section start indices</param>
    /// <returns>Index of previous section start</returns>
    public int GetPreviousSectionIndex(int currentIndex, List<int> sectionStarts)
    {
        if (sectionStarts == null || sectionStarts.Count == 0)
        {
            return 0;
        }

        var currentSection = 0;
        for (var i = 0; i < sectionStarts.Count; i++)
        {
            if (sectionStarts[i] <= currentIndex)
            {
                currentSection = i;
            }
            else
            {
                break;
            }
        }

        currentSection = Math.Clamp(currentSection, 0, sectionStarts.Count - 1);
        var currentSectionStart = sectionStarts[currentSection];

        if (currentIndex <= currentSectionStart && currentSection > 0)
        {
            return sectionStarts[currentSection - 1];
        }

        return currentSectionStart;
    }

    /// <summary>
    /// Calculates reading progress percentage
    /// </summary>
    /// <param name="currentIndex">Current word index</param>
    /// <param name="totalWords">Total number of words</param>
    /// <returns>Progress percentage (0-100)</returns>
    public double CalculateProgress(int currentIndex, int totalWords)
    {
        if (totalWords == 0)
        {
            return 0;
        }

        return (currentIndex / (double)totalWords) * 100;
    }

    /// <summary>
    /// Estimates time remaining based on current position and speed
    /// </summary>
    /// <param name="currentIndex">Current word index</param>
    /// <param name="words">List of all words</param>
    /// <returns>Estimated time remaining</returns>
    public TimeSpan EstimateTimeRemaining(int currentIndex, List<string> words)
    {
        if (currentIndex >= words.Count)
        {
            return TimeSpan.Zero;
        }

        double totalMs = 0;
        for (var i = currentIndex; i < words.Count; i++)
        {
            totalMs += GetWordDisplayTime(i, words[i]).TotalMilliseconds;
        }

        return TimeSpan.FromMilliseconds(totalMs);
    }

    public int? GetPauseAfterMilliseconds(int wordIndex)
    {
        if (_pauseAfterMs.TryGetValue(wordIndex, out var duration))
        {
            var baselineWpm = _baselineWpmByWord.TryGetValue(wordIndex, out var baseWpm) ? baseWpm : _wpm;
            var scale = baselineWpm > 0 ? baselineWpm / (double)Math.Max(MIN_WPM, Math.Min(MAX_WPM, _wpm)) : 1.0;
            var adjusted = duration * scale;
            return (int)Math.Round(ClampDuration(adjusted));
        }

        return null;
    }

    public RsvpTextProcessor.PhraseGroup? GetPhraseForWord(int wordIndex)
    {
        if (_phraseLookup.TryGetValue(wordIndex, out var phrase))
        {
            return phrase;
        }

        return null;
    }
}
