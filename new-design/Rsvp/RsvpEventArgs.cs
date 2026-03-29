using System;
using System.Collections.Generic;
using Teleprompter.Models.HeadCues;
using Windows.UI;

namespace Teleprompter.Services.Rsvp;

public class WordDisplayEventArgs : EventArgs
{
    public string PreORP { get; set; } = string.Empty;
    public string OrpChar { get; set; } = string.Empty;
    public string PostORP { get; set; } = string.Empty;
    public string LeftWord1 { get; set; } = string.Empty;
    public string LeftWord2 { get; set; } = string.Empty;
    public string LeftWord3 { get; set; } = string.Empty;
    public string LeftWord4 { get; set; } = string.Empty;
    public string LeftWord5 { get; set; } = string.Empty;
    public string RightWord1 { get; set; } = string.Empty;
    public string RightWord2 { get; set; } = string.Empty;
    public string RightWord3 { get; set; } = string.Empty;
    public string RightWord4 { get; set; } = string.Empty;
    public string RightWord5 { get; set; } = string.Empty;
    public int CurrentWordIndex { get; set; }
    public int TotalWords { get; set; }
    public string EmotionName { get; set; } = string.Empty;
    public string EmotionEmoji { get; set; } = string.Empty;
    public Color EmotionColor { get; set; }
    public string EmotionColorHex { get; set; } = "#FFFFFF";
    public string WordColor { get; set; } = string.Empty;
    public IReadOnlyList<string> PhraseWords { get; set; } = Array.Empty<string>();
    public int PhraseEstimatedDurationMs { get; set; }
    public int PhrasePauseAfterMs { get; set; }
    public bool PhraseContainsPauseCue { get; set; }
    public bool IsPause { get; set; }
    public string UpcomingEmotionName { get; set; } = string.Empty;
    public string UpcomingEmotionEmoji { get; set; } = string.Empty;
    public string UpcomingEmotionColorHex { get; set; } = "#FFFFFF";
    public string HeadCueId { get; set; } = HeadCueCatalog.Neutral.Id;
}

public class PlaybackStateEventArgs : EventArgs
{
    public bool IsPlaying { get; set; }
    public bool IsStopped { get; set; }
}

public class ProgressEventArgs : EventArgs
{
    public int CurrentWordIndex { get; set; }
    public int TotalWords { get; set; }
    public double ProgressPercentage { get; set; }
    public TimeSpan TimeRemaining { get; set; }
    public TimeSpan TimeElapsed { get; set; }
}

public class EmotionChangeEventArgs : EventArgs
{
    public string EmotionName { get; set; } = string.Empty;
    public string EmotionEmoji { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#FFFFFF";
}
