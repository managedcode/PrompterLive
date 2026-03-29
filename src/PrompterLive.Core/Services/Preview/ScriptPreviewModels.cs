namespace PrompterLive.Core.Services.Preview;

public class SegmentPreviewModel
{
    public string Title { get; set; } = string.Empty;
    public string? EmotionKey { get; set; }
    public string? Emotion { get; set; }
    public int TargetWpm { get; set; }
    public string BackgroundColor { get; set; } = "#FF3B82F6";
    public string TextColor { get; set; } = "#FFFFFFFF";
    public string? AccentColor { get; set; }
    public string? Content { get; set; }
    public List<BlockPreviewModel> Blocks { get; } = new();
    public List<WordPreviewModel> SegmentWords { get; } = new();
}

public class BlockPreviewModel
{
    public string Title { get; set; } = string.Empty;
    public int TargetWpm { get; set; }
    public string? EmotionKey { get; set; }
    public string? Emotion { get; set; }
    public string Text { get; set; } = string.Empty;
    public List<WordPreviewModel> Words { get; } = new();
}

public class WordPreviewModel
{
    public string Text { get; set; } = string.Empty;
    public string? Color { get; set; }
    public bool IsPause { get; set; }
    public bool IsEmphasis { get; set; }
}
