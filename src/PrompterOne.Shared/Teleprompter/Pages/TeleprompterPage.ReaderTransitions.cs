namespace PrompterOne.Shared.Pages;

public partial class TeleprompterPage
{
    private readonly record struct ReaderCardTransitionHandle(CancellationTokenSource Source, CancellationToken Token);

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

    private async Task PrepareReaderCardTransitionAsync(int nextCardIndex, int? explicitDirection = null)
    {
        if (nextCardIndex < 0 || nextCardIndex >= _cards.Count || nextCardIndex == _activeReaderCardIndex)
        {
            return;
        }

        //  Playback wrap (last → 0) and any forward playback advance
        //  always move upward — AGENTS rule: "forward block jumps stay
        //  on the straight reference path". Only an explicit manual
        //  backward jump reverses the motion. The caller passes the
        //  intended direction directly so the wrap case can't
        //  accidentally be classified as backward (which would make
        //  cards descend from above during normal forward playback).
        _readerCardTransitionDirection = explicitDirection
            ?? (nextCardIndex > _activeReaderCardIndex
                ? ReaderCardForwardStep
                : ReaderCardBackwardStep);
        _preparedReaderCardIndex = nextCardIndex;
        _readerCardsWithoutMotionTransition.Add(nextCardIndex);
        await InvokeAsync(StateHasChanged);
        //  Force the browser to actually PAINT the snap-to-starting-
        //  position state before re-enabling the transition. Without
        //  this commit, wrap-around playback (last → card 0) saw the
        //  incoming card animate from its previous rd-card-prev
        //  position (-104%, ABOVE) straight down to 0 — i.e. descending
        //  from the top, the user's "з гори щось спускається" bug.
        //  Task.Yield alone does not guarantee a frame paint; the JS
        //  `commitFrame()` helper resolves only after a double rAF.
        await CommitFrameForCardTransitionAsync();
        _readerCardsWithoutMotionTransition.Remove(nextCardIndex);
    }

    private async Task CommitFrameForCardTransitionAsync()
    {
        try
        {
            await KineticInterop.CommitFrameAsync();
        }
        catch (Microsoft.JSInterop.JSException)
        {
            //  JS bridge unavailable (prerender) — fall back to yield.
            await Task.Yield();
        }
        catch (InvalidOperationException)
        {
            await Task.Yield();
        }
        catch (TaskCanceledException)
        {
        }
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

    private ReaderCardTransitionHandle BeginReaderCardTransitionScope(CancellationToken cancellationToken)
    {
        DisposeReaderCardTransitionCancellationTokenSource();
        var transitionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _readerCardTransitionCts = transitionCts;
        return new ReaderCardTransitionHandle(transitionCts, transitionCts.Token);
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
