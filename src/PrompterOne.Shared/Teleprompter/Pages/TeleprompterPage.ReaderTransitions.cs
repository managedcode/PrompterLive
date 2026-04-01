namespace PrompterOne.Shared.Pages;

public partial class TeleprompterPage
{
    private const string ReaderCardNoTransitionCssClass = "rd-card-static";

    private readonly HashSet<int> _readerCardsWithoutMotionTransition = [];
    private int? _preparedReaderCardIndex;
    private int? _readerTransitionSourceCardIndex;

    private void ResetReaderCardTransitionState()
    {
        _readerCardsWithoutMotionTransition.Clear();
        _preparedReaderCardIndex = null;
        _readerTransitionSourceCardIndex = null;
    }

    private async Task PrepareReaderCardTransitionAsync(int nextCardIndex)
    {
        if (nextCardIndex < 0 || nextCardIndex >= _cards.Count || nextCardIndex == _activeReaderCardIndex)
        {
            return;
        }

        _preparedReaderCardIndex = nextCardIndex;
        _readerCardsWithoutMotionTransition.Add(nextCardIndex);
        await InvokeAsync(StateHasChanged);
        await Task.Yield();
        _readerCardsWithoutMotionTransition.Remove(nextCardIndex);
    }

    private async Task FinalizeReaderCardTransitionAsync(int previousCardIndex)
    {
        if (_readerTransitionSourceCardIndex != previousCardIndex)
        {
            return;
        }

        _readerCardsWithoutMotionTransition.Add(previousCardIndex);
        _readerTransitionSourceCardIndex = null;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();
        _readerCardsWithoutMotionTransition.Remove(previousCardIndex);
        await InvokeAsync(StateHasChanged);
    }
}
