namespace PrompterLive.Core.Models.HeadCues;

public static class HeadCueCatalog
{
    private static readonly IReadOnlyDictionary<string, string> _emotionToCue = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["neutral"] = "H0",
        ["calm"] = "H0",
        ["professional"] = "H9",
        ["focused"] = "H5",
        ["motivational"] = "H9",
        ["urgent"] = "H4",
        ["concerned"] = "H1",
        ["sad"] = "H1",
        ["warm"] = "H7",
        ["happy"] = "H6",
        ["excited"] = "H6",
        ["energetic"] = "H8"
    };

    public static HeadCueDefinition Neutral => CreateDefinitions()["H0"];

    public static IReadOnlyDictionary<string, HeadCueDefinition> All => CreateDefinitions();

    public static HeadCueDefinition Get(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Neutral;
        }

        return CreateDefinitions().TryGetValue(id, out var definition) ? definition : Neutral;
    }

    public static string ResolveForEmotion(string? emotion)
    {
        if (string.IsNullOrWhiteSpace(emotion))
        {
            return Neutral.Id;
        }

        return _emotionToCue.TryGetValue(emotion, out var cueId) ? cueId : Neutral.Id;
    }

    private static IReadOnlyDictionary<string, HeadCueDefinition> CreateDefinitions() =>
        new Dictionary<string, HeadCueDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["H0"] = new("H0", AppText.Get("HeadCue.Title.H0"), AppText.Get("HeadCue.Description.H0"), 0, 0, 0, "ms-appx:///Assets/HeadCues/h0.png"),
            ["H1"] = new("H1", AppText.Get("HeadCue.Title.H1"), AppText.Get("HeadCue.Description.H1"), 10, 0, 0, "ms-appx:///Assets/HeadCues/h1.png"),
            ["H2"] = new("H2", AppText.Get("HeadCue.Title.H2"), AppText.Get("HeadCue.Description.H2"), 20, 0, 0, "ms-appx:///Assets/HeadCues/h2.png"),
            ["H3"] = new("H3", AppText.Get("HeadCue.Title.H3"), AppText.Get("HeadCue.Description.H3"), -10, 0, 0, "ms-appx:///Assets/HeadCues/h3.png"),
            ["H4"] = new("H4", AppText.Get("HeadCue.Title.H4"), AppText.Get("HeadCue.Description.H4"), -20, 0, 0, "ms-appx:///Assets/HeadCues/h4.png"),
            ["H5"] = new("H5", AppText.Get("HeadCue.Title.H5"), AppText.Get("HeadCue.Description.H5"), 0, -20, 0, "ms-appx:///Assets/HeadCues/h5.png"),
            ["H6"] = new("H6", AppText.Get("HeadCue.Title.H6"), AppText.Get("HeadCue.Description.H6"), 0, 20, 0, "ms-appx:///Assets/HeadCues/h6.png"),
            ["H7"] = new("H7", AppText.Get("HeadCue.Title.H7"), AppText.Get("HeadCue.Description.H7"), 0, -10, -12, "ms-appx:///Assets/HeadCues/h7.png"),
            ["H8"] = new("H8", AppText.Get("HeadCue.Title.H8"), AppText.Get("HeadCue.Description.H8"), 0, 10, 12, "ms-appx:///Assets/HeadCues/h8.png"),
            ["H9"] = new("H9", AppText.Get("HeadCue.Title.H9"), AppText.Get("HeadCue.Description.H9"), -5, 0, 0, "ms-appx:///Assets/HeadCues/h9.png")
        };
}
