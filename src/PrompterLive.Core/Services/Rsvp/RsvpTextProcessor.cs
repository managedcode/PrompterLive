using System.Text.RegularExpressions;

namespace PrompterLive.Core.Services.Rsvp;

/// <summary>
/// Processes text for RSVP display following Spritz methodology
/// Handles word splitting, pause insertion, segment parsing, and text preprocessing
/// </summary>
public class RsvpTextProcessor
{
    private const int DefaultSegmentSpeed = 250;
    private const double ActorPacingMultiplier = 1.35; // slower, more expressive cadence than pure speed reading
    private const int MinimumPhraseDurationMs = 450;
    private const int MinimumWordDurationMs = 180;
    private const int DefaultPauseMs = 400;
    private const int LongPauseMs = 800;

    public class ProcessedSegment
    {
        public string Title { get; set; } = "";
        public string Emotion { get; set; } = "neutral";
        public int Speed { get; set; } = DefaultSegmentSpeed;
        public List<string> Words { get; set; } = new();
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
    }

    public class ProcessedScript
    {
        public List<ProcessedSegment> Segments { get; set; } = new();
        public List<string> AllWords { get; set; } = new();
        public Dictionary<int, int> WordToSegmentMap { get; set; } = new();
        public Dictionary<int, int> WordSpeedOverrides { get; set; } = new();
        public Dictionary<int, string> WordEmotionOverrides { get; set; } = new();
        public Dictionary<int, string> WordColorOverrides { get; set; } = new();
        public Dictionary<int, int> PauseDurations { get; set; } = new(); // Pause duration in ms for empty word indices
        public List<PhraseGroup> PhraseGroups { get; set; } = new();
        public Dictionary<int, string> UpcomingEmotionByStartIndex { get; set; } = new();
    }

    public class PhraseGroup
    {
        public int StartWordIndex { get; set; }
        public int EndWordIndex { get; set; }
        public IReadOnlyList<string> Words { get; set; } = Array.Empty<string>();
        public int EstimatedDurationMs { get; set; }
        public int PauseAfterMs { get; set; }
        public string EmotionHint { get; set; } = "neutral";
        public bool ContainsPauseCue { get; set; }
        public bool ContainsEmphasis { get; set; }
    }
    /// <summary>
    /// Parses script content with segments, emotions, speed metadata
    /// Supports both TPS format and simple text with segment markers
    /// </summary>
    /// <param name="content">Script content with segment markers or TPS format</param>
    /// <returns>Processed script with segments and metadata</returns>
    public ProcessedScript ParseScript(string content)
    {
        // Strip YAML front matter (--- ... ---) if present
        content = StripFrontMatter(content);
        content = NormalizeLineEndings(content);
        var result = new ProcessedScript();

        if (string.IsNullOrEmpty(content))
        {
            // Return empty script with default segment
            result.Segments.Add(new ProcessedSegment
            {
                Title = "Default",
                Words = new List<string>(),
                StartIndex = 0,
                EndIndex = 0
            });
            return result;
        }

        // Check if content is in TPS format
        if (IsTpsFormat(content))
        {
            return ParseTpsScript(content);
        }

        // Fallback to simple parsing
        var lines = content.Split('\n');
        ProcessedSegment? currentSegment = null;
        var allWords = new List<string>();
        var currentEmotion = "neutral";
        var currentSpeed = 250;

        foreach (var line in lines)
        {
            // Check for segment header
            if (line.StartsWith("## "))
            {
                // Save previous segment if exists
                if (currentSegment != null)
                {
                    currentSegment.EndIndex = allWords.Count - 1;
                    result.Segments.Add(currentSegment);
                }

                // Parse TPS segment header format: ## [Name|WPM|Emotion|Time]
                var segmentMatch = Regex.Match(line, @"##\s*\[([^\|]+)\|([^\|]+)\|([^\|]+)\|([^\]]+)\]");
                if (segmentMatch.Success)
                {
                    var title = segmentMatch.Groups[1].Value.Trim();
                    var wpmRange = segmentMatch.Groups[2].Value.Trim();
                    var emotion = segmentMatch.Groups[3].Value.Trim();

                    // Parse WPM range (e.g., "250-300WPM")
                    var wpmMatch = Regex.Match(wpmRange, @"(\d+)");
                    if (wpmMatch.Success && int.TryParse(wpmMatch.Groups[1].Value, out var wpm))
                    {
                        currentSpeed = wpm;
                    }

                    currentEmotion = emotion;
                    currentSegment = new ProcessedSegment
                    {
                        Title = title,
                        Emotion = currentEmotion,
                        Speed = currentSpeed,
                        StartIndex = allWords.Count
                    };
                }
                else
                {
                    // Simple segment header
                    currentSegment = new ProcessedSegment
                    {
                        Title = line.Substring(3).Trim(),
                        Emotion = currentEmotion,
                        Speed = currentSpeed,
                        StartIndex = allWords.Count
                    };
                }
                continue;
            }

            // Parse block header format: ### [Name|WPM|Emotion]
            if (line.StartsWith("### "))
            {
                var blockMatch = Regex.Match(line, @"###\s*\[([^\|\]]+)\|([^\|\]]+)(?:\|([^\]]+))?\]");
                if (blockMatch.Success)
                {
                    var wpmStr = blockMatch.Groups[2].Value.Trim();
                    var wpmNumMatch = Regex.Match(wpmStr, @"(\d+)");
                    if (wpmNumMatch.Success && int.TryParse(wpmNumMatch.Groups[1].Value, out var blockWpm))
                    {
                        currentSpeed = blockWpm;
                    }

                    if (blockMatch.Groups[3].Success && !string.IsNullOrEmpty(blockMatch.Groups[3].Value))
                    {
                        currentEmotion = blockMatch.Groups[3].Value.Trim();
                    }
                }
                continue;
            }

            // Parse emotion metadata
            var emotionMatch = Regex.Match(line, @"\[emotion:\s*(\w+)\]");
            if (emotionMatch.Success)
            {
                currentEmotion = emotionMatch.Groups[1].Value;
                currentSegment?.Emotion = currentEmotion;
            }

            // Parse speed metadata
            var speedMatch = Regex.Match(line, @"\[speed:\s*(\d+)\]");
            if (speedMatch.Success && int.TryParse(speedMatch.Groups[1].Value, out var speed))
            {
                currentSpeed = speed;
                currentSegment?.Speed = currentSpeed;
            }

            // Process line with inline tags preserved
            var (cleanLine, lineMetadata) = ProcessLineWithTags(line);

            // Process words from cleaned line
            if (!string.IsNullOrWhiteSpace(cleanLine))
            {
                // If no segment yet, create default
                if (currentSegment == null)
                {
                    currentSegment = new ProcessedSegment
                    {
                        Title = "Main Content",
                        Emotion = currentEmotion,
                        Speed = currentSpeed,
                        StartIndex = allWords.Count
                    };
                }

                var words = ProcessLine(cleanLine);
                var wordPosInLine = 0;
                foreach (var word in words)
                {
                    allWords.Add(word);
                    currentSegment.Words.Add(word);

                    var wordIndex = allWords.Count - 1;

                    // Map word index to segment index
                    result.WordToSegmentMap[wordIndex] = result.Segments.Count;

                    // Check for inline metadata for this word position
                    if (lineMetadata != null && wordPosInLine < lineMetadata.Count)
                    {
                        var meta = lineMetadata[wordPosInLine];

                        // Apply inline speed override if present
                        if (meta.Speed.HasValue)
                        {
                            result.WordSpeedOverrides[wordIndex] = meta.Speed.Value;
                        }
                        else
                        {
                            result.WordSpeedOverrides[wordIndex] = currentSpeed;
                        }

                        // Apply inline emotion override if present
                        if (!string.IsNullOrEmpty(meta.Emotion))
                        {
                            result.WordEmotionOverrides[wordIndex] = meta.Emotion;
                        }
                        else
                        {
                            result.WordEmotionOverrides[wordIndex] = currentEmotion;
                        }

                        // Apply inline color if present
                        if (!string.IsNullOrEmpty(meta.Color))
                        {
                            result.WordColorOverrides[wordIndex] = meta.Color;
                        }
                    }
                    else
                    {
                        // No inline metadata, use current defaults
                        result.WordSpeedOverrides[wordIndex] = currentSpeed;
                        result.WordEmotionOverrides[wordIndex] = currentEmotion;
                    }

                    wordPosInLine++;
                }
            }
        }

        // Add last segment
        if (currentSegment != null)
        {
            currentSegment.EndIndex = allWords.Count - 1;
            result.Segments.Add(currentSegment);
        }

        // If no segments were created, add default
        if (result.Segments.Count == 0)
        {
            result.Segments.Add(new ProcessedSegment
            {
                Title = "Default",
                Emotion = "neutral",
                Speed = 250,
                Words = allWords,
                StartIndex = 0,
                EndIndex = Math.Max(0, allWords.Count - 1)
            });
        }

        result.AllWords = allWords;
        FinalizeProcessedScript(result);
        return result;
    }

    /// <summary>
    /// Removes YAML-style front matter block from the top of the document
    /// </summary>
    private static string StripFrontMatter(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }
        // (?s) enables singleline to allow dot to match newlines
        return Regex.Replace(text, @"(?s)^\s*---\s*.*?---\s*", string.Empty);
    }

    /// <summary>
    /// Process a single line of text into words with phrase markers
    /// </summary>
    private static List<string> ProcessLine(string line)
    {
        var words = new List<string>();

        // Skip technical markers completely
        if (IsTechnicalMarker(line))
        {
            return words; // Return empty list for technical markers
        }

        // Remove pause tags before splitting
        line = Regex.Replace(line, @"\[PAUSE\]", "", RegexOptions.IgnoreCase);
        line = Regex.Replace(line, @"\[pause(?::[^\]]+)?\]", "", RegexOptions.IgnoreCase);

        // Split on whitespace but preserve emphasis markers
        var tokens = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in tokens)
        {
            // Skip PAUSE markers
            if (token == "[PAUSE]" || token.StartsWith("[pause"))
            {
                continue;
            }

            // Handle micro/phrase pauses '/' and '//': add pauses but do not display token
            if (token == "/")
            {
                words.Add("");
                continue;
            }
            if (token == "//")
            {
                words.Add("");
                words.Add("");
                continue;
            }

            words.Add(token);

            // Add pauses after sentence-ending punctuation
            if (token.Any(".!?".Contains))
            {
                // Add empty strings as pause markers
                words.Add("");
                words.Add("");
            }
        }

        return words;
    }

    /// <summary>
    /// Legacy method - preprocesses plain text for RSVP display
    /// </summary>
    /// <param name="text">Input text</param>
    /// <returns>List of processed words ready for RSVP display</returns>
    public List<string> PreprocessText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new List<string>();
        }

        // Split on whitespace
        var words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var processedWords = new List<string>();

        foreach (var word in words)
        {
            // Add each word only ONCE - timing handled separately
            processedWords.Add(word);

            // Add pauses after sentence-ending punctuation
            if (word.Any(".!?".Contains))
            {
                // Add empty strings as pause markers for longer pause
                processedWords.Add("");
                processedWords.Add("");
            }
        }

        return processedWords;
    }

    /// <summary>
    /// Finds section starts (sentence boundaries) in word list
    /// </summary>
    /// <param name="words">List of words</param>
    /// <returns>List of indices where sections start</returns>
    public List<int> FindSectionStarts(List<string> words)
    {
        var sectionStarts = new List<int>();
        sectionStarts.Add(0); // First word is always start of first section

        for (var i = 0; i < words.Count - 1; i++)
        {
            var word = words[i];

            // If current word ends sentence, next non-empty word starts new section
            if (!string.IsNullOrEmpty(word) && word.Any(".!?".Contains))
            {
                // Find next non-empty word
                var nextWordIndex = i + 1;
                while (nextWordIndex < words.Count && string.IsNullOrEmpty(words[nextWordIndex]))
                {
                    nextWordIndex++;
                }

                if (nextWordIndex < words.Count)
                {
                    sectionStarts.Add(nextWordIndex);
                }
            }
        }

        return sectionStarts;
    }

    /// <summary>
    /// Checks if word is important (capitalized, contains emphasis markers)
    /// </summary>
    public bool IsImportantWord(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return false;
        }

        // All caps words are important
        if (word.Length > 2 && word == word.ToUpper())
        {
            return true;
        }

        // Words with emphasis punctuation
        if (word.Contains('!') || word.Contains('?') || word.Contains(':'))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if word is short (3 characters or less)
    /// </summary>
    public bool IsShortWord(string word)
    {
        return !string.IsNullOrEmpty(word) && word.Length <= 3;
    }

    /// <summary>
    /// Checks if word has punctuation that affects timing
    /// </summary>
    public bool HasPunctuation(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return false;
        }

        return word.Any(",.;:!?".Contains);
    }

    /// <summary>
    /// Checks if content is in TPS format
    /// </summary>
    private static bool IsTpsFormat(string content)
    {
        // Check for TPS-specific markers
        return content.Contains("## [") || content.Contains("### [") ||
               content.Contains("[emphasis]") || content.Contains("[slow]") ||
               content.Contains("[pause") || Regex.IsMatch(content, @"\[\d+WPM\]");
    }

    /// <summary>
    /// Word metadata for inline formatting
    /// </summary>
    private sealed class WordMetadata
    {
        public int? Speed { get; set; }
        public string? Emotion { get; set; }
        public string? Color { get; set; }
    }

    /// <summary>
    /// Process a line and extract inline formatting metadata
    /// </summary>
    private static (string cleanLine, List<WordMetadata>? metadata) ProcessLineWithTags(string line)
    {
        var metadata = new List<WordMetadata>();
        var currentColor = "";
        var currentInlineSpeed = (int?)null;
        var currentInlineEmotion = "";

        // Process inline tags and build metadata for each word
        var processedLine = line;

        // First handle paired color tags: [blue]text[/blue]
        var pairedColorPattern = @"\[(blue|red|green|yellow|purple|orange|cyan|magenta|white|gray|black)\]([^\[]*?)\[\/(blue|red|green|yellow|purple|orange|cyan|magenta|white|gray|black)\]";
        processedLine = Regex.Replace(processedLine, pairedColorPattern, (match) =>
        {
            var color = match.Groups[1].Value;
            var text = match.Groups[2].Value;
            // Mark this text with color metadata
            return $"{{COLOR:{color}}}{text}{{/COLOR}}";
        }, RegexOptions.IgnoreCase);

        // Handle simple color tags that affect following text: [blue], [red], etc.
        var simpleColorPattern = @"\[(blue|red|green|yellow|purple|orange|cyan|magenta|white|gray|black)\]";
        processedLine = Regex.Replace(processedLine, simpleColorPattern, (match) =>
        {
            var color = match.Groups[1].Value;
            return $"{{SETCOLOR:{color}}}";
        }, RegexOptions.IgnoreCase);

        // Handle paired speed tags: [300WPM]text[/300WPM]
        var pairedSpeedPattern = @"\[(\d+)WPM\]([^\[]*?)\[\/(\d+)WPM\]";
        processedLine = Regex.Replace(processedLine, pairedSpeedPattern, (match) =>
        {
            var speed = match.Groups[1].Value;
            var text = match.Groups[2].Value;
            return $"{{SPEED:{speed}}}{text}{{/SPEED}}";
        }, RegexOptions.IgnoreCase);

        // Handle simple speed tags: [300WPM]
        var simpleSpeedPattern = @"\[(\d+)WPM\]";
        processedLine = Regex.Replace(processedLine, simpleSpeedPattern, (match) =>
        {
            var speed = match.Groups[1].Value;
            return $"{{SETSPEED:{speed}}}";
        }, RegexOptions.IgnoreCase);

        // Extract emotion emojis at start of line - improved pattern
        // Match emoji followed by optional emotion name
        var emojiPattern = @"^([😊😟🎯💪⚡🚨😌😢😠😨💼🕊️🌧️])\s*(\w+)?\s*";
        var emojiMatch = Regex.Match(processedLine, emojiPattern);
        if (emojiMatch.Success)
        {
            var emoji = emojiMatch.Groups[1].Value;
            var emotionName = emojiMatch.Groups[2].Value.ToLower();

            // Map emoji to emotion or use emotion name if provided
            if (emoji == "😊" || emotionName == "warm")
            {
                currentInlineEmotion = "warm";
            }
            else if (emoji == "😟" || emotionName == "concerned")
            {
                currentInlineEmotion = "concerned";
            }
            else if (emoji == "🎯" || emotionName == "focused")
            {
                currentInlineEmotion = "focused";
            }
            else if (emoji == "💪" || emotionName == "motivational")
            {
                currentInlineEmotion = "motivational";
            }
            else if (emoji == "⚡" || emotionName == "energetic")
            {
                currentInlineEmotion = "energetic";
            }
            else if (emoji == "🚨" || emotionName == "urgent")
            {
                currentInlineEmotion = "urgent";
            }
            else if (emoji == "😌" || emotionName == "calm")
            {
                currentInlineEmotion = "calm";
            }
            else if (emoji == "😢" || emotionName == "sad")
            {
                currentInlineEmotion = "sad";
            }
            else if (emoji == "😠" || emotionName == "angry")
            {
                currentInlineEmotion = "angry";
            }
            else if (emoji == "😨" || emotionName == "fear")
            {
                currentInlineEmotion = "fear";
            }
            else if (emoji == "💼" || emotionName == "professional")
            {
                currentInlineEmotion = "professional";
            }
            else if (emoji == "🕊️" || emotionName == "peaceful")
            {
                currentInlineEmotion = "peaceful";
            }
            else if (emoji == "🌧️" || emotionName == "melancholy")
            {
                currentInlineEmotion = "melancholy";
            }

            // Remove emoji and emotion name from line
            processedLine = processedLine.Substring(emojiMatch.Length);
        }

        // Now process the line word by word and build metadata
        var cleanWords = new List<string>();
        var tokens = processedLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in tokens)
        {
            var meta = new WordMetadata();
            var cleanToken = token;

            // Check for SETCOLOR marker (simple color tag)
            if (cleanToken.Contains("{SETCOLOR:"))
            {
                var setColorMatch = Regex.Match(cleanToken, @"\{SETCOLOR:([^}]+)\}");
                if (setColorMatch.Success)
                {
                    currentColor = setColorMatch.Groups[1].Value;
                    cleanToken = cleanToken.Replace(setColorMatch.Value, "");
                }
            }

            // Check for paired color markers
            if (cleanToken.StartsWith("{COLOR:"))
            {
                var colorMatch = Regex.Match(cleanToken, @"\{COLOR:([^}]+)\}");
                if (colorMatch.Success)
                {
                    currentColor = colorMatch.Groups[1].Value;
                    cleanToken = cleanToken.Replace(colorMatch.Value, "");
                }
            }
            if (cleanToken.EndsWith("{/COLOR}"))
            {
                cleanToken = cleanToken.Replace("{/COLOR}", "");
                meta.Color = currentColor;
                currentColor = "";
            }
            else if (!string.IsNullOrEmpty(currentColor))
            {
                meta.Color = currentColor;
            }

            // Check for SETSPEED marker (simple speed tag)
            if (cleanToken.Contains("{SETSPEED:"))
            {
                var setSpeedMatch = Regex.Match(cleanToken, @"\{SETSPEED:(\d+)\}");
                if (setSpeedMatch.Success && int.TryParse(setSpeedMatch.Groups[1].Value, out var setSpeed))
                {
                    currentInlineSpeed = setSpeed;
                    cleanToken = cleanToken.Replace(setSpeedMatch.Value, "");
                }
            }

            // Check for paired speed markers
            if (cleanToken.StartsWith("{SPEED:"))
            {
                var speedMatch = Regex.Match(cleanToken, @"\{SPEED:(\d+)\}");
                if (speedMatch.Success && int.TryParse(speedMatch.Groups[1].Value, out var speed))
                {
                    currentInlineSpeed = speed;
                    cleanToken = cleanToken.Replace(speedMatch.Value, "");
                }
            }
            if (cleanToken.EndsWith("{/SPEED}"))
            {
                cleanToken = cleanToken.Replace("{/SPEED}", "");
                meta.Speed = currentInlineSpeed;
                currentInlineSpeed = null;
            }
            else if (currentInlineSpeed.HasValue)
            {
                meta.Speed = currentInlineSpeed;
            }

            // Apply emotion if set for this line
            if (!string.IsNullOrEmpty(currentInlineEmotion))
            {
                meta.Emotion = currentInlineEmotion;
            }

            // Clean remaining TPS tags
            cleanToken = RemoveTpsFormatting(cleanToken);

            if (!string.IsNullOrWhiteSpace(cleanToken))
            {
                cleanWords.Add(cleanToken);
                metadata.Add(meta);
            }
        }

        var cleanLine = string.Join(" ", cleanWords);
        return (cleanLine, metadata.Count > 0 ? metadata : null);
    }

    /// <summary>
    /// Removes TPS formatting tags from text
    /// </summary>
    private static string RemoveTpsFormatting(string text)
    {
        // First check if this is a block header line and return empty if so
        if (Regex.IsMatch(text, @"^###\s*\[[^\]]+\]", RegexOptions.IgnoreCase))
        {
            return string.Empty;
        }

        // Check if this is just a "###" marker or block header parts
        if (text == "###" || (text.StartsWith("[") && text.EndsWith("]")))
        {
            return string.Empty;
        }

        // Remove inline pause markers using slash shorthand
        var trimmed = text.Trim();
        if (trimmed == "/" || trimmed == "//")
        {
            return string.Empty;
        }

        // Remove our temporary markers
        text = Regex.Replace(text, @"\{COLOR:[^}]+\}", "");
        text = Regex.Replace(text, @"\{/COLOR\}", "");
        text = Regex.Replace(text, @"\{SETCOLOR:[^}]+\}", "");
        text = Regex.Replace(text, @"\{SPEED:\d+\}", "");
        text = Regex.Replace(text, @"\{/SPEED\}", "");
        text = Regex.Replace(text, @"\{SETSPEED:\d+\}", "");

        // Remove simple color tags [blue], [red], etc.
        text = Regex.Replace(text, @"\[(blue|red|green|yellow|purple|orange|cyan|magenta|white|gray|black)\]", "", RegexOptions.IgnoreCase);

        // Remove paired color tags [blue]text[/blue]
        text = Regex.Replace(text, @"\[(blue|red|green|yellow|purple|orange|cyan|magenta|white|gray|black)\]([^\[]*?)\[\/(?:blue|red|green|yellow|purple|orange|cyan|magenta|white|gray|black)\]", "$2", RegexOptions.IgnoreCase);

        // Remove speed tags
        text = Regex.Replace(text, @"\[\d+WPM\]", "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\[\d+WPM\]([^\[]*?)\[\/\d+WPM\]", "$1", RegexOptions.IgnoreCase);

        // Remove single square-bracket commands (pause, metadata, edit points, phonetic, pronunciation)
        text = Regex.Replace(text, @"\[pause(?::[^\]]+)?\]", "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\[(?:emotion|speed):[^\]]+\]", "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\[(?:edit_point(?::[^\]]+)?)\]", "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\[(?:phonetic:[^\]]+|pronunciation:[^\]]+)\]", "", RegexOptions.IgnoreCase);

        // Remove emphasis and other formatting tags
        text = Regex.Replace(text, @"\[emphasis\]([^\[]*?)\[\/emphasis\]", "$1", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\[(?:emphasis|slow|fast)\]", "", RegexOptions.IgnoreCase);

        // Other paired tags we might have missed
        text = Regex.Replace(text, @"\[([a-zA-Z]+)\]([^\[]*?)\[\/\1\]", "$2");

        // Paired curly-brace style sets: {green,slow}text{/}
        text = Regex.Replace(text, @"\{[^\}]*\}([^\{]*?)\{\/\}", "$1");

        // Paired angle-bracket tags: <emphasis>text</emphasis>, <solution>text</solution>
        text = Regex.Replace(text, @"<([a-zA-Z]+)[^>]*>(.*?)<\/\1>", "$2");

        return text;
    }

    /// <summary>
    /// Parses TPS format script using TpsParser
    /// </summary>
    private ProcessedScript ParseTpsScript(string content)
    {
        var result = new ProcessedScript();

        // Use TpsParser to parse the TPS document
        var parser = new TpsParser();
        var scriptData = parser.ParseTps(content);

        var allWords = new List<string>();
        var wordIndex = 0;

        // If no segments in parsed data, fall back to simple parsing
        if (scriptData.Segments == null || scriptData.Segments.Length == 0)
        {
            return ParseScriptSimple(content);
        }

        // Convert ScriptData segments to ProcessedSegments
        foreach (var segment in scriptData.Segments)
        {
            var processedSegment = new ProcessedSegment
            {
                Title = segment.Name,
                Emotion = NormalizeEmotion(segment.Emotion),
                StartIndex = wordIndex,
                Speed = segment.WpmOverride ?? scriptData.TargetWpm
            };

            // Process blocks within segment if they exist
            if (segment.Blocks != null && segment.Blocks.Length > 0)
            {
                foreach (var block in segment.Blocks)
                {
                    var blockSpeed = block.WpmOverride ?? processedSegment.Speed;
                    var blockEmotion = block.Emotion != null ? NormalizeEmotion(block.Emotion) : processedSegment.Emotion;

                    // Process phrases within block if they exist
                    if (block.Phrases != null && block.Phrases.Length > 0)
                    {
                        foreach (var phrase in block.Phrases)
                        {
                            // Process words in phrase
                            if (phrase.Words != null && phrase.Words.Length > 0)
                            {
                                foreach (var word in phrase.Words)
                                {
                                    if (!string.IsNullOrWhiteSpace(word.Text))
                                    {
                                        // Don't remove formatting here - word already has clean text
                                        // The word.Text should already be clean, with attributes in word properties
                                        var cleanedWord = word.Text;
                                        if (!string.IsNullOrWhiteSpace(cleanedWord))
                                        {
                                            allWords.Add(cleanedWord);
                                            processedSegment.Words.Add(cleanedWord);

                                            // Store word metadata
                                            if (word.WpmOverride.HasValue && word.WpmOverride.Value != processedSegment.Speed)
                                            {
                                                result.WordSpeedOverrides[wordIndex] = word.WpmOverride.Value;
                                            }
                                            else if (blockSpeed != processedSegment.Speed)
                                            {
                                                result.WordSpeedOverrides[wordIndex] = blockSpeed;
                                            }

                                            // Store emotion override if different from segment
                                            if (blockEmotion != processedSegment.Emotion)
                                            {
                                                result.WordEmotionOverrides[wordIndex] = blockEmotion;
                                            }

                                            // Store color override if present
                                            if (!string.IsNullOrEmpty(word.Color))
                                            {
                                                result.WordColorOverrides[wordIndex] = word.Color;
                                            }

                                            result.WordToSegmentMap[wordIndex] = result.Segments.Count;
                                            wordIndex++;
                                        }
                                    }

                                    // Handle pause after word
                                    if (word.PauseAfter.HasValue && word.PauseAfter.Value > 0)
                                    {
                                        // Add a single pause marker with the full duration
                                        allWords.Add("");
                                        processedSegment.Words.Add("");
                                        result.WordToSegmentMap[wordIndex] = result.Segments.Count;
                                        result.PauseDurations[wordIndex] = word.PauseAfter.Value; // Store pause duration in ms
                                        wordIndex++;
                                    }
                                }
                            }
                            else
                            {
                                // No words array, tokenize the phrase text
                                var phraseWords = TokenizeText(phrase.Text);
                                foreach (var word in phraseWords)
                                {
                                    if (!string.IsNullOrWhiteSpace(word))
                                    {
                                        // Clean the word before adding
                                        var cleanedWord = RemoveTpsFormatting(word);
                                        if (!string.IsNullOrWhiteSpace(cleanedWord))
                                        {
                                            allWords.Add(cleanedWord);
                                            processedSegment.Words.Add(cleanedWord);
                                            if (blockSpeed != processedSegment.Speed)
                                            {
                                                result.WordSpeedOverrides[wordIndex] = blockSpeed;
                                            }

                                            if (blockEmotion != processedSegment.Emotion)
                                            {
                                                result.WordEmotionOverrides[wordIndex] = blockEmotion;
                                            }

                                            result.WordToSegmentMap[wordIndex] = result.Segments.Count;
                                            wordIndex++;
                                        }
                                    }
                                }
                            }

                            // Handle phrase pause duration
                            if (phrase.PauseDuration.HasValue && phrase.PauseDuration.Value > 0)
                            {
                                // Add pause markers (one per 300ms)
                                var pauseCount = Math.Max(1, phrase.PauseDuration.Value / 300);
                                for (var i = 0; i < pauseCount; i++)
                                {
                                    allWords.Add("[PAUSE]");
                                    processedSegment.Words.Add("[PAUSE]");
                                    result.WordToSegmentMap[wordIndex] = result.Segments.Count;
                                    wordIndex++;
                                }
                            }
                        }
                    }
                    else
                    {
                        // No phrases, tokenize the block content
                        var blockWords = TokenizeText(block.Content);
                        foreach (var word in blockWords)
                        {
                            if (!string.IsNullOrWhiteSpace(word))
                            {
                                // Clean the word before adding
                                var cleanedWord = RemoveTpsFormatting(word);
                                if (!string.IsNullOrWhiteSpace(cleanedWord))
                                {
                                    allWords.Add(cleanedWord);
                                    processedSegment.Words.Add(cleanedWord);

                                    if (blockSpeed != processedSegment.Speed)
                                    {
                                        result.WordSpeedOverrides[wordIndex] = blockSpeed;
                                    }

                                    if (blockEmotion != processedSegment.Emotion)
                                    {
                                        result.WordEmotionOverrides[wordIndex] = blockEmotion;
                                    }

                                    result.WordToSegmentMap[wordIndex] = result.Segments.Count;
                                    wordIndex++;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // No blocks, clean and tokenize the segment content directly
                // First remove any block headers that might be in the content
                var cleanedContent = segment.Content;

                // Remove block headers: ### [BlockName|WPM]
                cleanedContent = Regex.Replace(cleanedContent, @"^###\s*\[[^\]]+\].*$", "", RegexOptions.Multiline);

                // Remove any segment headers that might still be there
                cleanedContent = Regex.Replace(cleanedContent, @"^##\s*\[[^\]]+\].*$", "", RegexOptions.Multiline);

                var segmentWords = TokenizeText(cleanedContent);

                // Check for inline color and speed tags in the content
                var currentColor = "";
                var currentSpeed = processedSegment.Speed;

                foreach (var rawWord in segmentWords)
                {
                    if (string.IsNullOrWhiteSpace(rawWord))
                    {
                        continue;
                    }

                    // Check for color tags
                    if (rawWord.StartsWith("[") && rawWord.EndsWith("]"))
                    {
                        var tag = rawWord.Substring(1, rawWord.Length - 2).ToLower();
                        if (IsColorTag(tag))
                        {
                            currentColor = tag;
                            continue; // Don't add tag as a word
                        }
                        else if (tag.EndsWith("wpm") && int.TryParse(tag.Replace("wpm", ""), out var wpm))
                        {
                            currentSpeed = wpm;
                            continue; // Don't add tag as a word
                        }
                    }

                    // Clean and add the actual word
                    var cleanedWord = RemoveTpsFormatting(rawWord);
                    if (!string.IsNullOrWhiteSpace(cleanedWord))
                    {
                        allWords.Add(cleanedWord);
                        processedSegment.Words.Add(cleanedWord);
                    }
                    else
                    {
                        continue; // Skip empty words after cleaning
                    }

                    // Apply current speed override if different
                    if (currentSpeed != processedSegment.Speed)
                    {
                        result.WordSpeedOverrides[wordIndex] = currentSpeed;
                    }

                    // Apply current color if set
                    if (!string.IsNullOrEmpty(currentColor))
                    {
                        result.WordColorOverrides[wordIndex] = currentColor;
                    }

                    result.WordToSegmentMap[wordIndex] = result.Segments.Count;
                    wordIndex++;
                }
            }

            processedSegment.EndIndex = wordIndex - 1;
            result.Segments.Add(processedSegment);
        }

        result.AllWords = allWords;

        // If no segments were created, create a default one
        if (result.Segments.Count == 0)
        {
            result.Segments.Add(new ProcessedSegment
            {
                Title = "Main Content",
                Emotion = "neutral",
                Speed = scriptData.TargetWpm,
                Words = allWords,
                StartIndex = 0,
                EndIndex = allWords.Count - 1
            });
        }

        return result;
    }

    /// <summary>
    /// Normalize emotion string from TPS parser to standard emotion name
    /// </summary>
    private static string NormalizeEmotion(string? emotion)
    {
        if (string.IsNullOrWhiteSpace(emotion))
        {
            return "neutral";
        }

        // Remove emojis and normalize
        var normalized = Regex.Replace(emotion, @"[\p{So}\p{C}]", "").Trim().ToLower();

        // Map common emotion names
        return normalized switch
        {
            "warm" or "happy" or "joyful" => "warm",
            "concerned" or "worried" => "concerned",
            "focused" or "serious" => "focused",
            "motivational" or "inspiring" => "motivational",
            "energetic" or "excited" => "energetic",
            "urgent" or "critical" => "urgent",
            "calm" or "peaceful" => "calm",
            "sad" or "melancholy" => "sad",
            "angry" or "frustrated" => "angry",
            "fear" or "anxious" => "fear",
            "professional" or "formal" => "professional",
            "empathetic" or "caring" => "empathetic",
            _ => normalized
        };
    }

    /// <summary>
    /// Check if a tag is a valid color tag
    /// </summary>
    private static bool IsColorTag(string tag)
    {
        var validColors = new[] { "red", "green", "blue", "orange", "purple", "yellow", "highlight" };
        return validColors.Contains(tag.ToLower());
    }

    /// <summary>
    /// Tokenize text into words
    /// </summary>
    private static string[] TokenizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                  .Where(w => !string.IsNullOrWhiteSpace(w))
                  .ToArray();
    }

    /// <summary>
    /// Simple script parsing for fallback
    /// </summary>
    private ProcessedScript ParseScriptSimple(string content)
    {
        // Use the existing ParseScript logic but skip the TPS check
        var result = new ProcessedScript();

        if (string.IsNullOrEmpty(content))
        {
            result.Segments.Add(new ProcessedSegment
            {
                Title = "Default",
                Words = new List<string>(),
                StartIndex = 0,
                EndIndex = 0
            });
            return result;
        }

        var lines = content.Split('\n');
        ProcessedSegment? currentSegment = null;
        var allWords = new List<string>();
        var currentEmotion = "neutral";
        var currentSpeed = 250;

        foreach (var line in lines)
        {
            if (line.StartsWith("## "))
            {
                if (currentSegment != null)
                {
                    currentSegment.EndIndex = allWords.Count - 1;
                    result.Segments.Add(currentSegment);
                }

                var segmentMatch = Regex.Match(line, @"##\s*\[([^\|]+)\|([^\|]+)\|([^\|]+)\|([^\]]+)\]");
                if (segmentMatch.Success)
                {
                    var title = segmentMatch.Groups[1].Value.Trim();
                    var wpmRange = segmentMatch.Groups[2].Value.Trim();
                    var emotion = segmentMatch.Groups[3].Value.Trim();

                    var wpmMatch = Regex.Match(wpmRange, @"(\d+)");
                    if (wpmMatch.Success && int.TryParse(wpmMatch.Groups[1].Value, out var wpm))
                    {
                        currentSpeed = wpm;
                    }

                    currentEmotion = emotion;
                    currentSegment = new ProcessedSegment
                    {
                        Title = title,
                        Emotion = currentEmotion,
                        Speed = currentSpeed,
                        StartIndex = allWords.Count
                    };
                }
                else
                {
                    currentSegment = new ProcessedSegment
                    {
                        Title = line.Substring(3).Trim(),
                        Emotion = currentEmotion,
                        Speed = currentSpeed,
                        StartIndex = allWords.Count
                    };
                }
                continue;
            }

            if (line.StartsWith("### "))
            {
                var blockMatch = Regex.Match(line, @"###\s*\{([^\|]+)\|([^\|]+)(?:\|([^\}]+))?\}");
                if (blockMatch.Success)
                {
                    var wpmStr = blockMatch.Groups[2].Value.Trim();
                    var wpmNumMatch = Regex.Match(wpmStr, @"(\d+)");
                    if (wpmNumMatch.Success && int.TryParse(wpmNumMatch.Groups[1].Value, out var blockWpm))
                    {
                        currentSpeed = blockWpm;
                    }

                    if (blockMatch.Groups[3].Success && !string.IsNullOrEmpty(blockMatch.Groups[3].Value))
                    {
                        currentEmotion = blockMatch.Groups[3].Value.Trim();
                    }
                }
                continue;
            }

            var cleanLine = RemoveTpsFormatting(line);

            if (!string.IsNullOrWhiteSpace(cleanLine))
            {
                if (currentSegment == null)
                {
                    currentSegment = new ProcessedSegment
                    {
                        Title = "Main Content",
                        Emotion = currentEmotion,
                        Speed = currentSpeed,
                        StartIndex = allWords.Count
                    };
                }

                var words = ProcessLine(cleanLine);
                foreach (var word in words)
                {
                    allWords.Add(word);
                    currentSegment.Words.Add(word);
                    result.WordToSegmentMap[allWords.Count - 1] = result.Segments.Count;
                }
            }
        }

        if (currentSegment != null)
        {
            currentSegment.EndIndex = allWords.Count - 1;
            result.Segments.Add(currentSegment);
        }

        result.AllWords = allWords;

        if (result.Segments.Count == 0)
        {
            result.Segments.Add(new ProcessedSegment
            {
                Title = "Main Content",
                Emotion = "neutral",
                Speed = 250,
                Words = allWords,
                StartIndex = 0,
                EndIndex = allWords.Count - 1
            });
        }

        FinalizeProcessedScript(result);
        return result;
    }

    /// <summary>
    /// Check if a line is a technical marker that should not be displayed
    /// </summary>
    private static bool IsTechnicalMarker(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var trimmed = line.Trim();

        // Check for TPS segment headers: ## [SegmentName|WPM|Emotion]
        if (trimmed.StartsWith("## [") || trimmed.StartsWith("##["))
        {
            return true;
        }

        // Check for TPS block headers: ### [BlockName|WPM]
        if (trimmed.StartsWith("### [") || trimmed.StartsWith("###["))
        {
            return true;
        }

        // Check for OLD segment markers (for backwards compatibility): === SEGMENT ===
        if (trimmed.StartsWith("===") && trimmed.EndsWith("==="))
        {
            return true;
        }

        // Check for OLD block markers: --- BLOCK ---
        if (trimmed.StartsWith("---") && trimmed.EndsWith("---"))
        {
            return true;
        }

        // Check for TPS commands
        if (trimmed.StartsWith("@speed:") ||
            trimmed.StartsWith("@pause:") ||
            trimmed.StartsWith("@color:") ||
            trimmed.StartsWith("@emotion:"))
        {
            return true;
        }

        return false;
    }

    private static string NormalizeLineEndings(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text.Replace("\r\n", "\n").Replace('\r', '\n');
    }

    public ProcessedScript EnhanceScript(ProcessedScript script)
    {
        if (script == null)
        {
            return new ProcessedScript();
        }

        FinalizeProcessedScript(script);
        return script;
    }

    private void FinalizeProcessedScript(ProcessedScript script)
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

    private void GeneratePhraseGroups(ProcessedScript script)
    {
        script.PhraseGroups.Clear();

        var currentIndices = new List<int>();
        var currentWords = new List<string>();
        var words = script.AllWords;
        var accumulatedDuration = 0;
        var pendingPauseMs = 0;
        var pendingPauseFlag = false;
        var lastEmotion = "neutral";

        for (var i = 0; i < words.Count; i++)
        {
            var word = words[i];

            if (string.IsNullOrEmpty(word))
            {
                if (currentIndices.Count == 0)
                {
                    pendingPauseMs += GetPauseDuration(script, i, string.Empty);
                    pendingPauseFlag = true;
                    continue;
                }

                pendingPauseMs += GetPauseDuration(script, i, currentWords.Last());
                pendingPauseFlag = true;
                FinalizePhrase(script, currentIndices, currentWords, accumulatedDuration, pendingPauseMs, pendingPauseFlag, lastEmotion);
                currentIndices.Clear();
                currentWords.Clear();
                accumulatedDuration = 0;
                pendingPauseMs = 0;
                pendingPauseFlag = false;
                continue;
            }

            if (currentIndices.Count == 0 && pendingPauseFlag)
            {
                // Pause belonged to the previous phrase – reset for new phrase
                pendingPauseMs = 0;
                pendingPauseFlag = false;
            }

            currentIndices.Add(i);
            currentWords.Add(word);
            var wordEmotion = GetEmotionForWord(script, i, lastEmotion);
            if (!string.IsNullOrWhiteSpace(wordEmotion))
            {
                lastEmotion = wordEmotion;
            }
            accumulatedDuration += EstimateWordDuration(script, i, word);

            var shouldBreak = ShouldEndPhrase(word, currentWords.Count);
            if (!shouldBreak)
            {
                continue;
            }

            FinalizePhrase(script, currentIndices, currentWords, accumulatedDuration, pendingPauseMs, pendingPauseFlag || EndsWithStrongPause(word), lastEmotion);
            currentIndices = new List<int>();
            currentWords = new List<string>();
            accumulatedDuration = 0;
            pendingPauseMs = 0;
            pendingPauseFlag = false;
        }

        if (currentIndices.Count > 0)
        {
            FinalizePhrase(script, currentIndices, currentWords, accumulatedDuration, pendingPauseMs, pendingPauseFlag, lastEmotion);
        }
    }

    private void FinalizePhrase(
        ProcessedScript script,
        List<int> indices,
        List<string> words,
        int accumulatedDuration,
        int pauseMs,
        bool containsPauseCue,
        string emotion)
    {
        if (indices.Count == 0)
        {
            return;
        }

        var duration = Math.Max(MinimumPhraseDurationMs, accumulatedDuration);
        var pause = pauseMs > 0 ? pauseMs : 0;
        var containsEmphasis = words.Any(IsImportantWord);

        var group = new PhraseGroup
        {
            StartWordIndex = indices.First(),
            EndWordIndex = indices.Last(),
            Words = words.ToArray(),
            EstimatedDurationMs = duration,
            PauseAfterMs = pause,
            EmotionHint = emotion,
            ContainsPauseCue = containsPauseCue || pause > 0,
            ContainsEmphasis = containsEmphasis
        };

        script.PhraseGroups.Add(group);
    }

    private int EstimateWordDuration(ProcessedScript script, int wordIndex, string word)
    {
        var wpm = GetSpeedForWord(script, wordIndex);
        if (wpm <= 0)
        {
            wpm = DefaultSegmentSpeed;
        }

        var baseMs = 60000d / wpm;
        var multiplier = ActorPacingMultiplier;

        if (IsShortWord(word))
        {
            multiplier *= 1.05;
        }

        if (IsImportantWord(word))
        {
            multiplier *= 1.2;
        }

        if (HasSentenceEndingPunctuation(word))
        {
            multiplier *= 1.3;
        }
        else if (HasClausePunctuation(word))
        {
            multiplier *= 1.15;
        }

        var estimated = (int)Math.Round(baseMs * multiplier);
        return Math.Max(MinimumWordDurationMs, estimated);
    }

    private static bool HasSentenceEndingPunctuation(string word)
    {
        return word.Any(c => c is '.' or '!' or '?');
    }

    private static bool HasClausePunctuation(string word)
    {
        return word.Any(c => c is ',' or ';' or ':' or '—' or '–');
    }

    private static int GetSpeedForWord(ProcessedScript script, int wordIndex)
    {
        if (script.WordSpeedOverrides.TryGetValue(wordIndex, out var speed))
        {
            return speed;
        }

        if (script.WordToSegmentMap.TryGetValue(wordIndex, out var segmentIndex) &&
            segmentIndex >= 0 && segmentIndex < script.Segments.Count)
        {
            return script.Segments[segmentIndex].Speed;
        }

        return DefaultSegmentSpeed;
    }

    private static string GetEmotionForWord(ProcessedScript script, int wordIndex, string fallback)
    {
        if (script.WordEmotionOverrides.TryGetValue(wordIndex, out var emotion) && !string.IsNullOrWhiteSpace(emotion))
        {
            return emotion;
        }

        if (script.WordToSegmentMap.TryGetValue(wordIndex, out var segmentIndex) &&
            segmentIndex >= 0 && segmentIndex < script.Segments.Count)
        {
            var segmentEmotion = script.Segments[segmentIndex].Emotion;
            if (!string.IsNullOrWhiteSpace(segmentEmotion))
            {
                return segmentEmotion;
            }
        }

        return fallback;
    }

    private static void BuildUpcomingEmotionLookup(ProcessedScript script)
    {
        script.UpcomingEmotionByStartIndex.Clear();
        foreach (var phrase in script.PhraseGroups)
        {
            if (!string.IsNullOrWhiteSpace(phrase.EmotionHint))
            {
                script.UpcomingEmotionByStartIndex[phrase.StartWordIndex] = phrase.EmotionHint;
            }
        }
    }

    private static bool ShouldEndPhrase(string word, int currentPhraseWordCount)
    {
        if (currentPhraseWordCount >= 5)
        {
            return true;
        }

        if (HasSentenceEndingPunctuation(word))
        {
            return true;
        }

        if (HasClausePunctuation(word) && currentPhraseWordCount >= 3)
        {
            return true;
        }

        return false;
    }

    private static int GetPauseDuration(ProcessedScript script, int wordIndex, string previousWord)
    {
        if (script.PauseDurations.TryGetValue(wordIndex, out var duration) && duration > 0)
        {
            return duration;
        }

        if (!string.IsNullOrEmpty(previousWord) && HasSentenceEndingPunctuation(previousWord))
        {
            return LongPauseMs;
        }

        return DefaultPauseMs;
    }

    private static bool EndsWithStrongPause(string word)
    {
        return word.Length > 0 && (word.EndsWith('.') || word.EndsWith('!') || word.EndsWith('?'));
    }
}
