using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Teleprompter.Models.CompiledScript;
using Teleprompter.Models.HeadCues;
using Teleprompter.Models.Tps;
using System.Text.RegularExpressions;

namespace Teleprompter.Services.Rsvp;

/// <summary>
/// Controller that coordinates all RSVP components
/// Manages the interaction between UI, text processing, and playback
/// </summary>
public class RsvpController
{
    private readonly RsvpTextProcessor _textProcessor;
    private readonly RsvpOrpCalculator _orpCalculator;
    private readonly RsvpEmotionAnalyzer _emotionAnalyzer;
    private readonly RsvpPlaybackEngine _playbackEngine;
    private readonly TpsParser _tpsParser;
    private readonly ScriptCompiler _scriptCompiler;

    private CompiledScript? _compiledScript;
    private List<CompiledWord> _currentWords = new();
    private List<string> _words = new();  // Keep for backward compatibility
    private List<int> _sectionStarts = new();
    private RsvpTextProcessor.ProcessedScript? _processedScript;
    private readonly List<(int Start, int End)> _segmentRanges = new();
    private int _currentWordIndex = 0;
    private ScriptData? _scriptData;
    private bool _isPlaying = false;
    private bool _isStopped = true;
    private CancellationTokenSource? _playbackCancellation;
    private Task? _playbackTask;
    private DateTime _startTime = DateTime.Now;
    private string _currentEmotion = "neutral";
    private string _currentHeadCueId = HeadCueCatalog.Neutral.Id;
    private int _lastHeadCuePhraseStartIndex = -1;
    private readonly Dictionary<int, string> _phraseHeadCuePlan = new();
    private static readonly Dictionary<string, string[]> EmotionCuePatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["neutral"] = new[] { "H0", "H5", "H0", "H6" },
        ["professional"] = new[] { "H9", "H5", "H9", "H6" },
        ["motivational"] = new[] { "H9", "H1", "H2", "H9" },
        ["energetic"] = new[] { "H8", "H2", "H1", "H8" },
        ["warm"] = new[] { "H7", "H1", "H0", "H7" },
        ["happy"] = new[] { "H6", "H1", "H6", "H7" },
        ["excited"] = new[] { "H8", "H2", "H6", "H8" },
        ["calm"] = new[] { "H0", "H3", "H0", "H5" },
        ["concerned"] = new[] { "H1", "H3", "H1", "H0" },
        ["urgent"] = new[] { "H4", "H2", "H4", "H0" },
        ["sad"] = new[] { "H3", "H0", "H3", "H5" },
        ["focused"] = new[] { "H5", "H0", "H6", "H0" },
        ["confident"] = new[] { "H9", "H1", "H9", "H5" }
    };

    private static readonly string[][] DefaultCuePatterns =
    {
        new[] { "H0", "H5", "H0", "H6" },
        new[] { "H0", "H1", "H9", "H0" },
        new[] { "H0", "H7", "H0", "H8" }
    };
    private int _currentSpeed = 250;
    private bool _ignoreScriptSpeeds;
    private const int QuickSeekWordCount = 6;


    #region Events

    /// <summary>
    /// Raised when word display needs to be updated
    /// </summary>
    public event EventHandler<Rsvp.WordDisplayEventArgs>? WordDisplayUpdate;

    /// <summary>
    /// Raised when emotion changes
    /// </summary>
    public event EventHandler<Rsvp.EmotionChangeEventArgs>? EmotionChanged;

    /// <summary>
    /// Raised when playback state changes
    /// </summary>
    public event EventHandler<Rsvp.PlaybackStateEventArgs>? PlaybackStateChanged;

    /// <summary>
    /// Raised when progress updates
    /// </summary>
    public event EventHandler<Rsvp.ProgressEventArgs>? ProgressUpdate;
    
    /// <summary>
    /// Raised when reading speed changes
    /// </summary>
    public event EventHandler<int>? SpeedChanged;

    #endregion


    public RsvpController()
    {
        _textProcessor = new RsvpTextProcessor();
        _orpCalculator = new RsvpOrpCalculator();
        _emotionAnalyzer = new RsvpEmotionAnalyzer();
        _playbackEngine = new RsvpPlaybackEngine(_textProcessor);
        _tpsParser = new TpsParser();
        _scriptCompiler = new ScriptCompiler();
    }

    #region Properties

    public bool IsPlaying => _isPlaying;
    public bool IsStopped => _isStopped;
    public int CurrentWordIndex => _currentWordIndex;
    public int TotalWords => _currentWords.Count;
    public int CurrentWpm => _playbackEngine.WordsPerMinute;
    public ScriptData? ScriptData => _scriptData;
    public int WordsPerMinute
    {
        get => _playbackEngine.WordsPerMinute;
        set
        {
            var clamped = Math.Max(50, value);
            _playbackEngine.WordsPerMinute = clamped;
            _currentSpeed = clamped;
            SpeedChanged?.Invoke(this, clamped);
        }
    }

    public bool IgnoreScriptSpeeds
    {
        get => _ignoreScriptSpeeds;
        set => _ignoreScriptSpeeds = value;
    }

    public void ConfigurePracticeMode(int initialWpm)
    {
        _ignoreScriptSpeeds = true;
        var clamped = Math.Max(50, initialWpm);
        _currentSpeed = clamped;
        _playbackEngine.WordsPerMinute = clamped;
        SpeedChanged?.Invoke(this, clamped);
    }

    public void DisablePracticeMode()
    {
        _ignoreScriptSpeeds = false;
    }
    
    public int CurrentSegmentIndex
    {
        get
        {
            if (_processedScript != null &&
                _processedScript.WordToSegmentMap.TryGetValue(_currentWordIndex, out var segIndex))
            {
                return segIndex + 1; // Convert to 1-based for display
            }

            if (_segmentRanges.Count > 0)
            {
                for (int i = 0; i < _segmentRanges.Count; i++)
                {
                    var (start, end) = _segmentRanges[i];
                    if (_currentWordIndex >= start && _currentWordIndex <= end)
                    {
                        return i + 1;
                    }
                }
            }

            if (_compiledScript?.Segments.Count > 0)
            {
                var wordsPerSegment = Math.Max(1, _compiledScript.Segments.Count);
                var segmentLength = Math.Max(1, _words.Count / wordsPerSegment);
                var fallbackIndex = (_currentWordIndex / segmentLength) + 1;
                return Math.Min(fallbackIndex, _compiledScript.Segments.Count);
            }

            if (_scriptData?.Segments?.Length > 0)
            {
                var wordsPerSegment = Math.Max(1, _scriptData.Segments.Length);
                var segmentLength = Math.Max(1, _words.Count / wordsPerSegment);
                var fallbackIndex = (_currentWordIndex / segmentLength) + 1;
                return Math.Min(fallbackIndex, _scriptData.Segments.Length);
            }

            return 1;
        }
    }
    
    public int TotalSegments
    {
        get
        {
            if (_processedScript?.Segments.Count > 0)
            {
                return _processedScript.Segments.Count;
            }

            if (_segmentRanges.Count > 0)
            {
                return _segmentRanges.Count;
            }

            if (_compiledScript?.Segments.Count > 0)
            {
                return _compiledScript.Segments.Count;
            }

            if (_scriptData?.Segments?.Length > 0)
            {
                return _scriptData.Segments.Length;
            }

            return 1;
        }
    }

    public IReadOnlyList<(int Start, int End)> SegmentRanges => _segmentRanges;

    #endregion

    #region Text Loading

    /// <summary>
    /// Loads a pre-compiled script directly for RSVP display
    /// </summary>
    public async Task LoadCompiledScript(CompiledScript compiledScript)
    {
        if (compiledScript == null || compiledScript.Segments.Count == 0)
            return;

        _compiledScript = compiledScript;

        var defaultWpm = DetermineDefaultWpm(compiledScript, 250);
        BuildPlaybackDataFromCompiledScript(defaultWpm);

        ApplyInitialPlaybackState();

        System.Diagnostics.Debug.WriteLine($"LoadCompiledScript - Loaded. Words: {_words.Count}, CompiledWords: {_currentWords.Count}");
    }

    /// <summary>
    /// Loads and preprocesses text for RSVP display
    /// </summary>
    public void LoadText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        // Use ParseScript to properly handle commands and clean text
        _processedScript = _textProcessor.ParseScript(text);
        _processedScript = _textProcessor.EnhanceScript(_processedScript);
        _playbackEngine.LoadTimeline(_processedScript);
        _words = _processedScript.AllWords;

        BuildHeadCuePlan();

        BuildHeadCuePlan();
        _sectionStarts = _textProcessor.FindSectionStarts(_words);
        _currentWordIndex = 0;
        _isStopped = true;  // Start in stopped state, ready to play
        _isPlaying = false;
        RebuildSegmentRangesFromProcessedScript();

        // Skip initial empty words if any
        while (_currentWordIndex < _words.Count && string.IsNullOrEmpty(_words[_currentWordIndex]))
        {
            _currentWordIndex++;
        }

        // Apply initial emotion and speed if available
        if (_processedScript.Segments.Count > 0)
        {
            var firstSegment = _processedScript.Segments[0];
            _currentEmotion = firstSegment.Emotion;
            _currentSpeed = firstSegment.Speed;
            _playbackEngine.WordsPerMinute = _currentSpeed;
            
            // Raise emotion change event
            RaiseEmotionChanged(_currentEmotion);
            SpeedChanged?.Invoke(this, _currentSpeed);
        }

        // Display first word
        DisplayCurrentWord();
        UpdateProgress();
        RaisePlaybackStateChanged();
    }

    /// <summary>
    /// Loads structured ScriptData (segments/blocks/phrases/words) and builds processed script
    /// </summary>
    public async Task LoadScriptData(ScriptData scriptData)
    {
        if (scriptData == null) return;

        // Store the script data for segment tracking
        _scriptData = scriptData;

        // If we have TPS content, parse and compile it properly
        if (!string.IsNullOrEmpty(scriptData.Content))
        {
            System.Diagnostics.Debug.WriteLine($"LoadScriptData - Parsing TPS content, length: {scriptData.Content.Length}");

            // Parse TPS to structured format
            var tpsDocument = await _tpsParser.ParseAsync(scriptData.Content);
            System.Diagnostics.Debug.WriteLine($"LoadScriptData - TPS parsed. Segments in document: {tpsDocument.Segments?.Count ?? 0}");

            if (tpsDocument.Segments == null || tpsDocument.Segments.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"LoadScriptData - ERROR: No segments found after parsing. First 500 chars: {scriptData.Content.Substring(0, Math.Min(500, scriptData.Content.Length))}");
                return;
            }

            // Compile to clean script for display
            _compiledScript = await _scriptCompiler.CompileAsync(tpsDocument);
            System.Diagnostics.Debug.WriteLine($"LoadScriptData - Script compiled. Segments: {_compiledScript?.Segments?.Count ?? 0}");

            var defaultWpm = scriptData.TargetWpm > 0
                ? scriptData.TargetWpm
                : DetermineDefaultWpm(_compiledScript, 250);

            BuildPlaybackDataFromCompiledScript(defaultWpm);
            ApplyInitialPlaybackState();

            System.Diagnostics.Debug.WriteLine($"LoadScriptData - TPS parsed. Words: {_words.Count}, CompiledWords: {_currentWords.Count}");
            return;
        }

        var processed = new RsvpTextProcessor.ProcessedScript();
        var allWords = new List<string>();
        int wordIndex = 0;

        int fallbackWpm = scriptData.TargetWpm > 0 ? scriptData.TargetWpm : 250;

        if (scriptData.Segments != null && scriptData.Segments.Length > 0)
        {
            foreach (var seg in scriptData.Segments)
            {
                var segStart = wordIndex;
                var segSpeed = seg.WpmOverride ?? fallbackWpm;
                var segMax = seg.WpmMax.HasValue && seg.WpmMax.Value > segSpeed ? seg.WpmMax.Value : (int?)null;
                var segEmotion = string.IsNullOrWhiteSpace(seg.Emotion) ? "neutral" : seg.Emotion;
                var segmentDefaultIndices = new List<int>();
                var segmentFactorOverrides = new Dictionary<int, float>();

                if (seg.Blocks != null && seg.Blocks.Length > 0)
                {
                    foreach (var blk in seg.Blocks)
                    {
                        var blockSpeed = blk.WpmOverride ?? segSpeed;
                        var blockEmotion = string.IsNullOrWhiteSpace(blk.Emotion) ? segEmotion : blk.Emotion!;

                        // Use phrases if present; else split block content
                        if (blk.Phrases != null && blk.Phrases.Length > 0)
                        {
                            foreach (var phr in blk.Phrases)
                            {
                                var phraseWords = new List<string>();
                                if (phr.Words != null && phr.Words.Length > 0)
                                {
                                    foreach (var w in phr.Words)
                                    {
                                        var t = w.Text ?? string.Empty;
                                        if (!string.IsNullOrWhiteSpace(t))
                                        {
                                            allWords.Add(t);
                                            phraseWords.Add(t);
                                            processed.WordToSegmentMap[wordIndex] = processed.Segments.Count;
                                            if (w.WpmOverride.HasValue)
                                            {
                                                if (w.WpmOverride.Value >= 0)
                                                {
                                                    processed.WordSpeedOverrides[wordIndex] = w.WpmOverride.Value;
                                                }
                                                else
                                                {
                                                    // -1 => slow (0.8), -2 => fast (1.2)
                                                    var factor = w.WpmOverride.Value == -1 ? 0.8f : 1.2f;
                                                    segmentFactorOverrides[wordIndex] = factor;
                                                    processed.WordSpeedOverrides[wordIndex] = blk.WpmOverride ?? segSpeed;
                                                }
                                            }
                                            else if (blk.WpmOverride.HasValue)
                                            {
                                                processed.WordSpeedOverrides[wordIndex] = blk.WpmOverride.Value;
                                            }
                                            else
                                            {
                                                processed.WordSpeedOverrides[wordIndex] = segSpeed; // temp, interpolate later if segMax
                                                segmentDefaultIndices.Add(wordIndex);
                                            }
                                            processed.WordEmotionOverrides[wordIndex] = blockEmotion;

                                            // Store color override if present
                                            if (!string.IsNullOrEmpty(w.Color))
                                            {
                                                processed.WordColorOverrides[wordIndex] = w.Color;
                                                System.Diagnostics.Debug.WriteLine($"Storing color '{w.Color}' for word '{t}' at index {wordIndex}");
                                            }

                                            wordIndex++;
                                        }
                                    }
                                }
                                else if (!string.IsNullOrWhiteSpace(phr.Text))
                                {
                                    var tokens = phr.Text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                                    foreach (var t in tokens)
                                    {
                                        if (t == "/") { allWords.Add(""); continue; }
                                        if (t == "//") { allWords.Add(""); allWords.Add(""); continue; }
                                        allWords.Add(t);
                                        phraseWords.Add(t);
                                        processed.WordToSegmentMap[wordIndex] = processed.Segments.Count;
                                        if (blk.WpmOverride.HasValue)
                                        {
                                            processed.WordSpeedOverrides[wordIndex] = blk.WpmOverride.Value;
                                        }
                                        else
                                        {
                                            processed.WordSpeedOverrides[wordIndex] = segSpeed;
                                            segmentDefaultIndices.Add(wordIndex);
                                        }
                                        processed.WordEmotionOverrides[wordIndex] = blockEmotion;
                                        wordIndex++;
                                        if (t.Any(c => ".!?".Contains(c))) { allWords.Add(""); allWords.Add(""); }
                                    }
                                }

                                // Phrase‑level pause
                                if (phr.PauseDuration.HasValue && phr.PauseDuration.Value > 0)
                                {
                                    allWords.Add("");
                                    if (phr.PauseDuration.Value > 700) allWords.Add("");
                                }
                            }
                        }
                        else if (!string.IsNullOrWhiteSpace(blk.Content))
                        {
                            var tokens = blk.Content.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var t in tokens)
                            {
                                if (t == "/") { allWords.Add(""); continue; }
                                if (t == "//") { allWords.Add(""); allWords.Add(""); continue; }
                                allWords.Add(t);
                                processed.WordToSegmentMap[wordIndex] = processed.Segments.Count;
                                if (blk.WpmOverride.HasValue)
                                {
                                    processed.WordSpeedOverrides[wordIndex] = blk.WpmOverride.Value;
                                }
                                else
                                {
                                    processed.WordSpeedOverrides[wordIndex] = segSpeed;
                                    segmentDefaultIndices.Add(wordIndex);
                                }
                                processed.WordEmotionOverrides[wordIndex] = blockEmotion;
                                wordIndex++;
                                if (t.Any(c => ".!?".Contains(c))) { allWords.Add(""); allWords.Add(""); }
                            }
                        }
                    }
                }
                else if (!string.IsNullOrWhiteSpace(seg.Content))
                {
                    var tokens = seg.Content.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var t in tokens)
                    {
                        if (t == "/") { allWords.Add(""); continue; }
                        if (t == "//") { allWords.Add(""); allWords.Add(""); continue; }
                        allWords.Add(t);
                        processed.WordToSegmentMap[wordIndex] = processed.Segments.Count;
                        processed.WordSpeedOverrides[wordIndex] = segSpeed;
                        segmentDefaultIndices.Add(wordIndex);
                        processed.WordEmotionOverrides[wordIndex] = segEmotion;
                        wordIndex++;
                        if (t.Any(c => ".!?".Contains(c))) { allWords.Add(""); allWords.Add(""); }
                    }
                }

                var segEnd = Math.Max(segStart, wordIndex - 1);
                processed.Segments.Add(new RsvpTextProcessor.ProcessedSegment
                {
                    Title = seg.Name,
                    Emotion = segEmotion,
                    Speed = segSpeed,
                    StartIndex = segStart,
                    EndIndex = segEnd,
                    Words = new List<string>() // optional
                });


                // Interpolate speeds across segment defaults if a range is defined
                if (segMax.HasValue && segEnd >= segStart)
                {
                    var denom = Math.Max(1, segEnd - segStart);
                    foreach (var idx in segmentDefaultIndices)
                    {
                        var frac = (idx - segStart) / (float)denom;
                        var spd = segSpeed + (int)Math.Round((segMax.Value - segSpeed) * frac);
                        processed.WordSpeedOverrides[idx] = spd;
                    }
                }

                // Apply per-word slow/fast factors relative to computed base speed
                foreach (var kv in segmentFactorOverrides)
                {
                    var baseSpd = processed.WordSpeedOverrides.TryGetValue(kv.Key, out var b) ? b : segSpeed;
                    var spd = (int)Math.Round(baseSpd * kv.Value);
                    processed.WordSpeedOverrides[kv.Key] = Math.Max(50, spd);
                }
            }
        }
        else if (!string.IsNullOrWhiteSpace(scriptData.Content))
        {
            // Fallback to plain content
            _processedScript = _textProcessor.ParseScript(scriptData.Content);
            _processedScript = _textProcessor.EnhanceScript(_processedScript);
            _playbackEngine.LoadTimeline(_processedScript);
            _words = _processedScript.AllWords;
            _sectionStarts = _textProcessor.FindSectionStarts(_words);
            _currentWordIndex = 0;
            _isStopped = true;
            RebuildSegmentRangesFromProcessedScript();
            DisplayCurrentWord();
            UpdateProgress();
            RaisePlaybackStateChanged();
            return;
        }

        processed.AllWords = allWords;
        _processedScript = processed;
        _processedScript = _textProcessor.EnhanceScript(_processedScript);
        _playbackEngine.LoadTimeline(_processedScript);
        BuildHeadCuePlan();
        _words = allWords;
        _sectionStarts = _textProcessor.FindSectionStarts(_words);
        _currentWordIndex = 0;
        _isStopped = true;
        RebuildSegmentRangesFromProcessedScript();

        // Initial emotion/speed from first segment
        if (_processedScript.Segments.Count > 0)
        {
            var first = _processedScript.Segments[0];
            _currentEmotion = first.Emotion;
            _playbackEngine.WordsPerMinute = first.Speed > 0 ? first.Speed : fallbackWpm;
            RaiseEmotionChanged(_currentEmotion);
            SpeedChanged?.Invoke(this, _playbackEngine.WordsPerMinute);
        }

        DisplayCurrentWord();
        UpdateProgress();
        RaisePlaybackStateChanged();
    }

    #endregion

    #region Helper Methods

    private void RebuildSegmentRangesFromProcessedScript()
    {
        _segmentRanges.Clear();

        if (_processedScript?.Segments.Count > 0)
        {
            foreach (var segment in _processedScript.Segments)
            {
                var start = Math.Max(0, segment.StartIndex);
                var end = Math.Max(start, Math.Min(segment.EndIndex, _words.Count - 1));
                _segmentRanges.Add((start, end));
            }
        }
    }

    private int DetermineDefaultWpm(CompiledScript? script, int fallback)
    {
        if (script?.Segments == null || script.Segments.Count == 0)
        {
            return fallback;
        }

        var firstWithSpeed = script.Segments.FirstOrDefault(s => s.TargetWPM.HasValue && s.TargetWPM.Value > 0);
        return firstWithSpeed?.TargetWPM ?? fallback;
    }

    private void BuildPlaybackDataFromCompiledScript(int defaultWpm)
    {
        _currentWords.Clear();
        _words.Clear();
        _currentWordIndex = 0;
        _processedScript = new RsvpTextProcessor.ProcessedScript();
        _segmentRanges.Clear();

        if (_compiledScript == null || _compiledScript.Segments.Count == 0)
        {
            _processedScript.AllWords = new List<string>();
            _sectionStarts = new List<int>();
            return;
        }

        for (int i = 0; i < _compiledScript.Segments.Count; i++)
        {
            ProcessCompiledSegment(_compiledScript.Segments[i], i, defaultWpm);
        }

        _processedScript.AllWords = new List<string>(_words);
        _sectionStarts = _textProcessor.FindSectionStarts(_words);
        RebuildSegmentRangesFromProcessedScript();
        _processedScript = _textProcessor.EnhanceScript(_processedScript);
        _playbackEngine.LoadTimeline(_processedScript);
    }

    private void ProcessCompiledSegment(CompiledSegment segment, int segmentIndex, int defaultWpm)
    {
        var segmentEmotion = NormalizeEmotionLabel(segment.Emotion);
        var segmentSpeed = segment.TargetWPM ?? defaultWpm;
        var segmentStartIndex = _words.Count;
        var segmentWords = new List<string>();

        void AddCompiledWord(CompiledWord word, string emotionContext, int speedContext)
        {
            var normalizedEmotion = NormalizeEmotionLabel(emotionContext);
            var effectiveEmotion = !string.IsNullOrWhiteSpace(word.Metadata.EmotionHint)
                ? NormalizeEmotionLabel(word.Metadata.EmotionHint)
                : normalizedEmotion;

            var wordIndex = _words.Count;
            _currentWords.Add(word);
            _processedScript.WordToSegmentMap[wordIndex] = segmentIndex;

            if (word.Metadata.IsPause)
            {
                _words.Add(string.Empty);
                if (word.Metadata.PauseDuration.HasValue && word.Metadata.PauseDuration.Value > 0)
                {
                    _processedScript.PauseDurations[wordIndex] = word.Metadata.PauseDuration.Value;
                }
                return;
            }

            _words.Add(word.CleanText);
            segmentWords.Add(word.CleanText);

            if (!string.IsNullOrWhiteSpace(effectiveEmotion) &&
                !string.Equals(effectiveEmotion, segmentEmotion, StringComparison.OrdinalIgnoreCase))
            {
                _processedScript.WordEmotionOverrides[wordIndex] = effectiveEmotion;
            }

            var normalizedColor = NormalizeColor(word.Metadata.Color);
            if (!string.IsNullOrEmpty(normalizedColor))
            {
                _processedScript.WordColorOverrides[wordIndex] = normalizedColor;
            }

            var effectiveSpeed = speedContext;

            if (word.Metadata.SpeedOverride.HasValue && word.Metadata.SpeedOverride.Value > 0)
            {
                effectiveSpeed = word.Metadata.SpeedOverride.Value;
            }
            else if (word.Metadata.SpeedMultiplier.HasValue && speedContext > 0)
            {
                var scaled = (int)Math.Round(speedContext * word.Metadata.SpeedMultiplier.Value);
                if (scaled > 0)
                {
                    effectiveSpeed = scaled;
                }
            }

            if (effectiveSpeed > 0)
            {
                _processedScript.WordSpeedOverrides[wordIndex] = effectiveSpeed;
            }
        }

        var hasBlocks = segment.Blocks != null && segment.Blocks.Count > 0;

        if (!hasBlocks && segment.Words != null && segment.Words.Count > 0)
        {
            foreach (var word in segment.Words)
            {
                AddCompiledWord(word, segmentEmotion, segmentSpeed);
            }
        }

        foreach (var block in segment.Blocks)
        {
            var blockEmotion = !string.IsNullOrWhiteSpace(block.Emotion) ? block.Emotion! : segmentEmotion;
            var blockSpeed = block.TargetWPM > 0 ? block.TargetWPM : segmentSpeed;

            var hasPhrases = block.Phrases != null && block.Phrases.Count > 0;

            if (!hasPhrases && block.Words != null && block.Words.Count > 0)
            {
                foreach (var word in block.Words)
                {
                    AddCompiledWord(word, blockEmotion, blockSpeed);
                }
            }

            if (hasPhrases)
            {
                foreach (var phrase in block.Phrases)
                {
                    if (phrase.Words == null || phrase.Words.Count == 0)
                        continue;

                    foreach (var word in phrase.Words)
                    {
                        AddCompiledWord(word, blockEmotion, blockSpeed);
                    }
                }
            }
        }

        var segmentEndIndex = _words.Count == 0 
            ? segmentStartIndex
            : (_words.Count > segmentStartIndex ? _words.Count - 1 : segmentStartIndex);

        _processedScript.Segments.Add(new RsvpTextProcessor.ProcessedSegment
        {
            Title = segment.Name,
            Emotion = segmentEmotion,
            Speed = segmentSpeed,
            Words = segmentWords,
            StartIndex = segmentStartIndex,
            EndIndex = segmentEndIndex
        });

    }

    private void ApplyInitialPlaybackState()
    {
        var fallbackWpm = DetermineDefaultWpm(_compiledScript, 250);
        RebuildSegmentRangesFromProcessedScript();

        if (_compiledScript != null && _compiledScript.Segments.Count > 0)
        {
            var firstSegment = _compiledScript.Segments[0];
            _currentEmotion = NormalizeEmotionLabel(firstSegment.Emotion);
            _currentSpeed = firstSegment.TargetWPM ?? fallbackWpm;
        }
        else
        {
            _currentEmotion = "neutral";
            _currentSpeed = fallbackWpm;
        }

        _playbackEngine.WordsPerMinute = _currentSpeed;
        RaiseEmotionChanged(_currentEmotion);
        SpeedChanged?.Invoke(this, _currentSpeed);

        _currentWordIndex = 0;
        _isStopped = true;
        _isPlaying = false;

        while (_currentWordIndex < _words.Count && string.IsNullOrEmpty(_words[_currentWordIndex]))
        {
            _currentWordIndex++;
        }

        DisplayCurrentWord();
        UpdateProgress();
        RaisePlaybackStateChanged();
    }

    private string NormalizeEmotionLabel(string? emotion)
    {
        if (string.IsNullOrWhiteSpace(emotion)) return "neutral";

        var cleaned = Regex.Replace(emotion, @"[\p{So}\p{C}]", string.Empty)
                             .Trim()
                             .ToLowerInvariant();

        return cleaned switch
        {
            "joyful" or "cheerful" or "happy" => "happy",
            "excited" => "excited",
            "energetic" => "energetic",
            "warm" => "warm",
            "motivational" or "inspiring" or "determined" => "motivational",
            "focused" or "serious" => "focused",
            "concerned" or "worried" => "concerned",
            "urgent" or "critical" => "urgent",
            "calm" or "peaceful" => "calm",
            "sad" or "melancholy" => "sad",
            "angry" or "frustrated" => "angry",
            "fear" or "anxious" => "fear",
            "professional" or "formal" => "professional",
            _ when string.IsNullOrWhiteSpace(cleaned) => "neutral",
            _ => cleaned
        };
    }

    private string NormalizeColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return string.Empty;
        }

        var trimmed = color.Trim();

        if (trimmed.StartsWith("#", StringComparison.Ordinal))
        {
            var match = ScriptCompiler.AvailableColors.FirstOrDefault(
                kvp => string.Equals(kvp.Value, trimmed, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(match.Key))
            {
                return match.Key;
            }
        }

        return trimmed.ToLowerInvariant();
    }

    #endregion

    #region Playback Control

    public void StartReading()
    {
        if (_words.Count == 0) return;

        _isPlaying = true;
        _isStopped = false;
        RaisePlaybackStateChanged();
        DisplayCurrentWord();
        
        // Start async playback loop
        _playbackCancellation = new CancellationTokenSource();
        _playbackTask = RunPlaybackLoopAsync(_playbackCancellation.Token);
    }

    public void PauseReading()
    {
        _isPlaying = false;
        _playbackCancellation?.Cancel();
        _playbackCancellation = null;
        RaisePlaybackStateChanged();
    }

    public void TogglePlayPause()
    {
        System.Diagnostics.Debug.WriteLine($"TogglePlayPause called. IsPlaying: {_isPlaying}");
        if (_isPlaying)
        {
            PauseReading();
        }
        else
        {
            if (_isStopped)
            {
                ResetToStart();
            }
            StartReading();
        }
    }

    public void TogglePlayback() => TogglePlayPause();

    public void StopReading()
    {
        _isPlaying = false;
        _isStopped = true;
        _playbackCancellation?.Cancel();
        _playbackCancellation = null;
        RaisePlaybackStateChanged();
    }

    private void ResetToStart()
    {
        _currentWordIndex = 0;
        while (_currentWordIndex < _words.Count && string.IsNullOrEmpty(_words[_currentWordIndex]))
        {
            _currentWordIndex++;
        }
        _isStopped = false;
        DisplayCurrentWord();
    }
    
    public void Reset()
    {
        bool wasPlaying = _isPlaying;
        if (_isPlaying)
        {
            PauseReading();
        }
        ResetToStart();
        UpdateProgress();
        if (wasPlaying)
        {
            StartReading();
        }
    }

    #endregion

    #region Navigation

    public void SeekBackwardChunk() => SeekRelativeWords(-QuickSeekWordCount);

    public void SeekForwardChunk() => SeekRelativeWords(QuickSeekWordCount);

    public void SeekRelativeWords(int delta)
    {
        if (_words.Count == 0 || delta == 0)
        {
            return;
        }

        NavigateToWordIndex(_currentWordIndex + delta);
    }

    public void RepeatCurrentSegment()
    {
        if (_words.Count == 0)
        {
            return;
        }

        foreach (var (start, end) in _segmentRanges)
        {
            if (_currentWordIndex >= start && _currentWordIndex <= end)
            {
                NavigateToWordIndex(start);
                return;
            }
        }

        var currentSectionStart = _sectionStarts.LastOrDefault(start => start <= _currentWordIndex);
        NavigateToWordIndex(currentSectionStart);
    }

    public void NextWord()
    {
        if (_words.Count == 0) return;

        // Cancel current playback temporarily
        _playbackCancellation?.Cancel();

        _currentWordIndex++;
        if (_currentWordIndex >= _words.Count)
        {
            _currentWordIndex = _words.Count - 1;
            StopReading();
            return;
        }

        _isStopped = false;
        DisplayCurrentWord();
        UpdateProgress();

        if (_isPlaying)
        {
            // Restart playback from new position
            _playbackCancellation = new CancellationTokenSource();
            _playbackTask = RunPlaybackLoopAsync(_playbackCancellation.Token);
        }
    }

    public void PreviousWord()
    {
        if (_words.Count == 0) return;

        // Cancel current playback temporarily
        _playbackCancellation?.Cancel();

        _isStopped = false;
        _currentWordIndex--;
        if (_currentWordIndex < 0)
        {
            _currentWordIndex = 0;
        }

        DisplayCurrentWord();
        UpdateProgress();

        if (_isPlaying)
        {
            // Restart playback from new position
            _playbackCancellation = new CancellationTokenSource();
            _playbackTask = RunPlaybackLoopAsync(_playbackCancellation.Token);
        }
    }

    public void NextSection()
    {
        var nextIndex = _playbackEngine.GetNextSectionIndex(_currentWordIndex, _sectionStarts);
        if (nextIndex >= 0)
        {
            _currentWordIndex = nextIndex;
            _isStopped = false;

            var wasPlaying = _isPlaying;
            if (wasPlaying) PauseReading();

            DisplayCurrentWord();
            UpdateProgress();

            if (wasPlaying)
            {
                // Small delay for smooth transition
                Task.Delay(300).ContinueWith(_ => StartReading());
            }
        }
    }

    public void PreviousSection()
    {
        var prevIndex = _playbackEngine.GetPreviousSectionIndex(_currentWordIndex, _sectionStarts);
        _currentWordIndex = prevIndex;
        _isStopped = false;

        var wasPlaying = _isPlaying;
        if (wasPlaying) PauseReading();

        DisplayCurrentWord();
        UpdateProgress();

        if (wasPlaying)
        {
            // Small delay for smooth transition
            Task.Delay(300).ContinueWith(_ => StartReading());
        }
    }

    #endregion

    private void NavigateToWordIndex(int targetIndex)
    {
        if (_words.Count == 0)
        {
            return;
        }

        _playbackCancellation?.Cancel();

        _currentWordIndex = FindNearestDisplayableWordIndex(targetIndex);
        _isStopped = false;

        DisplayCurrentWord();
        UpdateProgress();

        if (_isPlaying)
        {
            _playbackCancellation = new CancellationTokenSource();
            _playbackTask = RunPlaybackLoopAsync(_playbackCancellation.Token);
        }
    }

    private int FindNearestDisplayableWordIndex(int targetIndex)
    {
        if (_words.Count == 0)
        {
            return 0;
        }

        var clampedIndex = Math.Clamp(targetIndex, 0, _words.Count - 1);
        if (!string.IsNullOrEmpty(_words[clampedIndex]))
        {
            return clampedIndex;
        }

        for (var index = clampedIndex + 1; index < _words.Count; index++)
        {
            if (!string.IsNullOrEmpty(_words[index]))
            {
                return index;
            }
        }

        for (var index = clampedIndex - 1; index >= 0; index--)
        {
            if (!string.IsNullOrEmpty(_words[index]))
            {
                return index;
            }
        }

        return clampedIndex;
    }

    #region Speed Control

    public void IncreaseSpeed()
    {
        _playbackEngine.IncreaseSpeed();
        _currentSpeed = _playbackEngine.WordsPerMinute;
        SpeedChanged?.Invoke(this, _playbackEngine.WordsPerMinute);
        if (_isPlaying)
        {
            RestartPlayback();
        }
    }

    public void DecreaseSpeed()
    {
        _playbackEngine.DecreaseSpeed();
        _currentSpeed = _playbackEngine.WordsPerMinute;
        SpeedChanged?.Invoke(this, _playbackEngine.WordsPerMinute);
        if (_isPlaying)
        {
            RestartPlayback();
        }
    }

    private void RestartPlayback()
    {
        if (!_isPlaying) return;

        // Cancel current playback and restart with new speed
        _playbackCancellation?.Cancel();
        _playbackCancellation = new CancellationTokenSource();
        _playbackTask = RunPlaybackLoopAsync(_playbackCancellation.Token);
        _currentSpeed = _playbackEngine.WordsPerMinute;
        SpeedChanged?.Invoke(this, _playbackEngine.WordsPerMinute);
    }

    #endregion

    #region Display Updates

    public void DisplayCurrentWord()
    {
        // Fallback to old word system if new system is empty
        if (_currentWords.Count == 0 && _words.Count > 0)
        {
            DisplayCurrentWordLegacy();
            return;
        }

        if (_currentWordIndex >= _currentWords.Count || _currentWords.Count == 0)
            return;

        var currentWord = _currentWords[_currentWordIndex];
        var currentColor = "";
        var segmentIndex = 0;
        if (_processedScript != null &&
            _processedScript.WordToSegmentMap.TryGetValue(_currentWordIndex, out var segIdx) &&
            segIdx >= 0)
        {
            segmentIndex = segIdx;
        }

        var phrase = _playbackEngine.GetPhraseForWord(_currentWordIndex);
        var phraseContext = phrase;
        if (currentWord.Metadata.IsPause)
        {
            phraseContext = _playbackEngine.GetPhraseForWord(Math.Max(0, _currentWordIndex - 1)) ?? phrase;
        }

        var phraseWords = phraseContext?.Words ?? Array.Empty<string>();
        var phraseDuration = phraseContext?.EstimatedDurationMs ?? 0;
        var phrasePause = phraseContext?.PauseAfterMs ?? 0;
        var phraseHasPauseCue = phraseContext?.ContainsPauseCue ?? false;
        var upcomingEmotion = GetUpcomingEmotionHint(phraseContext);

        var phraseStartIndex = phraseContext?.StartWordIndex ?? -1;
        var isNewPhrase = !currentWord.Metadata.IsPause && phraseStartIndex >= 0 && phraseStartIndex != _lastHeadCuePhraseStartIndex;

        if (isNewPhrase)
        {
            string cueId;
            if (!string.IsNullOrWhiteSpace(currentWord.Metadata.HeadCue))
            {
                cueId = currentWord.Metadata.HeadCue!;
            }
            else if (_phraseHeadCuePlan.TryGetValue(phraseStartIndex, out var plannedCue))
            {
                cueId = plannedCue;
            }
            else
            {
                var phraseEmotion = !string.IsNullOrWhiteSpace(phraseContext?.EmotionHint)
                    ? phraseContext!.EmotionHint
                    : (_processedScript != null && segmentIndex >= 0 && segmentIndex < _processedScript.Segments.Count
                        ? _processedScript.Segments[segmentIndex].Emotion
                        : _currentEmotion);
                cueId = HeadCueCatalog.ResolveForEmotion(phraseEmotion);
            }

            _currentHeadCueId = cueId;
            _lastHeadCuePhraseStartIndex = phraseStartIndex;
        }
        else if (_lastHeadCuePhraseStartIndex == -1 && !string.IsNullOrWhiteSpace(currentWord.Metadata.HeadCue))
        {
            _currentHeadCueId = currentWord.Metadata.HeadCue!;
        }

        // Handle pause
        if (currentWord.Metadata.IsPause)
        {
            RaiseWordDisplay("", "⏸", "",
                GetContextWord(-5), GetContextWord(-4), GetContextWord(-3), GetContextWord(-2), GetContextWord(-1),
                GetContextWord(1), GetContextWord(2), GetContextWord(3), GetContextWord(4), GetContextWord(5),
                "",
                phraseWords,
                phraseDuration,
                phrasePause,
                phraseHasPauseCue,
                true,
                upcomingEmotion,
                _currentHeadCueId);
            return;
        }

        // Split word at ORP using the pre-calculated position
        var text = currentWord.CleanText;
        var orpPos = currentWord.ORPPosition;

        var preOrp = orpPos > 0 ? text.Substring(0, orpPos) : "";
        var orpChar = orpPos < text.Length ? text[orpPos].ToString() : "";
        var postOrp = orpPos + 1 < text.Length ? text.Substring(orpPos + 1) : "";

        // Use color from metadata
        if (!string.IsNullOrEmpty(currentWord.Metadata.Color))
        {
            currentColor = currentWord.Metadata.Color;
        }

        // Get context words (5 on each side)
        var left5 = GetContextWord(-5);
        var left4 = GetContextWord(-4);
        var left3 = GetContextWord(-3);
        var left2 = GetContextWord(-2);
        var left1 = GetContextWord(-1);
        var right1 = GetContextWord(1);
        var right2 = GetContextWord(2);
        var right3 = GetContextWord(3);
        var right4 = GetContextWord(4);
        var right5 = GetContextWord(5);

        // Determine emotion and color from script metadata
        if (_processedScript != null)
        {
            // Check for color override
            if (_processedScript.WordColorOverrides != null &&
                _processedScript.WordColorOverrides.TryGetValue(_currentWordIndex, out var wordColor))
            {
                currentColor = wordColor;
                System.Diagnostics.Debug.WriteLine($"Found color override for word index {_currentWordIndex}: {wordColor}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"No color override for word index {_currentWordIndex}");
            }
            
            // Check for emotion override
            if (_processedScript.WordEmotionOverrides.TryGetValue(_currentWordIndex, out var wordEmotion))
            {
                if (_currentEmotion != wordEmotion)
                {
                    _currentEmotion = wordEmotion;
                    RaiseEmotionChanged(_currentEmotion);
                }
            }
            else if (_processedScript.WordToSegmentMap.TryGetValue(_currentWordIndex, out var segIndex) &&
                     segIndex >= 0 && segIndex < _processedScript.Segments.Count)
            {
                var segEmotion = _processedScript.Segments[segIndex].Emotion;
                if (_currentEmotion != segEmotion)
                {
                    _currentEmotion = segEmotion;
                    RaiseEmotionChanged(_currentEmotion);
                }
            }
        }

        // Raise display update event with current emotion and color
        RaiseWordDisplay(preOrp, orpChar, postOrp,
            left5, left4, left3, left2, left1,
            right1, right2, right3, right4, right5,
            currentColor,
            phraseWords,
            phraseDuration,
            phrasePause,
                phraseHasPauseCue,
                false,
                upcomingEmotion,
                _currentHeadCueId);
    }

    private string GetContextWord(int offset)
    {
        var index = _currentWordIndex + offset;
        if (index >= 0 && index < _currentWords.Count)
        {
            var word = _currentWords[index];
            if (word.Metadata.IsPause)
                return "";
            return word.CleanText;
        }
        return "";
    }

    private void DisplayCurrentWordLegacy()
    {
        if (_currentWordIndex >= _words.Count || _words.Count == 0)
            return;

        var currentWord = _words[_currentWordIndex];
        var currentColor = "";

        var phrase = _playbackEngine.GetPhraseForWord(_currentWordIndex);
        var phraseContext = string.IsNullOrEmpty(currentWord)
            ? (_playbackEngine.GetPhraseForWord(Math.Max(0, _currentWordIndex - 1)) ?? phrase)
            : phrase;
        var phraseWords = phraseContext?.Words ?? Array.Empty<string>();
        var phraseDuration = phraseContext?.EstimatedDurationMs ?? 0;
        var phrasePause = phraseContext?.PauseAfterMs ?? 0;
        var phraseHasPauseCue = phraseContext?.ContainsPauseCue ?? false;
        var upcomingEmotion = GetUpcomingEmotionHint(phraseContext);

        // Handle pause
        if (string.IsNullOrEmpty(currentWord))
        {
            RaiseWordDisplay("", "⏸", "",
                GetLegacyContextWord(-5), GetLegacyContextWord(-4), GetLegacyContextWord(-3), GetLegacyContextWord(-2), GetLegacyContextWord(-1),
                GetLegacyContextWord(1), GetLegacyContextWord(2), GetLegacyContextWord(3), GetLegacyContextWord(4), GetLegacyContextWord(5),
                "",
                phraseWords,
                phraseDuration,
                phrasePause,
                phraseHasPauseCue,
                true,
                upcomingEmotion);
            return;
        }

        // Calculate ORP
        var orpPos = _orpCalculator.CalculateOrpIndex(currentWord);
        var preOrp = orpPos > 0 ? currentWord.Substring(0, orpPos) : "";
        var orpChar = orpPos < currentWord.Length ? currentWord[orpPos].ToString() : "";
        var postOrp = orpPos + 1 < currentWord.Length ? currentWord.Substring(orpPos + 1) : "";

        // Check for color override in processed script
        if (_processedScript != null)
        {
            if (_processedScript.WordColorOverrides != null &&
                _processedScript.WordColorOverrides.TryGetValue(_currentWordIndex, out var wordColor))
            {
                currentColor = wordColor;
            }

            // Check for emotion override
            if (_processedScript.WordEmotionOverrides.TryGetValue(_currentWordIndex, out var wordEmotion))
            {
                if (_currentEmotion != wordEmotion)
                {
                    _currentEmotion = wordEmotion;
                    RaiseEmotionChanged(_currentEmotion);
                }
            }
        }

        // Get context words
        var left5 = GetLegacyContextWord(-5);
        var left4 = GetLegacyContextWord(-4);
        var left3 = GetLegacyContextWord(-3);
        var left2 = GetLegacyContextWord(-2);
        var left1 = GetLegacyContextWord(-1);
        var right1 = GetLegacyContextWord(1);
        var right2 = GetLegacyContextWord(2);
        var right3 = GetLegacyContextWord(3);
        var right4 = GetLegacyContextWord(4);
        var right5 = GetLegacyContextWord(5);

        _currentHeadCueId = HeadCueCatalog.ResolveForEmotion(_currentEmotion);

        RaiseWordDisplay(preOrp, orpChar, postOrp,
            left5, left4, left3, left2, left1,
            right1, right2, right3, right4, right5,
            currentColor,
            phraseWords,
            phraseDuration,
            phrasePause,
            phraseHasPauseCue,
            false,
            upcomingEmotion,
            _currentHeadCueId);
    }

    private string GetLegacyContextWord(int offset)
    {
        var index = _currentWordIndex + offset;
        if (index >= 0 && index < _words.Count)
        {
            var word = _words[index];
            if (string.IsNullOrEmpty(word))
                return "";
            return word;
        }
        return "";
    }
    
    private void CheckUpcomingEmotions()
    {
        // Look ahead 3 words for emotion triggers
        for (int i = 1; i <= 3; i++)
        {
            var upcomingWord = GetContextWord(i);
            if (!string.IsNullOrEmpty(upcomingWord))
            {
                var potentialEmotion = _emotionAnalyzer.AnalyzeWord(upcomingWord);
                if (potentialEmotion != null)
                {
                    // Start transitioning early with reduced intensity based on distance
                    // This creates the anticipation effect
                    var transitionStrength = 1.0f - (i * 0.25f); // 75%, 50%, 25% strength
                    // Note: This would need more complex implementation to blend colors
                    // For now, the animation duration creates the smooth effect
                    break;
                }
            }
        }
    }

    private void UpdateProgress()
    {
        var progress = _playbackEngine.CalculateProgress(_currentWordIndex, _words.Count);
        var timeRemaining = _playbackEngine.EstimateTimeRemaining(_currentWordIndex, _words);
        var timeElapsed = DateTime.Now - _startTime;
        
        RaiseProgressUpdate(_currentWordIndex, _words.Count, progress, timeRemaining, timeElapsed);
    }

    #endregion

    #region Async Playback Loop

    private async Task RunPlaybackLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _isPlaying && _currentWordIndex < _words.Count)
            {
                var currentWord = _words[_currentWordIndex];
                
                // Check for speed/emotion changes from processed script
                if (_processedScript != null)
                {
                    // Check if there's a speed override for this word
                    if (!_ignoreScriptSpeeds &&
                        _processedScript.WordSpeedOverrides.TryGetValue(_currentWordIndex, out var wordSpeed))
                    {
                        if (wordSpeed != _currentSpeed)
                        {
                            _currentSpeed = wordSpeed;
                            _playbackEngine.WordsPerMinute = _currentSpeed;
                            SpeedChanged?.Invoke(this, _currentSpeed);
                        }
                    }
                    else if (_ignoreScriptSpeeds && _playbackEngine.WordsPerMinute != _currentSpeed)
                    {
                        _playbackEngine.WordsPerMinute = _currentSpeed;
                    }
                    
                    // Check if there's an emotion override for this word
                    if (_processedScript.WordEmotionOverrides.TryGetValue(_currentWordIndex, out var wordEmotion))
                    {
                        if (wordEmotion != _currentEmotion)
                        {
                            _currentEmotion = wordEmotion;
                            RaiseEmotionChanged(wordEmotion);
                        }
                    }
                }
                
                // Display the current word (or pause symbol)
                DisplayCurrentWord();
                
                // Calculate display time
                int displayTime;
                
                // Check if this is a pause (empty string)
                if (string.IsNullOrEmpty(currentWord))
                {
                    var phrasePause = _playbackEngine.GetPauseAfterMilliseconds(Math.Max(0, _currentWordIndex - 1));
                    if (phrasePause.HasValue)
                    {
                        displayTime = phrasePause.Value;
                    }
                    else if (_processedScript?.PauseDurations != null &&
                             _processedScript.PauseDurations.TryGetValue(_currentWordIndex, out var scriptedPause))
                    {
                        displayTime = scriptedPause;
                    }
                    else
                    {
                        var baseInterval = 60000 / Math.Max(1, _playbackEngine.WordsPerMinute);
                        displayTime = (int)(baseInterval * 3);
                    }
                }
                else
                {
                    displayTime = (int)_playbackEngine.GetWordDisplayTime(_currentWordIndex, currentWord).TotalMilliseconds;
                }
                
                // Wait for the display time
                await Task.Delay(displayTime, cancellationToken);
                
                if (cancellationToken.IsCancellationRequested || !_isPlaying)
                    break;
                
                // Move to next word
                _currentWordIndex++;
                
                if (_currentWordIndex >= _words.Count)
                {
                    _currentWordIndex = _words.Count - 1;
                    StopReading();
                    break;
                }
                
                UpdateProgress();
            }
        }
        catch (TaskCanceledException)
        {
            // Normal cancellation, no need to handle
        }
    }

    #endregion

    #region Event Raising

    private void RaiseWordDisplay(string preOrp, string orpChar, string postOrp,
        string left5, string left4, string left3, string left2, string left1,
        string right1, string right2, string right3, string right4, string right5,
        string wordColor = "",
        IReadOnlyList<string>? phraseWords = null,
        int phraseDurationMs = 0,
        int phrasePauseMs = 0,
        bool phraseHasPauseCue = false,
        bool isPause = false,
        string? upcomingEmotion = null,
        string? headCueId = null)
    {
        // Emotion is determined by script metadata in _currentEmotion
        var (emoji, colorHex) = MapEmotionDisplay(_currentEmotion);
        var upcoming = MapEmotionDisplay(upcomingEmotion);
        System.Diagnostics.Debug.WriteLine($"RaiseWordDisplay - wordIndex={_currentWordIndex}, segment={CurrentSegmentIndex}/{TotalSegments}, emotion='{_currentEmotion}', color='{wordColor}'");
        
        WordDisplayUpdate?.Invoke(this, new Rsvp.WordDisplayEventArgs
        {
            PreORP = preOrp,
            OrpChar = orpChar,
            PostORP = postOrp,
            LeftWord5 = left5,
            LeftWord4 = left4,
            LeftWord3 = left3,
            LeftWord2 = left2,
            LeftWord1 = left1,
            RightWord1 = right1,
            RightWord2 = right2,
            RightWord3 = right3,
            RightWord4 = right4,
            RightWord5 = right5,
            CurrentWordIndex = _currentWordIndex,
            TotalWords = _words.Count,
            EmotionName = _currentEmotion,
            EmotionEmoji = emoji,
            EmotionColor = Windows.UI.Color.FromArgb(255, 
                Convert.ToByte(colorHex.Substring(1, 2), 16),
                Convert.ToByte(colorHex.Substring(3, 2), 16),
                Convert.ToByte(colorHex.Substring(5, 2), 16)),
            EmotionColorHex = colorHex,
            WordColor = wordColor,
            PhraseWords = phraseWords ?? Array.Empty<string>(),
            PhraseEstimatedDurationMs = phraseDurationMs,
            PhrasePauseAfterMs = phrasePauseMs,
            PhraseContainsPauseCue = phraseHasPauseCue,
            IsPause = isPause,
            UpcomingEmotionName = upcomingEmotion ?? string.Empty,
            UpcomingEmotionEmoji = upcoming.emoji,
            UpcomingEmotionColorHex = upcoming.colorHex,
            HeadCueId = string.IsNullOrWhiteSpace(headCueId) ? _currentHeadCueId : headCueId
        });
    }

    private (string emoji, string colorHex) MapEmotionDisplay(string emotionName)
    {
        switch (emotionName?.ToLowerInvariant())
        {
            case "warm": return ("😊", "#FFA500");
            case "concerned": return ("😟", "#FF6B6B");
            case "focused": return ("🎯", "#4A90E2");
            case "motivational": return ("💪", "#9B59B6");
            case "energetic": return ("⚡", "#FFD700");
            case "urgent": return ("🚨", "#EF4444");
            case "happy": return ("😊", "#FFD700");
            case "excited": return ("🎉", "#FF6B6B");
            case "calm": return ("😌", "#4ECDC4");
            case "sad": return ("😢", "#95A5C6");
            case "angry": return ("😠", "#E74C3C");
            case "fear": return ("😨", "#8E44AD");
            case "professional": return ("💼", "#34495E");
            case "peaceful": return ("🕊️", "#27AE60");
            case "melancholy": return ("🌧️", "#7F8C8D");
            default: return ("😐", "#808080");
        }
    }

    private string? GetUpcomingEmotionHint(RsvpTextProcessor.PhraseGroup? currentPhrase)
    {
        if (_processedScript == null || _processedScript.PhraseGroups.Count == 0)
        {
            return null;
        }

        if (currentPhrase != null)
        {
            for (int i = 0; i < _processedScript.PhraseGroups.Count; i++)
            {
                if (_processedScript.PhraseGroups[i].StartWordIndex == currentPhrase.StartWordIndex)
                {
                    if (i + 1 < _processedScript.PhraseGroups.Count)
                    {
                        return _processedScript.PhraseGroups[i + 1].EmotionHint;
                    }

                    return null;
                }
            }
        }

        var next = _processedScript.PhraseGroups.FirstOrDefault(pg => pg.StartWordIndex > _currentWordIndex);
        return next?.EmotionHint;
    }

    private void BuildHeadCuePlan()
    {
        _phraseHeadCuePlan.Clear();
        _lastHeadCuePhraseStartIndex = -1;
        _currentHeadCueId = HeadCueCatalog.Neutral.Id;

        if (_processedScript == null || _processedScript.PhraseGroups.Count == 0)
        {
            return;
        }

        var segmentPhraseCounters = new Dictionary<int, int>();

        foreach (var phrase in _processedScript.PhraseGroups.OrderBy(pg => pg.StartWordIndex))
        {
            var segmentIndex = 0;
            if (_processedScript.WordToSegmentMap.TryGetValue(phrase.StartWordIndex, out var segIndex) && segIndex >= 0)
            {
                segmentIndex = segIndex;
            }

            var segmentEmotion = (segmentIndex >= 0 && segmentIndex < _processedScript.Segments.Count)
                ? _processedScript.Segments[segmentIndex].Emotion
                : "neutral";

            var phraseEmotion = !string.IsNullOrWhiteSpace(phrase.EmotionHint)
                ? phrase.EmotionHint
                : segmentEmotion;

            var pattern = SelectHeadCuePattern(phraseEmotion, segmentIndex);
            var step = segmentPhraseCounters.TryGetValue(segmentIndex, out var count) ? count : 0;
            var cueId = pattern[step % pattern.Length];

            segmentPhraseCounters[segmentIndex] = step + 1;
            _phraseHeadCuePlan[phrase.StartWordIndex] = cueId;
        }

        if (_phraseHeadCuePlan.Count > 0)
        {
            var firstCue = _phraseHeadCuePlan.OrderBy(kvp => kvp.Key).First().Value;
            _currentHeadCueId = firstCue;
        }
    }

    private string[] SelectHeadCuePattern(string? emotion, int segmentIndex)
    {
        if (!string.IsNullOrWhiteSpace(emotion) &&
            EmotionCuePatterns.TryGetValue(emotion, out var pattern) && pattern.Length > 0)
        {
            return pattern;
        }

        return DefaultCuePatterns[segmentIndex % DefaultCuePatterns.Length];
    }


    private void RaiseEmotionChanged(string emotionName)
    {
        // Map emotion names to display data
        var (emoji, colorHex) = emotionName.ToLower() switch
        {
            "warm" => ("😊", "#FFA500"),
            "concerned" => ("😟", "#FF6B6B"),
            "focused" => ("🎯", "#4A90E2"),
            "energetic" => ("⚡", "#FFD700"),
            "motivational" => ("💪", "#9B59B6"),
            _ => ("😐", "#808080") // neutral
        };
        
        EmotionChanged?.Invoke(this, new Rsvp.EmotionChangeEventArgs
        {
            EmotionName = emotionName,
            EmotionEmoji = emoji,
            ColorHex = colorHex
        });
    }
    
    private void RaiseEmotionChanged(string name, string emoji, string colorHex)
    {
        EmotionChanged?.Invoke(this, new Rsvp.EmotionChangeEventArgs
        {
            EmotionName = name,
            EmotionEmoji = emoji,
            ColorHex = colorHex
        });
    }

    private void RaisePlaybackStateChanged()
    {
        PlaybackStateChanged?.Invoke(this, new Rsvp.PlaybackStateEventArgs
        {
            IsPlaying = _isPlaying,
            IsStopped = _isStopped
        });
    }

    private void RaiseProgressUpdate(int currentIndex, int totalWords, double percentage, TimeSpan remaining, TimeSpan elapsed)
    {
        ProgressUpdate?.Invoke(this, new Rsvp.ProgressEventArgs
        {
            CurrentWordIndex = currentIndex,
            TotalWords = totalWords,
            ProgressPercentage = percentage,
            TimeRemaining = remaining,
            TimeElapsed = elapsed
        });
    }

    #endregion
}
