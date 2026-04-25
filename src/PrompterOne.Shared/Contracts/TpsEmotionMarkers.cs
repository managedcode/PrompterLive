namespace PrompterOne.Shared.Contracts;

public static class TpsEmotionMarkers
{
    public static string ResolveMarker(string? emotion) =>
        Normalize(emotion) switch
        {
            "warm" => "😊",
            "concerned" => "😟",
            "focused" => "🎯",
            "motivational" => "💪",
            "urgent" => "🚨",
            "happy" => "😄",
            "excited" => "🚀",
            "sad" => "😢",
            "calm" => "😌",
            "energetic" => "⚡",
            "professional" => "💼",
            "neutral" => "😐",
            _ => string.Empty
        };

    private static string Normalize(string? emotion) =>
        string.IsNullOrWhiteSpace(emotion)
            ? string.Empty
            : emotion.Trim().ToLowerInvariant();
}
