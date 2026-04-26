using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Storage;

namespace PrompterOne.Shared.Pages;

public partial class TeleprompterPage
{
    private async Task PersistCurrentReaderLayoutAsync()
    {
        await PersistReaderSettingsAsync(currentSettings => currentSettings with
        {
            FontScale = BuildReaderFontScale(_readerFontSize),
            ScrollSpeed = BuildReaderScrollSpeedSetting(_readerPlaybackSpeedWpm),
            TextWidth = BuildReaderTextWidthRatio(_readerTextWidthPercent),
            FocalPointPercent = _readerFocalPointPercent,
            MirrorText = _isReaderMirrorHorizontal,
            MirrorVertical = _isReaderMirrorVertical,
            TextAlignment = _readerTextAlignment,
            TextOrientation = _readerTextOrientation
        });
    }

    private Task PersistReaderCameraPreferenceAsync() =>
        PersistReaderSettingsAsync(currentSettings => currentSettings with
        {
            ShowCameraScene = _isReaderCameraActive
        });

    private async Task ToggleReaderAutoLoopAsync()
    {
        _isReaderAutoLoopEnabled = !_isReaderAutoLoopEnabled;
        await PersistReaderSettingsAsync(currentSettings => currentSettings with
        {
            AutoLoop = _isReaderAutoLoopEnabled
        });
    }

    private async Task SetReaderSpeedCueDisplayModeAsync(ReaderSpeedCueDisplayMode displayMode)
    {
        var normalizedDisplayMode = NormalizeReaderSpeedCueDisplayMode(displayMode);
        if (_readerSpeedCueDisplayMode == normalizedDisplayMode)
        {
            return;
        }

        _readerSpeedCueDisplayMode = normalizedDisplayMode;
        await PersistReaderSettingsAsync(currentSettings => currentSettings with
        {
            SpeedCueDisplayMode = normalizedDisplayMode
        });
        StateHasChanged();
    }

    private async Task PersistReaderSettingsAsync(Func<ReaderSettings, ReaderSettings> update)
    {
        var currentSettings = SessionService.State.ReaderSettings;
        var nextSettings = update(currentSettings);
        if (nextSettings == currentSettings)
        {
            return;
        }

        await SessionService.UpdateReaderSettingsAsync(nextSettings);
        await UserSettingsStore.SaveAsync(BrowserAppSettingsKeys.ReaderSettings, nextSettings);
    }

    private static double BuildReaderFontScale(int fontSize) =>
        Math.Round(fontSize / (double)DefaultReaderFontSize, 2, MidpointRounding.AwayFromZero);

    private static double BuildReaderTextWidthRatio(int textWidthPercent) =>
        Math.Round(textWidthPercent / (double)ReaderMaxTextWidthPercent, 4, MidpointRounding.AwayFromZero);

    private static double BuildReaderScrollSpeedSetting(int speedWpm) =>
        Math.Round((double)speedWpm, 2, MidpointRounding.AwayFromZero);

    private static ReaderSpeedCueDisplayMode NormalizeReaderSpeedCueDisplayMode(ReaderSpeedCueDisplayMode displayMode) =>
        Enum.IsDefined(typeof(ReaderSpeedCueDisplayMode), displayMode)
            ? displayMode
            : ReaderSettingsDefaults.SpeedCueDisplayMode;
}
