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
            TextWidth = BuildReaderTextWidthRatio(_readerTextWidth),
            FocalPointPercent = _readerFocalPointPercent,
            MirrorText = _isReaderMirrorHorizontal,
            MirrorVertical = _isReaderMirrorVertical,
            TextOrientation = _readerTextOrientation
        });
    }

    private Task PersistReaderCameraPreferenceAsync() =>
        PersistReaderSettingsAsync(currentSettings => currentSettings with
        {
            ShowCameraScene = _isReaderCameraActive
        });

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

    private static double BuildReaderTextWidthRatio(int textWidth) =>
        Math.Round(textWidth / (double)ReaderMaxTextWidth, 4, MidpointRounding.AwayFromZero);
}
