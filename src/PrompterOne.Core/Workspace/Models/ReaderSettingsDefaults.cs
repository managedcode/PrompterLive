namespace PrompterOne.Core.Models.Workspace;

public static class ReaderSettingsDefaults
{
    public const int CountdownSeconds = 3;
    public const double FontScale = 1.0;
    public const double TextWidth = 1.0;
    public const double ScrollSpeed = 1.0;
    public const bool MirrorText = false;
    public const bool MirrorVertical = false;
    public const ReaderTextOrientation TextOrientation = ReaderTextOrientation.Landscape;
    public const ReaderTextAlignment TextAlignment = ReaderTextAlignment.Left;
    public const bool ShowFocusLine = true;
    public const bool ShowProgress = true;
    public const bool ShowCameraScene = false;
    public const bool AutoLoop = true;
    public const int FocalPointPercent = 30;
    public const ReaderSpeedCueDisplayMode SpeedCueDisplayMode = ReaderSpeedCueDisplayMode.WordsPerMinute;
}
