using System.Collections.Generic;
using Windows.UI;

namespace Teleprompter.Services.Rsvp;

/// <summary>
/// Analyzes words for emotional context and provides voice guidance
/// Used for teleprompter voice coaching and visual feedback
/// </summary>
public class RsvpEmotionAnalyzer
{
    /// <summary>
    /// Emotion data with color and description
    /// </summary>
    public record EmotionData(string ColorHex, string Name, string Emoji);

    private readonly Dictionary<string, EmotionData> _emotions = new()
    {
        ["happy"] = new EmotionData("#FFD700", "Happy", "😊"),
        ["excited"] = new EmotionData("#FF6B6B", "Excited", "🎉"),
        ["calm"] = new EmotionData("#4ECDC4", "Calm", "😌"),
        ["sad"] = new EmotionData("#95A5C6", "Sad", "😢"),
        ["angry"] = new EmotionData("#E74C3C", "Angry", "😠"),
        ["fear"] = new EmotionData("#8E44AD", "Fear", "😨"),
        ["focused"] = new EmotionData("#3498DB", "Focused", "🎯"),
        ["energetic"] = new EmotionData("#E67E22", "Energetic", "⚡"),
        ["peaceful"] = new EmotionData("#27AE60", "Peaceful", "🕊️"),
        ["melancholy"] = new EmotionData("#7F8C8D", "Melancholy", "🌧️"),
        ["professional"] = new EmotionData("#34495E", "Professional", "💼"),
        ["joyful"] = new EmotionData("#F39C12", "Joyful", "🌟"),
        ["default"] = new EmotionData("#607D8B", "Neutral", "😐")
    };

    private string _currentEmotionKey = "default";

    /// <summary>
    /// Gets the current emotion data
    /// </summary>
    public EmotionData CurrentEmotion => _emotions[_currentEmotionKey];

    /// <summary>
    /// Analyzes a word and returns the appropriate emotion key
    /// </summary>
    /// <param name="word">Word to analyze</param>
    /// <returns>Emotion key if emotion should change, null otherwise</returns>
    public string? AnalyzeWord(string word)
    {
        if (string.IsNullOrEmpty(word))
            return null;

        var upperWord = word.ToUpper();

        // Happy/Joyful emotions
        if (ContainsAny(upperWord, "HAPPY", "JOY", "SMILE", "WONDERFUL", "BEAUTIFUL", "AMAZING", "FANTASTIC"))
        {
            return "happy";
        }
        // Excited emotions
        else if (ContainsAny(upperWord, "EXCITED", "THRILLED", "INCREDIBLE", "WOW", "AWESOME"))
        {
            return "excited";
        }
        // Calm/Peaceful emotions
        else if (ContainsAny(upperWord, "CALM", "PEACEFUL", "RELAX", "TRANQUIL", "SERENE", "GENTLE"))
        {
            return "calm";
        }
        // Sad emotions
        else if (ContainsAny(upperWord, "SAD", "MELANCHOLY", "RAIN", "LOST", "MEMORIES", "TEARS"))
        {
            return "sad";
        }
        // Fear/Anxiety emotions
        else if (ContainsAny(upperWord, "FEAR", "DANGER", "ANXIETY", "WORRY", "SCARED", "INTENSE"))
        {
            return "fear";
        }
        // Angry emotions
        else if (ContainsAny(upperWord, "ANGRY", "FURIOUS", "RAGE", "MAD", "FRUSTRATED"))
        {
            return "angry";
        }
        // Energetic emotions
        else if (ContainsAny(upperWord, "ENERGETIC", "ENERGY", "TRANSFORM", "EXCITING", "URGENT"))
        {
            return "energetic";
        }
        // Professional/Focused
        else if (ContainsAny(upperWord, "FOCUS", "CONCENTRATE", "ANALYZE", "PROFESSIONAL", "DATA", "STATISTICAL", "PERFORMANCE"))
        {
            return "professional";
        }
        // Peaceful
        else if (ContainsAny(upperWord, "SERENITY", "BREATH", "FLOW", "WASH"))
        {
            return "peaceful";
        }

        return null; // No emotion change needed
    }

    /// <summary>
    /// Updates the current emotion if word analysis suggests a change
    /// </summary>
    /// <param name="word">Word to analyze</param>
    /// <returns>True if emotion changed</returns>
    public bool UpdateEmotionForWord(string word)
    {
        var newEmotionKey = AnalyzeWord(word);
        if (newEmotionKey != null)
        {
            if (newEmotionKey != _currentEmotionKey)
            {
                _currentEmotionKey = newEmotionKey;
                return true;
            }
        }
        else
        {
            // If no emotion detected, reset to default if not already default
            if (_currentEmotionKey != "default")
            {
                _currentEmotionKey = "default";
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Sets emotion directly
    /// </summary>
    /// <param name="emotionKey">Emotion key to set</param>
    public void SetEmotion(string emotionKey)
    {
        if (_emotions.ContainsKey(emotionKey))
        {
            _currentEmotionKey = emotionKey;
        }
    }

    /// <summary>
    /// Resets to default emotion
    /// </summary>
    public void ResetToDefault()
    {
        _currentEmotionKey = "default";
    }

    /// <summary>
    /// Gets emotion color as Windows.UI.Color
    /// </summary>
    public Color GetEmotionColor()
    {
        return ColorFromHex(CurrentEmotion.ColorHex);
    }

    /// <summary>
    /// Converts hex color string to Windows.UI.Color
    /// </summary>
    private Color ColorFromHex(string hex)
    {
        hex = hex.Replace("#", "");
        var r = System.Convert.ToByte(hex.Substring(0, 2), 16);
        var g = System.Convert.ToByte(hex.Substring(2, 2), 16);
        var b = System.Convert.ToByte(hex.Substring(4, 2), 16);
        return Color.FromArgb(255, r, g, b);
    }

    /// <summary>
    /// Helper to check if text contains any of the given keywords
    /// </summary>
    private bool ContainsAny(string text, params string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            if (text.Contains(keyword))
                return true;
        }
        return false;
    }
}