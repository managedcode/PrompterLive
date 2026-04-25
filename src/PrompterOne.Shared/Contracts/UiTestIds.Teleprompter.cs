namespace PrompterOne.Shared.Contracts;

public static partial class UiTestIds
{
    public static class Teleprompter
    {
        public const string ActiveWord = "teleprompter-active-word";
        public const string Back = "teleprompter-back";
        public const string AlignmentCenter = "teleprompter-alignment-center";
        public const string AlignmentControls = "teleprompter-alignment-controls";
        public const string AlignmentJustify = "teleprompter-alignment-justify";
        public const string AlignmentLeft = "teleprompter-alignment-left";
        public const string AlignmentRight = "teleprompter-alignment-right";
        public const string AlignmentTooltipCenterKey = "alignment-center";
        public const string AlignmentTooltipFontSizeKey = "font-size-slider";
        public const string AlignmentTooltipFocalKey = "focal-slider";
        public const string AlignmentTooltipFullscreenKey = "fullscreen";
        public const string AlignmentTooltipJustifyKey = "alignment-justify";
        public const string AlignmentTooltipLeftKey = "alignment-left";
        public const string AlignmentTooltipMirrorHorizontalKey = "mirror-horizontal";
        public const string AlignmentTooltipMirrorVerticalKey = "mirror-vertical";
        public const string AlignmentTooltipOrientationKey = "orientation";
        public const string AlignmentTooltipRightKey = "alignment-right";
        public const string AlignmentTooltipWidthKey = "width-slider";
        public const string CameraBackground = "teleprompter-camera-layer-primary";
        public const string CameraToggle = "teleprompter-camera-toggle";
        public const string ClusterWrap = "teleprompter-cluster-wrap";
        public const string Controls = "teleprompter-controls";
        public const string EdgeSection = "teleprompter-edge-section";
        public const string EdgeInfo = "teleprompter-edge-info";
        public const string FocalSlider = "teleprompter-focal-slider";
        public const string FocalGuide = "teleprompter-focal-guide";
        public const string FontDown = "teleprompter-font-down";
        public const string FontSlider = "teleprompter-font-slider";
        public const string FontUp = "teleprompter-font-up";
        public const string FullscreenToggle = "teleprompter-fullscreen-toggle";
        public const string MirrorControls = "teleprompter-mirror-controls";
        public const string MirrorHorizontalToggle = "teleprompter-mirror-horizontal";
        public const string MirrorVerticalToggle = "teleprompter-mirror-vertical";
        public const string NextBlock = "teleprompter-next-block";
        public const string NextWord = "teleprompter-next-word";
        public const string OrientationToggle = "teleprompter-orientation-toggle";
        public const string Page = "teleprompter-page";
        public const string PlayToggle = "teleprompter-play-toggle";
        public const string PreviousBlock = "teleprompter-previous-block";
        public const string PreviousWord = "teleprompter-previous-word";
        public const string Progress = "teleprompter-progress";
        public const string ProgressFill = "teleprompter-progress-fill";
        public const string ProgressLabel = "teleprompter-progress-label";
        public const string ProgressSegments = "teleprompter-progress-segments";
        public const string SpeedDown = "teleprompter-speed-down";
        public const string SpeedCueDisplayMode = "teleprompter-speed-cue-display-mode";
        public const string SpeedCueDisplayMultiplier = "teleprompter-speed-cue-display-multiplier";
        public const string SpeedCueDisplayTooltipKey = "speed-cue-display";
        public const string SpeedCueDisplayWpm = "teleprompter-speed-cue-display-wpm";
        public const string SpeedUp = "teleprompter-speed-up";
        public const string SpeedValue = "teleprompter-speed-value";
        public const string Sliders = "teleprompter-sliders";
        public const string Stage = "teleprompter-stage";
        public const string TimeValue = "teleprompter-time";
        public const string BlockIndicator = "teleprompter-block-indicator";
        public const string FontLabel = "teleprompter-font-label";
        public const string FontValue = "teleprompter-font-value";
        public const string Gradient = "teleprompter-gradient";
        public const string PauseIcon = "teleprompter-pause-icon";
        public const string PlayIcon = "teleprompter-play-icon";
        public const string WidthValue = "teleprompter-width-value";
        public const string WidthSlider = "teleprompter-width-slider";

        public static string Card(int index) => $"teleprompter-card-{index}";

        public static string CardCluster(int index) => $"{Card(index)}-cluster";

        public static string CardGroup(int cardIndex, int groupIndex) => $"teleprompter-card-group-{cardIndex}-{groupIndex}";

        public static string CardGroupPrefix(int cardIndex) => $"teleprompter-card-group-{cardIndex}-";

        public static string CardText(int index) => $"{Card(index)}-text";

        public static string CardWord(int cardIndex, int groupIndex, int wordIndex) =>
            $"teleprompter-card-word-{cardIndex}-{groupIndex}-{wordIndex}";

        public static string CardWordEmotionMarker(int cardIndex, int groupIndex, int wordIndex) =>
            $"teleprompter-emotion-marker-{cardIndex}-{groupIndex}-{wordIndex}";

        public static string CardWordEmotionMarkerPrefix(int cardIndex) => $"teleprompter-emotion-marker-{cardIndex}-";

        public static string CardWordPrefix(int cardIndex) => $"teleprompter-card-word-{cardIndex}-";

        public static string CardWordSpeedCueLabel(int cardIndex, int groupIndex, int wordIndex) =>
            $"teleprompter-speed-cue-label-{cardIndex}-{groupIndex}-{wordIndex}";

        public static string CardWordSpeedCueLabelPrefix(int cardIndex) => $"teleprompter-speed-cue-label-{cardIndex}-";

        public static string ProgressSegmentFill(int index) => $"teleprompter-progress-segment-fill-{index}";

        public static string RailTooltip(string key) => $"teleprompter-rail-tooltip-{key}";
    }
}
