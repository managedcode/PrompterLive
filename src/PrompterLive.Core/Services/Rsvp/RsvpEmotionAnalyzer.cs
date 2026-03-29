namespace PrompterLive.Core.Services.Rsvp;

/// <summary>
/// Analyzes words for emotional context and exposes semantic color metadata
/// without relying on platform-specific UI types.
/// </summary>
public class RsvpEmotionAnalyzer
{
    public record EmotionData(string ColorHex, string Name, string Emoji);

    private readonly Dictionary<string, EmotionData> _emotions = new()
    {
        ["happy"] = new("#FFD700", "Happy", "😊"),
        ["excited"] = new("#FF6B6B", "Excited", "🎉"),
        ["calm"] = new("#4ECDC4", "Calm", "😌"),
        ["sad"] = new("#95A5C6", "Sad", "😢"),
        ["angry"] = new("#E74C3C", "Angry", "😠"),
        ["fear"] = new("#8E44AD", "Fear", "😨"),
        ["focused"] = new("#3498DB", "Focused", "🎯"),
        ["energetic"] = new("#E67E22", "Energetic", "⚡"),
        ["peaceful"] = new("#27AE60", "Peaceful", "🕊️"),
        ["melancholy"] = new("#7F8C8D", "Melancholy", "🌧️"),
        ["professional"] = new("#34495E", "Professional", "💼"),
        ["joyful"] = new("#F39C12", "Joyful", "🌟"),
        ["default"] = new("#607D8B", "Neutral", "😐")
    };

    private string _currentEmotionKey = "default";

    public EmotionData CurrentEmotion => _emotions[_currentEmotionKey];

    public string? AnalyzeWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return null;
        }

        var upperWord = word.ToUpperInvariant();

        if (ContainsAny(upperWord, "HAPPY", "JOY", "SMILE", "WONDERFUL", "BEAUTIFUL", "AMAZING", "FANTASTIC"))
        {
            return "happy";
        }

        if (ContainsAny(upperWord, "EXCITED", "THRILLED", "INCREDIBLE", "WOW", "AWESOME"))
        {
            return "excited";
        }

        if (ContainsAny(upperWord, "CALM", "PEACEFUL", "RELAX", "TRANQUIL", "SERENE", "GENTLE"))
        {
            return "calm";
        }

        if (ContainsAny(upperWord, "SAD", "MELANCHOLY", "RAIN", "LOST", "MEMORIES", "TEARS"))
        {
            return "sad";
        }

        if (ContainsAny(upperWord, "FEAR", "DANGER", "ANXIETY", "WORRY", "SCARED", "INTENSE"))
        {
            return "fear";
        }

        if (ContainsAny(upperWord, "ANGRY", "FURIOUS", "RAGE", "MAD", "FRUSTRATED"))
        {
            return "angry";
        }

        if (ContainsAny(upperWord, "ENERGETIC", "ENERGY", "TRANSFORM", "EXCITING", "URGENT"))
        {
            return "energetic";
        }

        if (ContainsAny(upperWord, "FOCUS", "CONCENTRATE", "ANALYZE", "PROFESSIONAL", "DATA", "STATISTICAL", "PERFORMANCE"))
        {
            return "professional";
        }

        if (ContainsAny(upperWord, "SERENITY", "BREATH", "FLOW", "WASH"))
        {
            return "peaceful";
        }

        return null;
    }

    public bool UpdateEmotionForWord(string word)
    {
        var nextEmotion = AnalyzeWord(word);
        if (nextEmotion is null)
        {
            if (_currentEmotionKey == "default")
            {
                return false;
            }

            _currentEmotionKey = "default";
            return true;
        }

        if (nextEmotion == _currentEmotionKey)
        {
            return false;
        }

        _currentEmotionKey = nextEmotion;
        return true;
    }

    public void SetEmotion(string emotionKey)
    {
        if (_emotions.ContainsKey(emotionKey))
        {
            _currentEmotionKey = emotionKey;
        }
    }

    public void ResetToDefault()
    {
        _currentEmotionKey = "default";
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            if (text.Contains(keyword, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
