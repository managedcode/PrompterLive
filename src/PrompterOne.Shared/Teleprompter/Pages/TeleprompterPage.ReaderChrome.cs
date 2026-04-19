namespace PrompterOne.Shared.Pages;

public partial class TeleprompterPage
{
    private const string ReaderControlsCssClass = "rd-controls";
    private const string ReaderEdgeInfoCssClass = "rd-edge-info";
    private const string ReaderEdgeProgressCssClass = "rd-edge-progress";
    private const string ReaderProgressShellCssClass = "rd-progress-shell";
    private const string ReaderReadingActiveCssClass = "rd-reading-active";
    private const string ReaderStageCssClass = "rd-stage";

    private string BuildReaderStageCssClass() =>
        BuildReaderChromeCssClass(ReaderStageCssClass);

    private string BuildReaderControlsCssClass() =>
        BuildReaderChromeCssClass(ReaderControlsCssClass);

    private string BuildReaderEdgeInfoCssClass() =>
        BuildReaderChromeCssClass(ReaderEdgeInfoCssClass);

    private string BuildReaderEdgeProgressCssClass() =>
        BuildReaderChromeCssClass(ReaderEdgeProgressCssClass);

    private string BuildReaderProgressShellCssClass() =>
        BuildReaderChromeCssClass(ReaderProgressShellCssClass);

    private string BuildReaderChromeCssClass(string baseCssClass) =>
        BuildClassList(baseCssClass, _isReaderPlaying ? ReaderReadingActiveCssClass : null);
}
