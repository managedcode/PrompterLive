using PrompterOne.Core.Models.Workspace;

namespace PrompterOne.Shared.Pages;

public partial class TeleprompterPage
{
    private Task ToggleReaderOrientationAsync() =>
        SetReaderOrientationAsync(BuildNextReaderOrientation());

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

    private ReaderTextOrientation BuildNextReaderOrientation() =>
        _readerTextOrientation switch
        {
            ReaderTextOrientation.Landscape => ReaderTextOrientation.Portrait,
            ReaderTextOrientation.Portrait => ReaderTextOrientation.Inverted,
            ReaderTextOrientation.Inverted => ReaderTextOrientation.PortraitCounterClockwise,
            _ => ReaderTextOrientation.Landscape
        };
}
