namespace PrompterOne.Shared.Pages;

public partial class TeleprompterPage
{
    private const string ReaderCardNoTransitionCssClass = "rd-card-static";

    private readonly HashSet<int> _readerCardsWithoutMotionTransition = [];
    private CancellationTokenSource? _readerCardTransitionCts;
    private int? _readerCardTransitionDirection;
    private int? _preparedReaderCardIndex;
    private int? _readerTransitionSourceCardIndex;

    private void ResetReaderCardTransitionState()
    {
        DisposeReaderCardTransitionCancellationTokenSource();
        _readerCardsWithoutMotionTransition.Clear();
        _readerCardTransitionDirection = null;
        _preparedReaderCardIndex = null;
        _readerTransitionSourceCardIndex = null;
    }

    private async Task PrepareReaderCardTransitionAsync(int nextCardIndex)
    {
        if (nextCardIndex < 0 || nextCardIndex >= _cards.Count || nextCardIndex == _activeReaderCardIndex)
        {
            return;
        }

        _readerCardTransitionDirection = nextCardIndex > _activeReaderCardIndex
            ? ReaderCardForwardStep
            : ReaderCardBackwardStep;
        _preparedReaderCardIndex = nextCardIndex;
        _readerCardsWithoutMotionTransition.Add(nextCardIndex);
        await InvokeAsync(StateHasChanged);
        await Task.Yield();
        _readerCardsWithoutMotionTransition.Remove(nextCardIndex);
    }

    private async Task CancelPendingReaderCardTransitionAsync()
    {
        var affectedCardIndexes = GetAffectedReaderCardTransitionIndexes();
        if (affectedCardIndexes.Count == 0 && _readerCardTransitionCts is null)
        {
            return;
        }

        DisposeReaderCardTransitionCancellationTokenSource();
        await NormalizeReaderCardTransitionStateAsync(affectedCardIndexes);
    }

    private CancellationTokenSource BeginReaderCardTransitionScope(CancellationToken cancellationToken)
    {
        DisposeReaderCardTransitionCancellationTokenSource();
        _readerCardTransitionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        return _readerCardTransitionCts;
    }

    private void CompleteReaderCardTransitionScope(CancellationTokenSource transitionCts)
    {
        if (ReferenceEquals(_readerCardTransitionCts, transitionCts))
        {
            _readerCardTransitionCts = null;
        }

        transitionCts.Dispose();
    }

    private async Task FinalizeReaderCardTransitionAsync(int previousCardIndex)
    {
        if (_readerTransitionSourceCardIndex != previousCardIndex)
        {
            return;
        }

        _readerCardsWithoutMotionTransition.Add(previousCardIndex);
        _readerCardTransitionDirection = null;
        _readerTransitionSourceCardIndex = null;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();
        _readerCardsWithoutMotionTransition.Remove(previousCardIndex);
        await InvokeAsync(StateHasChanged);
    }

    private async Task NormalizeReaderCardTransitionStateAsync(IReadOnlyCollection<int> affectedCardIndexes)
    {
        foreach (var cardIndex in affectedCardIndexes)
        {
            _readerCardsWithoutMotionTransition.Add(cardIndex);
        }

        _readerCardTransitionDirection = null;
        _preparedReaderCardIndex = null;
        _readerTransitionSourceCardIndex = null;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        foreach (var cardIndex in affectedCardIndexes)
        {
            _readerCardsWithoutMotionTransition.Remove(cardIndex);
        }

        await InvokeAsync(StateHasChanged);
    }

    private HashSet<int> GetAffectedReaderCardTransitionIndexes()
    {
        var indexes = new HashSet<int>();
        AddIfValid(indexes, _activeReaderCardIndex);
        AddIfValid(indexes, _preparedReaderCardIndex);
        AddIfValid(indexes, _readerTransitionSourceCardIndex);
        return indexes;
    }

    private void DisposeReaderCardTransitionCancellationTokenSource()
    {
        if (_readerCardTransitionCts is null)
        {
            return;
        }

        _readerCardTransitionCts.Cancel();
        _readerCardTransitionCts.Dispose();
        _readerCardTransitionCts = null;
    }

    private void AddIfValid(ISet<int> indexes, int? cardIndex)
    {
        if (cardIndex is not null && cardIndex.Value >= 0 && cardIndex.Value < _cards.Count)
        {
            indexes.Add(cardIndex.Value);
        }
    }
}
