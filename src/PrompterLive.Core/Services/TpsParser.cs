using System.Text.RegularExpressions;

using PrompterLive.Core.Models.Documents;
using PrompterLive.Core.Models.Tps;

namespace PrompterLive.Core.Services;

/// <summary>
/// Parser for TPS (TelePrompterScript) markdown format
/// </summary>
public partial class TpsParser
{
    // Regex patterns for TPS format parsing
    [GeneratedRegex(@"^---\s*$", RegexOptions.Multiline)]
    private static partial Regex FrontMatterDelimiter();

    [GeneratedRegex(@"^##\s*\[([^\|\]]+)(?:\|([^\|\]]*))?(?:\|([^\|\]]*))?(?:\|([^\|\]]*))?\]", RegexOptions.Multiline)]
    private static partial Regex SegmentHeader();

    [GeneratedRegex(@"^##\s+(.+)$", RegexOptions.Multiline)]
    private static partial Regex SimpleSegmentHeader();

    [GeneratedRegex(@"^###\s*\[([^\|\]]+)(?:\|([^\|\]]*))?(?:\|([^\]]*))?\]", RegexOptions.Multiline)]
    private static partial Regex BlockHeader();

    [GeneratedRegex(@"\[pause:(\d+(?:ms|s))\]")]
    private static partial Regex PauseMarker();

    [GeneratedRegex(@"\[edit_point(?::(\w+))?\]")]
    private static partial Regex EditPointMarker();

    // NOTE: Curly brace syntax {style,color}...{/} is DEPRECATED and no longer supported
    // All formatting must use bracket syntax: [tag]...[/tag]

    // ── TPS Format Constants ──────────────────────────────────────────
    // Single source of truth for all valid keywords. Regex patterns and
    // validation helpers are derived from these arrays — never hardcode
    // color/emotion names elsewhere.

    /// <summary>All valid emotion keywords (case-insensitive in format)</summary>
    private static readonly string[] Emotions =
    [
        "warm", "concerned", "focused", "motivational", "neutral",
        "urgent", "happy", "excited", "sad", "calm", "energetic", "professional"
    ];

    /// <summary>All valid inline color keywords (case-insensitive in format).
    /// "black" is excluded — invisible on dark teleprompter background.
    /// "highlight" lives in FormattingTags — it is a background overlay, not a text color.</summary>
    private static readonly string[] InlineColors =
    [
        "red", "green", "blue", "yellow", "orange", "purple",
        "cyan", "magenta", "pink", "teal", "white", "gray"
    ];

    /// <summary>Speed preset keywords</summary>
    private static readonly string[] SpeedPresets = ["xslow", "slow", "fast", "xfast", "normal"];

    /// <summary>Formatting keywords</summary>
    private static readonly string[] FormattingTags = ["emphasis", "highlight"];

    // Pre-built regex alternation fragments (e.g. "warm|concerned|focused|...")
    private static readonly string EmotionAlt = string.Join("|", Emotions);
    private static readonly string ColorAlt = string.Join("|", InlineColors);
    private static readonly string SpeedAlt = string.Join("|", SpeedPresets);
    private static readonly string FormattingAlt = string.Join("|", FormattingTags);

    /// <summary>Emotion → inline text color mapping for dark background rendering.
    /// All values chosen for WCAG AA contrast against ~#1A1B2E background.</summary>
    private static readonly Dictionary<string, string> EmotionTextColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["warm"] = "#FFA94D",        // Orange — warm/friendly
        ["concerned"] = "#FF6B6B",   // Soft red — worried
        ["focused"] = "#51CF66",     // Green — concentrated
        ["motivational"] = "#CC5DE8",// Purple — inspiring
        ["urgent"] = "#FF6B6B",      // Bright red — critical
        ["happy"] = "#FFE066",       // Yellow — joyful
        ["excited"] = "#F783AC",     // Pink — enthusiastic
        ["sad"] = "#9775FA",         // Indigo — somber
        ["calm"] = "#38D9A9",        // Teal — peaceful
        ["energetic"] = "#FFA94D",   // Orange-Red — dynamic
        ["professional"] = "#74C0FC",// Light navy — formal (visible on dark BG)
        ["neutral"] = "#ADB5BD"      // Light gray — balanced (visible on dark BG)
    };

    // Escape sequence placeholders (Unicode Private Use Area)
    private const char EscapedBracketOpen = '\uE001';   // \[
    private const char EscapedBracketClose = '\uE002';  // \]
    private const char EscapedPipe = '\uE003';          // \|
    private const char EscapedSlash = '\uE004';         // \/
    private const char EscapedAsterisk = '\uE005';      // \*
    private const char EscapedBackslash = '\uE006';     // \\

    /// <summary>
    /// Convert escape sequences to placeholders before parsing
    /// </summary>
    private static string ProcessEscapeSequences(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return text
            .Replace(@"\\", EscapedBackslash.ToString())  // Must be first
            .Replace(@"\[", EscapedBracketOpen.ToString())
            .Replace(@"\]", EscapedBracketClose.ToString())
            .Replace(@"\|", EscapedPipe.ToString())
            .Replace(@"\/", EscapedSlash.ToString())
            .Replace(@"\*", EscapedAsterisk.ToString());
    }

    /// <summary>
    /// Restore escaped characters after parsing
    /// </summary>
    private static string RestoreEscapedCharacters(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return text
            .Replace(EscapedBracketOpen, '[')
            .Replace(EscapedBracketClose, ']')
            .Replace(EscapedPipe, '|')
            .Replace(EscapedSlash, '/')
            .Replace(EscapedAsterisk, '*')
            .Replace(EscapedBackslash, '\\');
    }

    /// <summary>
    /// Parse TPS file into TpsDocument
    /// </summary>
    public async Task<TpsDocument> ParseFileAsync(string filePath)
    {
        var content = await File.ReadAllTextAsync(filePath);
        content = NormalizeLineEndings(content);
        return await ParseAsync(content);
    }

    /// <summary>
    /// Parse TPS content into TpsDocument (for compilation)
    /// </summary>
    public Task<TpsDocument> ParseAsync(string tpsContent)
    {
        if (string.IsNullOrWhiteSpace(tpsContent))
        {
            return Task.FromResult(new TpsDocument());
        }

        tpsContent = NormalizeLineEndings(tpsContent);
        tpsContent = ProcessEscapeSequences(tpsContent);

        var (frontMatter, content) = ExtractFrontMatter(tpsContent);

        var document = new TpsDocument
        {
            Metadata = frontMatter
        };

        // Parse segments - check both complex and simple formats
        var segmentMatches = SegmentHeader().Matches(content);
        if (segmentMatches.Count == 0)
        {
            // Try simple segment format
            segmentMatches = SimpleSegmentHeader().Matches(content);
        }

        // If still no segments found, treat entire content as one segment
        if (segmentMatches.Count == 0)
        {
            var colors = GetEmotionColors("neutral");
            var segment = new TpsSegment
            {
                Id = Guid.NewGuid().ToString(),
                Name = AppText.Get("Parser.Segment.Content"),
                Content = content,
                Emotion = "neutral",
                BackgroundColor = colors.Background,
                TextColor = colors.Text,
                AccentColor = colors.Accent,
                TargetWPM = 250
            };
            document.Segments.Add(segment);
            return Task.FromResult(document);
        }

        var segmentPositions = segmentMatches.Cast<Match>().Select(m => m.Index).ToList();
        segmentPositions.Add(content.Length);

        for (var i = 0; i < segmentMatches.Count; i++)
        {
            var match = segmentMatches[i];
            var segmentEnd = segmentPositions[i + 1];
            var segmentContent = content.Substring(match.Index, segmentEnd - match.Index);

            TpsSegment segment;
            if (match.Groups.Count > 2)
            {
                // Complex segment format
                var emotion = match.Groups[3].Success ? match.Groups[3].Value : "neutral";
                var colors = GetEmotionColors(emotion);

                segment = new TpsSegment
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = match.Groups[1].Value.Trim(),
                    Content = segmentContent,
                    TargetWPM = match.Groups[2].Success && int.TryParse(match.Groups[2].Value.Replace("WPM", ""), out var wpm) ? wpm : (int?)null,
                    Emotion = emotion,
                    BackgroundColor = colors.Background,
                    TextColor = colors.Text,
                    AccentColor = colors.Accent,
                    Duration = match.Groups[4].Success ? ParseDuration(match.Groups[4].Value) : null
                };
            }
            else
            {
                // Simple segment format (## Title)
                var colors = GetEmotionColors("neutral");
                segment = new TpsSegment
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = match.Groups[1].Value.Trim(),
                    Content = segmentContent,
                    Emotion = "neutral",
                    BackgroundColor = colors.Background,
                    TextColor = colors.Text,
                    AccentColor = colors.Accent
                };
            }

            // Parse blocks within segment
            var blockMatches = BlockHeader().Matches(segmentContent);
            if (blockMatches.Count > 0)
            {
                var headerLength = match.Length;
                var firstBlockIndex = blockMatches[0].Index;
                if (firstBlockIndex > headerLength)
                {
                    var leadingContent = segmentContent.Substring(headerLength, firstBlockIndex - headerLength);
                    if (!string.IsNullOrWhiteSpace(leadingContent))
                    {
                        segment.LeadingContent = leadingContent.Trim('\r', '\n');
                    }
                }
            }

            var blockPositions = blockMatches.Cast<Match>().Select(m => m.Index).ToList();
            blockPositions.Add(segmentContent.Length);

            for (var j = 0; j < blockMatches.Count; j++)
            {
                var blockMatch = blockMatches[j];
                var blockEnd = blockPositions[j + 1];
                var blockContent = segmentContent.Substring(blockMatch.Index, blockEnd - blockMatch.Index);

                var block = new TpsBlock
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = blockMatch.Groups[1].Value.Trim(),
                    Content = blockContent,
                    TargetWPM = blockMatch.Groups[2].Success && int.TryParse(blockMatch.Groups[2].Value.Replace("WPM", ""), out var blockWpm) ? blockWpm : (int?)null,
                    Emotion = blockMatch.Groups[3].Success ? blockMatch.Groups[3].Value : null
                };

                segment.Blocks.Add(block);
            }

            document.Segments.Add(segment);
        }

        return Task.FromResult(document);
    }

    private TimeSpan? ParseDuration(string durationString)
    {
        if (string.IsNullOrWhiteSpace(durationString))
        {
            return null;
        }

        // Parse formats like "0:00-1:30" or "1:30"
        var parts = durationString.Split('-');
        if (parts.Length == 2)
        {
            // It's a range, return the duration (end - start)
            if (TryParseTime(parts[0], out var start) && TryParseTime(parts[1], out var end))
            {
                return end - start;
            }
        }
        else if (parts.Length == 1)
        {
            // It's a single duration
            if (TryParseTime(parts[0], out var duration))
            {
                return duration;
            }
        }

        return null;
    }

    private static bool TryParseTime(string timeString, out TimeSpan time)
    {
        time = TimeSpan.Zero;
        var parts = timeString.Trim().Split(':');

        if (parts.Length == 2)
        {
            // mm:ss format
            if (int.TryParse(parts[0], out var minutes) && int.TryParse(parts[1], out var seconds))
            {
                time = new TimeSpan(0, minutes, seconds);
                return true;
            }
        }
        else if (parts.Length == 3)
        {
            // hh:mm:ss format
            if (int.TryParse(parts[0], out var hours) && int.TryParse(parts[1], out var minutes) && int.TryParse(parts[2], out var seconds))
            {
                time = new TimeSpan(hours, minutes, seconds);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Parse TPS content into structured ScriptData
    /// </summary>
    public ScriptData ParseTps(string tpsContent)
    {
        if (string.IsNullOrWhiteSpace(tpsContent))
        {
            return CreateEmptyScript();
        }

        tpsContent = NormalizeLineEndings(tpsContent);
        tpsContent = ProcessEscapeSequences(tpsContent);

        var (frontMatter, content) = ExtractFrontMatter(tpsContent);
        var segments = ParseSegments(content, frontMatter);

        return new ScriptData
        {
            ScriptId = Guid.NewGuid().ToString(),
            Title = frontMatter.GetValueOrDefault("title", AppText.Get("Parser.Title.Untitled")),
            Content = content,  // Keep raw TPS content for editing
            TargetWpm = int.TryParse(frontMatter.GetValueOrDefault("base_wpm"), out var wpm) ? wpm : 140,
            Segments = segments.ToArray()
        };
    }

    /// <summary>
    /// Extract front matter and content from TPS
    /// </summary>
    private static (Dictionary<string, string> frontMatter, string content) ExtractFrontMatter(string tpsContent)
    {
        var frontMatter = new Dictionary<string, string>();
        var content = tpsContent;

        var frontMatterMatch = FrontMatterDelimiter().Matches(tpsContent);
        if (frontMatterMatch.Count >= 2)
        {
            var start = frontMatterMatch[0].Index + frontMatterMatch[0].Length;
            var end = frontMatterMatch[1].Index;
            var frontMatterText = tpsContent[start..end].Trim();
            content = tpsContent[(frontMatterMatch[1].Index + frontMatterMatch[1].Length)..].Trim();

            // Parse YAML-like front matter
            var lines = frontMatterText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var colonIndex = line.IndexOf(':');
                if (colonIndex > 0)
                {
                    var key = line[..colonIndex].Trim();
                    var value = line[(colonIndex + 1)..].Trim().Trim('"', '\'');
                    frontMatter[key] = value;
                }
            }
        }

        return (frontMatter, content);
    }

    /// <summary>
    /// Parse segments from content
    /// </summary>
    private List<ScriptSegment> ParseSegments(string content, Dictionary<string, string> frontMatter)
    {
        var segments = new List<ScriptSegment>();

        // Try complex format first
        var segmentMatches = SegmentHeader().Matches(content);

        // If no complex segments, try simple format
        if (segmentMatches.Count == 0)
        {
            segmentMatches = SimpleSegmentHeader().Matches(content);
            if (segmentMatches.Count > 0)
            {
                return ParseSimpleSegments(content, segmentMatches);
            }
        }

        if (segmentMatches.Count == 0)
        {
            // No segments defined, create a single default segment
            var words = TokenizeContent(content);
            segments.Add(CreateDefaultSegment(content, words));
            return segments;
        }

        for (var i = 0; i < segmentMatches.Count; i++)
        {
            var match = segmentMatches[i];
            var segmentName = match.Groups[1].Value;
            var wpmSpec = match.Groups[2].Value;
            var emotion = match.Groups[3].Value;

            // Extract segment content (skip the header line)
            var startPos = match.Index + match.Length;
            var endPos = i + 1 < segmentMatches.Count
                ? segmentMatches[i + 1].Index
                : content.Length;

            var segmentContent = content[startPos..endPos].Trim();
            var blocks = ParseBlocks(segmentContent);
            var words = TokenizeContent(segmentContent);

            // Parse WPM specification
            var (wpmMin, wpmMax) = ParseWpmRangeSpec(wpmSpec);

            // Time is now calculated automatically, not parsed from format
            var startTime = "0:00";
            var endTime = "0:00";

            // Get colors for this emotion
            var colors = GetEmotionColors(emotion);

            var segment = new ScriptSegment
            {
                Name = segmentName,
                Emotion = NormalizeEmotionName(emotion),
                BackgroundColor = colors.Background,
                TextColor = colors.Text,
                AccentColor = colors.Accent,
                WpmOverride = wpmMin,
                WpmMax = wpmMax,
                StartTime = startTime,
                EndTime = endTime,
                StartIndex = segments.Sum(s => CountWords(s.Content)),
                EndIndex = segments.Sum(s => CountWords(s.Content)) + words.Length - 1,
                Content = segmentContent,
                Blocks = blocks.ToArray()
            };

            segments.Add(segment);
        }

        return segments;
    }

    /// <summary>
    /// Parse simple segments with emotion emojis and inline tags
    /// </summary>
    private List<ScriptSegment> ParseSimpleSegments(string content, MatchCollection segmentMatches)
    {
        var segments = new List<ScriptSegment>();
        var totalWordCount = 0;

        for (var i = 0; i < segmentMatches.Count; i++)
        {
            var match = segmentMatches[i];
            var segmentTitle = match.Groups[1].Value.Trim();

            // Extract segment content
            var startPos = match.Index + match.Length;
            var endPos = i + 1 < segmentMatches.Count
                ? segmentMatches[i + 1].Index
                : content.Length;

            var segmentContent = content[startPos..endPos].Trim();

            // Parse emotion from emoji at start of content
            var emotion = "neutral";
            var speed = 250;

            // Check for emoji and emotion at start
            var emojiPattern = @"^([😊😟🎯💪⚡🚨😌😢😠😨💼🕊️🌧️])\s*(\w+)?";
            var emojiMatch = Regex.Match(segmentContent, emojiPattern);
            if (emojiMatch.Success)
            {
                var emoji = emojiMatch.Groups[1].Value;
                emotion = MapEmojiToEmotion(emoji);
                // Remove emoji from content
                segmentContent = segmentContent.Substring(emojiMatch.Length).Trim();
            }

            // Check for speed tags in content
            var speedPattern = @"\[(\d+)WPM\]";
            var speedMatch = Regex.Match(segmentContent, speedPattern, RegexOptions.IgnoreCase);
            if (speedMatch.Success && int.TryParse(speedMatch.Groups[1].Value, out var wpm))
            {
                speed = wpm;
            }

            // Parse into blocks with inline formatting
            var blocks = ParseInlineBlocks(segmentContent, emotion, speed);

            // Count words
            var wordCount = CountWords(segmentContent);

            // Get colors for this emotion
            var colors = GetEmotionColors(emotion);

            var segment = new ScriptSegment
            {
                Name = segmentTitle,
                Emotion = emotion,
                BackgroundColor = colors.Background,
                TextColor = colors.Text,
                AccentColor = colors.Accent,
                WpmOverride = speed,
                StartIndex = totalWordCount,
                EndIndex = totalWordCount + wordCount - 1,
                Content = segmentContent,
                Blocks = blocks.ToArray()
            };

            segments.Add(segment);
            totalWordCount += wordCount;
        }

        return segments;
    }

    /// <summary>
    /// Map emoji to emotion name
    /// </summary>
    private static string MapEmojiToEmotion(string emoji)
    {
        return emoji switch
        {
            "😊" => "warm",
            "😟" => "concerned",
            "🎯" => "focused",
            "💪" => "motivational",
            "⚡" => "energetic",
            "🚨" => "urgent",
            "😌" => "calm",
            "😢" => "sad",
            "😠" => "angry",
            "😨" => "fear",
            "💼" => "professional",
            "🕊️" => "peaceful",
            "🌧️" => "melancholy",
            _ => "neutral"
        };
    }

    /// <summary>
    /// Parse blocks with inline formatting tags
    /// </summary>
    private List<ScriptBlock> ParseInlineBlocks(string content, string defaultEmotion, int defaultSpeed)
    {
        var blocks = new List<ScriptBlock>();

        // Split by paragraphs or major formatting changes
        var lines = content.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
        var blockStartIndex = 0;

        foreach (var line in lines)
        {
            var blockContent = line.Trim();
            if (string.IsNullOrEmpty(blockContent))
            {
                continue;
            }

            // Check for inline emotion changes
            var emotion = defaultEmotion;
            var speed = defaultSpeed;

            // Check for color tags that might indicate emotion
            if (blockContent.Contains("[red]") || blockContent.Contains("[yellow]"))
            {
                emotion = "urgent";
            }
            else if (blockContent.Contains("[blue]"))
            {
                emotion = "calm";
            }
            else if (blockContent.Contains("[green]"))
            {
                emotion = "positive";
            }

            // Check for speed changes
            var speedMatch = Regex.Match(blockContent, @"\[(\d+)WPM\]", RegexOptions.IgnoreCase);
            if (speedMatch.Success && int.TryParse(speedMatch.Groups[1].Value, out var blockSpeed))
            {
                speed = blockSpeed;
            }

            var wordCount = CountWords(blockContent);

            var block = new ScriptBlock
            {
                Name = AppText.Format("Parser.Block.Indexed", blocks.Count + 1),
                Emotion = emotion,
                WpmOverride = speed,
                StartIndex = blockStartIndex,
                EndIndex = blockStartIndex + wordCount - 1,
                Content = blockContent,
                Phrases = ParsePhrases(blockContent).ToArray()
            };

            blocks.Add(block);
            blockStartIndex += wordCount;
        }

        // If no blocks created, make one default block
        if (blocks.Count == 0)
        {
            blocks.Add(new ScriptBlock
            {
                Name = AppText.Get("Parser.Block.Default"),
                Emotion = defaultEmotion,
                WpmOverride = defaultSpeed,
                StartIndex = 0,
                EndIndex = CountWords(content) - 1,
                Content = content,
                Phrases = ParsePhrases(content).ToArray()
            });
        }

        return blocks;
    }

    /// <summary>
    /// Parse blocks within a segment
    /// </summary>
    private List<ScriptBlock> ParseBlocks(string segmentContent)
    {
        var blocks = new List<ScriptBlock>();
        var blockMatches = BlockHeader().Matches(segmentContent);

        if (blockMatches.Count == 0)
        {
            // No blocks defined, create a single default block
            var words = TokenizeContent(segmentContent);
            blocks.Add(new ScriptBlock
            {
                Name = AppText.Get("Parser.Block.Default"),
                StartIndex = 0,
                EndIndex = words.Length - 1,
                Content = segmentContent
            });
            return blocks;
        }

        for (var i = 0; i < blockMatches.Count; i++)
        {
            var match = blockMatches[i];
            var blockName = match.Groups[1].Value;
            var wpmSpec = match.Groups[2].Value;
            var emotion = match.Groups[3].Value;

            // Extract block content
            var startPos = match.Index + match.Length;
            var endPos = i + 1 < blockMatches.Count
                ? blockMatches[i + 1].Index
                : segmentContent.Length;

            var blockContent = segmentContent[startPos..endPos].Trim();
            var phrases = ParsePhrases(blockContent);

            var block = new ScriptBlock
            {
                Name = blockName,
                Emotion = string.IsNullOrEmpty(emotion) ? null : NormalizeEmotionName(emotion),
                WpmOverride = ParseWpmSpec(wpmSpec),
                StartIndex = blocks.Sum(b => CountWords(b.Content)),
                EndIndex = blocks.Sum(b => CountWords(b.Content)) + CountWords(blockContent) - 1,
                Content = blockContent,
                Phrases = phrases.ToArray()
            };

            blocks.Add(block);
        }

        return blocks;
    }

    /// <summary>
    /// Parse phrases within a block
    /// </summary>
    private List<ScriptPhrase> ParsePhrases(string blockContent)
    {
        var phrases = new List<ScriptPhrase>();

        // Split by sentence boundaries for now (could be more sophisticated)
        var sentences = blockContent.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

        var startIndex = 0;
        foreach (var sentence in sentences)
        {
            var cleanSentence = sentence.Trim();
            if (string.IsNullOrEmpty(cleanSentence))
            {
                continue;
            }

            var words = TokenizeContent(cleanSentence);
            var parsedWords = ParseWords(cleanSentence);

            var phrase = new ScriptPhrase
            {
                Text = cleanSentence,
                StartIndex = startIndex,
                EndIndex = startIndex + words.Length - 1,
                Words = parsedWords.ToArray()
            };

            phrases.Add(phrase);
            startIndex += words.Length;
        }

        return phrases;
    }

    /// <summary>
    /// Parse individual words with their properties
    /// </summary>
    private static List<ScriptWord> ParseWords(string text)
    {
        var words = new List<ScriptWord>();

        // First, let's extract and preserve color information
        var processedText = text;
        var wordColors = new Dictionary<string, string>();

        // Extract paired emotion tags and convert to colors
        processedText = Regex.Replace(processedText,
            $@"\[({EmotionAlt})\]([^\[]*?)\[\/(?:{EmotionAlt})\]",
            (match) =>
            {
                var emotion = match.Groups[1].Value;
                var content = match.Groups[2].Value;
                var color = EmotionTextColors.GetValueOrDefault(emotion, "#808080");
                foreach (var word in content.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    wordColors[word] = color;
                }

                return content;
            }, RegexOptions.IgnoreCase);

        // Extract paired color tags
        processedText = Regex.Replace(processedText,
            $@"\[({ColorAlt})\]([^\[]*?)\[\/(?:{ColorAlt})\]",
            (match) =>
            {
                var color = match.Groups[1].Value;
                var content = match.Groups[2].Value;
                foreach (var word in content.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    wordColors[word] = color;
                }

                return content;
            }, RegexOptions.IgnoreCase);

        // === COMPREHENSIVE TAG REMOVAL ===
        // Remove ALL TPS formatting tags to ensure clean text display

        // 1. Remove pause markers
        processedText = Regex.Replace(processedText, @"\[pause:\d+[ms]?\]", "", RegexOptions.IgnoreCase);

        // 2. Remove edit points
        processedText = Regex.Replace(processedText, @"\[edit_point(?::(high|medium|low))?\]", "", RegexOptions.IgnoreCase);

        // 3. Remove pronunciation guides (extract content between tags)
        processedText = Regex.Replace(processedText, @"\[phonetic:[^\]]+\]([^\[]+)\[/phonetic\]", "$1", RegexOptions.IgnoreCase);
        processedText = Regex.Replace(processedText, @"\[pronunciation:[^\]]+\]([^\[]+)\[/pronunciation\]", "$1", RegexOptions.IgnoreCase);

        // 4. TPS format uses ONLY [] bracket syntax
        // Curly braces {} and HTML-style <tags> are NOT supported

        // 6. Remove all simple opening/closing tags (emotions, colors, speed, formatting)
        processedText = Regex.Replace(processedText, $@"\[/?({EmotionAlt})\]", "", RegexOptions.IgnoreCase);
        processedText = Regex.Replace(processedText, $@"\[/?({ColorAlt})\]", "", RegexOptions.IgnoreCase);
        processedText = Regex.Replace(processedText, $@"\[/?({FormattingAlt}|{SpeedAlt})\]", "", RegexOptions.IgnoreCase);

        // 7. Remove speed tags with numbers
        processedText = Regex.Replace(processedText, @"\[/?\d+WPM\]", "", RegexOptions.IgnoreCase);

        // 8. Extract content from paired tags we haven't handled yet
        // Pattern: [tag]content[/tag] -> content
        processedText = Regex.Replace(processedText, @"\[([^\]]+)\]([^\[]*)\[/\1\]", "$2", RegexOptions.IgnoreCase);

        // 9. Final cleanup: Remove any remaining [something] tags
        // This is a catch-all for any other formatting tags
        processedText = Regex.Replace(processedText, @"\[[^\]]*\]", "", RegexOptions.IgnoreCase);

        // Now tokenize the cleaned text
        var tokens = TokenizeContent(processedText);

        foreach (var token in tokens)
        {
            // Handle pause markers
            if (token == "/" || token == "//")
            {
                words.Add(new ScriptWord
                {
                    Text = string.Empty,
                    OrpIndex = 0,
                    PauseAfter = token == "//" ? 600 : 300
                });
                continue;
            }

            // Clean any remaining formatting from the word
            var cleanWord = CleanWord(token);

            if (!string.IsNullOrWhiteSpace(cleanWord))
            {
                var scriptWord = new ScriptWord
                {
                    Text = cleanWord,
                    OrpIndex = CalculateORP(cleanWord),
                    EmphasisLevel = DetermineEmphasisLevel(token),
                    Color = wordColors.TryGetValue(cleanWord, out var value) ? value : null,
                    PauseAfter = ExtractPause(token),
                    IsEditPoint = IsEditPoint(token),
                    EditPointPriority = ExtractEditPointPriority(token)
                };

                words.Add(scriptWord);
            }
        }

        return words;
    }

    /// <summary>
    /// Calculate Optimal Recognition Point for a word
    /// </summary>
    private static int CalculateORP(string word)
    {
        var length = word.Length;
        return length switch
        {
            <= 1 => 0,
            <= 5 => 1,  // 30% position
            <= 9 => 2,  // 35% position  
            <= 13 => 3, // 40% position
            _ => 4      // Longer words
        };
    }

    /// <summary>
    /// Parse WPM specification (e.g., "140WPM")
    /// </summary>
    private static int? ParseWpmSpec(string wpmSpec)
    {
        if (string.IsNullOrEmpty(wpmSpec))
        {
            return null;
        }

        var numbers = Regex.Matches(wpmSpec, @"\d+");
        if (numbers.Count > 0 && int.TryParse(numbers[0].Value, out var wpm))
        {
            return wpm;
        }

        return null;
    }

    /// <summary>
    /// Parse WPM range specification (e.g., "250-300WPM"). Returns (min,max?)
    /// </summary>
    private static (int? min, int? max) ParseWpmRangeSpec(string wpmSpec)
    {
        if (string.IsNullOrEmpty(wpmSpec))
        {
            return (null, null);
        }

        var numbers = Regex.Matches(wpmSpec, @"\d+");
        if (numbers.Count >= 2)
        {
            int.TryParse(numbers[0].Value, out var min);
            int.TryParse(numbers[1].Value, out var max);
            return (min, max);
        }
        if (numbers.Count == 1 && int.TryParse(numbers[0].Value, out var only))
        {
            return (only, null);
        }
        return (null, null);
    }

    /// <summary>
    /// Normalize emotion tokens like "😊 Warm" or "😟 Concerned" to canonical key (warm, concerned)
    /// </summary>
    private static string NormalizeEmotionName(string emotion)
    {
        if (string.IsNullOrWhiteSpace(emotion))
        {
            return "neutral";
        }
        // Remove emoji and trim
        var e = Regex.Replace(emotion, @"^[\p{So}\p{C}]+\s*", string.Empty).Trim();
        e = e.ToLowerInvariant();
        // Map synonyms
        return e switch
        {
            "😊 warm" => "warm",
            "😟 concerned" => "concerned",
            "🎯 focused" => "focused",
            "🤝 empathetic" or "💚 empathetic" => "empathetic",
            "neutral" or "😐 neutral" => "neutral",
            "urgent" or "🚨 urgent" => "urgent",
            _ => e
        };
    }

    /// <summary>
    /// Helper methods for word processing
    /// </summary>
    private static string[] TokenizeContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Array.Empty<string>();
        }

        return content.Split(new[] { ' ', '\t', '\n', '\r' },
                           StringSplitOptions.RemoveEmptyEntries)
                     .Where(word => !string.IsNullOrWhiteSpace(word))
                     .ToArray();
    }

    private static int CountWords(string content) => TokenizeContent(content).Length;

    private static string CleanWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return string.Empty;
        }

        if (word == "/" || word == "//")
        {
            return string.Empty;
        }

        var t = word;

        // === COMPREHENSIVE TAG REMOVAL - EVERYTHING USES [] BRACKETS ===

        // 1. Remove pause markers
        t = Regex.Replace(t, @"\[pause:\d+[ms]?\]", "", RegexOptions.IgnoreCase);

        // 2. Remove edit points
        t = Regex.Replace(t, @"\[edit_point(?::(high|medium|low))?\]", "", RegexOptions.IgnoreCase);

        // 3. Extract content from pronunciation guides
        t = Regex.Replace(t, @"\[phonetic:[^\]]+\]([^\[]+)\[/phonetic\]", "$1", RegexOptions.IgnoreCase);
        t = Regex.Replace(t, @"\[pronunciation:[^\]]+\]([^\[]+)\[/pronunciation\]", "$1", RegexOptions.IgnoreCase);

        // 4. Remove emotion tags (both paired and unpaired)
        t = Regex.Replace(t, $@"\[({EmotionAlt})\]([^\[]*)\[/({EmotionAlt})\]", "$2", RegexOptions.IgnoreCase);
        t = Regex.Replace(t, $@"\[/?({EmotionAlt})\]", "", RegexOptions.IgnoreCase);

        // 5. Remove color tags (both paired and unpaired)
        t = Regex.Replace(t, $@"\[({ColorAlt})\]([^\[]*)\[/({ColorAlt})\]", "$2", RegexOptions.IgnoreCase);
        t = Regex.Replace(t, $@"\[/?({ColorAlt})\]", "", RegexOptions.IgnoreCase);

        // 6. Remove speed tags
        t = Regex.Replace(t, @"\[\d+WPM\]([^\[]*)\[/\d+WPM\]", "$1", RegexOptions.IgnoreCase);
        t = Regex.Replace(t, @"\[/?\d+WPM\]", "", RegexOptions.IgnoreCase);
        t = Regex.Replace(t, $@"\[({SpeedAlt})\]([^\[]*)\[/({SpeedAlt})\]", "$2", RegexOptions.IgnoreCase);
        t = Regex.Replace(t, $@"\[/?({SpeedAlt})\]", "", RegexOptions.IgnoreCase);

        // 7. Remove formatting tags (emphasis, highlight)
        t = Regex.Replace(t, $@"\[({FormattingAlt})\]([^\[]*)\[/({FormattingAlt})\]", "$2", RegexOptions.IgnoreCase);
        t = Regex.Replace(t, $@"\[/?({FormattingAlt})\]", "", RegexOptions.IgnoreCase);

        // 8. Final catch-all: remove any remaining [] tags
        t = Regex.Replace(t, @"\[[^\]]*\]", "", RegexOptions.IgnoreCase);

        // 9. Clean up markdown emphasis (converted to [emphasis] internally)
        t = Regex.Replace(t, @"\*\*([^*]+)\*\*", "$1"); // **bold**
        t = Regex.Replace(t, @"\*([^*]+)\*", "$1");     // *italic*
        t = Regex.Replace(t, @"__([^_]+)__", "$1");     // __underline__

        // 10. Restore escaped characters
        t = RestoreEscapedCharacters(t);

        return t.Trim();
    }

    private static int DetermineEmphasisLevel(string token)
    {
        if (token.Contains("**"))
        {
            return 2; // Strong emphasis (markdown)
        }

        if (token.Contains('*') || token.Contains("[emphasis]"))
        {
            return 1; // Normal emphasis
        }

        return 0; // No emphasis
    }

    private static bool IsValidColor(string color)
    {
        return InlineColors.Contains(color, StringComparer.OrdinalIgnoreCase);
    }

    private static int? ExtractPause(string token)
    {
        var match = PauseMarker().Match(token);
        if (match.Success)
        {
            var pauseValue = match.Groups[1].Value;
            if (pauseValue.EndsWith("ms"))
            {
                if (int.TryParse(pauseValue[..^2], out var ms))
                {
                    return ms;
                }
            }
            else if (pauseValue.EndsWith("s"))
            {
                if (int.TryParse(pauseValue[..^1], out var seconds))
                {
                    return seconds * 1000; // Convert to milliseconds
                }
            }
        }
        return null;
    }

    private static bool IsEditPoint(string token)
    {
        return EditPointMarker().IsMatch(token);
    }

    private static string? ExtractEditPointPriority(string token)
    {
        var match = EditPointMarker().Match(token);
        if (match.Success && match.Groups[1].Success)
        {
            return match.Groups[1].Value;
        }
        return null;
    }

    private static ScriptData CreateEmptyScript()
    {
        return new ScriptData
        {
            ScriptId = Guid.NewGuid().ToString(),
            Title = AppText.Get("Parser.Empty.Title"),
            Content = string.Empty,
            TargetWpm = 140,
            Segments = Array.Empty<ScriptSegment>()
        };
    }

    private static ScriptSegment CreateDefaultSegment(string content, string[] words)
    {
        // Use blue colors for default/neutral emotion
        return new ScriptSegment
        {
            Name = AppText.Get("Parser.Empty.MainContent"),
            Emotion = "😊",
            BackgroundColor = "#FF3B82F6",
            TextColor = "#FFFFFFFF",
            AccentColor = "#FF2563EB",
            StartIndex = 0,
            EndIndex = words.Length - 1,
            Content = content
        };
    }

    /// <summary>
    /// Get color scheme for an emotion
    /// </summary>
    private static (string Background, string Text, string Accent) GetEmotionColors(string emotion)
    {
        return emotion.ToLower() switch
        {
            "warm" => ("#FFFB923C", "#FF1F2937", "#FFE97F00"), // Orange
            "concerned" => ("#FFF87171", "#FF1F2937", "#FFDC2626"), // Red
            "focused" => ("#FF4ADE80", "#FF1F2937", "#FF16A34A"), // Green
            "motivational" => ("#FFA855F7", "#FFFFFFFF", "#FF7C3AED"), // Purple
            "urgent" => ("#FFEF4444", "#FFFFFFFF", "#FFB91C1C"), // Bright Red
            "happy" => ("#FFFACC15", "#FF1F2937", "#FFD97706"), // Yellow
            "excited" => ("#FFEC4899", "#FFFFFFFF", "#FFDB2777"), // Pink
            "sad" => ("#FF6366F1", "#FFFFFFFF", "#FF4F46E5"), // Indigo
            "calm" => ("#FF14B8A6", "#FFFFFFFF", "#FF0D9488"), // Teal
            "energetic" => ("#FFF97316", "#FFFFFFFF", "#FFEA580C"), // Orange-Red
            "professional" => ("#FF1E40AF", "#FFFFFFFF", "#FF1E3A8A"), // Navy
            "neutral" => ("#FF3B82F6", "#FFFFFFFF", "#FF2563EB"), // Blue
            _ => ("#FF3B82F6", "#FFFFFFFF", "#FF2563EB") // Default blue
        };
    }

    private static string NormalizeLineEndings(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text.Replace("\r\n", "\n").Replace('\r', '\n');
    }
}
