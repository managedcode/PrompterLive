using PrompterOne.Shared.Contracts;

namespace PrompterOne.Shared.Pages;

public partial class TeleprompterPage
{
    private sealed record ReaderCardSeed(
        string SectionName,
        string DisplayName,
        string EmotionKey,
        string EmotionLabel,
        string BackgroundClass,
        string AccentColor,
        int TargetWpm,
        int WordCount,
        int DurationMilliseconds,
        string WidthPercentString,
        string EdgeColor,
        IReadOnlyList<ReaderChunkViewModel> Chunks);

    private sealed record ReaderCardViewModel(
        string SectionName,
        string DisplayName,
        string EmotionKey,
        string EmotionLabel,
        string BackgroundClass,
        string AccentColor,
        int TargetWpm,
        int WordCount,
        int DurationMilliseconds,
        string WidthPercentString,
        string EdgeColor,
        IReadOnlyList<ReaderChunkViewModel> Chunks,
        string TestId);

    private abstract record ReaderChunkViewModel;

    private sealed record ReaderGroupViewModel(IReadOnlyList<ReaderWordViewModel> Words, bool IsEmphasis) : ReaderChunkViewModel;

    private sealed record ReaderPauseViewModel(int DurationMs, string CssClass) : ReaderChunkViewModel;

    private sealed record ReaderWordViewModel(
        string Text,
        string CssClass,
        int DurationMs,
        int PauseAfterMs = 0,
        string? Style = null,
        string? Title = null,
        string? PronunciationGuide = null,
        int? EffectiveWpm = null,
        IReadOnlyDictionary<string, object>? Attributes = null);

    private sealed record ReaderCameraLayerViewModel(
        string ElementId,
        string DeviceId,
        bool AutoStart,
        string Role,
        int Order,
        string CssClass,
        string Style,
        string TestId)
    {
        public static ReaderCameraLayerViewModel Placeholder { get; } = new(
            ElementId: UiDomIds.Teleprompter.Camera,
            DeviceId: string.Empty,
            AutoStart: false,
            Role: "primary",
            Order: 0,
            CssClass: "rd-camera",
            Style: string.Empty,
            TestId: UiTestIds.Teleprompter.CameraBackground);
    }
}
