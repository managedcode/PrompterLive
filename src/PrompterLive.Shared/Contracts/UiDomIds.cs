namespace PrompterLive.Shared.Contracts;

public static class UiDomIds
{
    public static class Learn
    {
        public const string HeaderWpmBadge = "rsvp-wpm-badge";
        public const string ContextLeft = "rsvp-ctx-l";
        public const string ContextRight = "rsvp-ctx-r";
        public const string NextPhrase = "rsvp-next-phrase";
        public const string Speed = "rsvp-speed";
        public const string Word = "rsvp-word";
    }

    public static class Teleprompter
    {
        public const string Camera = "rd-camera";
        public const string FontLabel = "rd-font-label";
        public const string HeaderSegment = "rd-header-segment";
        public const string ProgressFill = "rd-progress-fill";
        public const string Time = "rd-time";
        public const string WidthValue = "rd-width-val";

        public static string CameraOverlay(int order) => $"rd-camera-overlay-{order}";
    }
}
