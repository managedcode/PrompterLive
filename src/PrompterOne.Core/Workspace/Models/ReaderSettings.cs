namespace PrompterOne.Core.Models.Workspace;

public sealed record ReaderSettings(
    int CountdownSeconds = ReaderSettingsDefaults.CountdownSeconds,
    double FontScale = ReaderSettingsDefaults.FontScale,
    double TextWidth = ReaderSettingsDefaults.TextWidth,
    double ScrollSpeed = ReaderSettingsDefaults.ScrollSpeed,
    bool MirrorText = ReaderSettingsDefaults.MirrorText,
    bool ShowFocusLine = ReaderSettingsDefaults.ShowFocusLine,
    bool ShowProgress = ReaderSettingsDefaults.ShowProgress,
    bool ShowCameraScene = ReaderSettingsDefaults.ShowCameraScene,
    int FocalPointPercent = ReaderSettingsDefaults.FocalPointPercent);
