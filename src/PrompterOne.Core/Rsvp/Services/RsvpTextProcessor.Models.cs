using ManagedCode.Tps;

namespace PrompterOne.Core.Services.Rsvp;

public partial class RsvpTextProcessor
{
    public class ProcessedSegment
    {
        public string Title { get; set; } = string.Empty;
        public string Emotion { get; set; } = TpsSpec.DefaultEmotion;
        public int Speed { get; set; } = RsvpTextProcessorDefaults.DefaultSegmentSpeed;
        public List<string> Words { get; set; } = [];
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
    }

    public class ProcessedScript
    {
        public List<ProcessedSegment> Segments { get; set; } = [];
        public List<string> AllWords { get; set; } = [];
        public Dictionary<int, int> WordToSegmentMap { get; set; } = [];
        public Dictionary<int, int> WordSpeedOverrides { get; set; } = [];
        public Dictionary<int, string> WordEmotionOverrides { get; set; } = [];
        public Dictionary<int, string> WordColorOverrides { get; set; } = [];
        public Dictionary<int, int> PauseDurations { get; set; } = [];
        public List<PhraseGroup> PhraseGroups { get; set; } = [];
        public Dictionary<int, string> UpcomingEmotionByStartIndex { get; set; } = [];
    }

    public class PhraseGroup
    {
        public int StartWordIndex { get; set; }
        public int EndWordIndex { get; set; }
        public IReadOnlyList<string> Words { get; set; } = Array.Empty<string>();
        public int EstimatedDurationMs { get; set; }
        public int PauseAfterMs { get; set; }
        public string EmotionHint { get; set; } = TpsSpec.DefaultEmotion;
        public bool ContainsPauseCue { get; set; }
        public bool ContainsEmphasis { get; set; }
    }
}
