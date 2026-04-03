using PrompterOne.Core.Models.Workspace;

namespace PrompterOne.Shared.Pages;

public partial class TeleprompterPage
{
    private Task ToggleReaderOrientationAsync() =>
        SetReaderOrientationAsync(
            _readerTextOrientation == ReaderTextOrientation.Landscape
                ? ReaderTextOrientation.Portrait
                : ReaderTextOrientation.Landscape);

    private async Task SetReaderOrientationAsync(ReaderTextOrientation textOrientation)
    {
        if (_readerTextOrientation == textOrientation)
        {
            return;
        }

        _readerTextOrientation = textOrientation;
        RequestReaderAlignment(instant: true);
        await PersistCurrentReaderLayoutAsync();
    }
}
