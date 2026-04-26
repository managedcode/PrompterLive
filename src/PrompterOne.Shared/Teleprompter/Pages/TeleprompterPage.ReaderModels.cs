using PrompterOne.Shared.Components.Editor;
using PrompterOne.Shared.Contracts;

namespace PrompterOne.Shared.Pages;

public partial class TeleprompterPage
{
    private sealed record ReaderCardSeed(
        string SectionName,
        string DisplayName,
        string EmotionKey,
        string EmotionLabel,
        string EmotionMarker,
        string BackgroundClass,
        string AccentColor,
        int TargetWpm,
        int WordCount,
        int DurationMilliseconds,
        string WidthPercentString,
        string EdgeColor,
        IReadOnlyList<ReaderChunkViewModel> Chunks,
        IReadOnlyList<EditorBlockAttachmentViewModel> Attachments);

    private sealed record ReaderCardViewModel(
        string SectionName,
        string DisplayName,
        string EmotionKey,
        string EmotionLabel,
        string EmotionMarker,
        string BackgroundClass,
        string AccentColor,
        int TargetWpm,
        int WordCount,
        int DurationMilliseconds,
        string WidthPercentString,
        string EdgeColor,
        IReadOnlyList<ReaderChunkViewModel> Chunks,
        IReadOnlyList<EditorBlockAttachmentViewModel> Attachments,
        string TestId);

    private abstract record ReaderChunkViewModel;

    private sealed record ReaderGroupViewModel(
        IReadOnlyList<ReaderWordViewModel> Words,
        bool IsEmphasis,
        bool IsHighlight) : ReaderChunkViewModel;

    private sealed record ReaderEditPointViewModel(string Priority, string CssClass) : ReaderChunkViewModel;

    private sealed record ReaderPauseViewModel(int DurationMs, string CssClass, string PauseKind) : ReaderChunkViewModel;

    private sealed record ReaderWordViewModel(
        string Text,
        string CssClass,
        int DurationMs,
        int PauseAfterMs = 0,
        string? Style = null,
        string? OriginalText = null,
        string? PronunciationGuide = null,
        string? EmotionMarker = null,
        string? EmotionLabel = null,
        int? EffectiveWpm = null,
        IReadOnlyDictionary<string, object>? Attributes = null);

    private sealed record ReaderCameraLayerViewModel(
        string ElementId,
        string DeviceId,
        bool AutoStart,
        string Role,
        int Order,
        string CssClass,
        string BaseTransform,
        string TestId)
    {
        public static ReaderCameraLayerViewModel Placeholder { get; } = new(
            ElementId: UiDomIds.Teleprompter.Camera,
            DeviceId: string.Empty,
            AutoStart: false,
            Role: "primary",
            Order: 0,
            CssClass: "rd-camera",
            BaseTransform: string.Empty,
            TestId: UiTestIds.Teleprompter.CameraBackground);
    }
}
