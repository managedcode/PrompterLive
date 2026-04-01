using Microsoft.AspNetCore.Components;
using PrompterOne.Shared.Contracts;

namespace PrompterOne.Shared.Pages;

public partial class TeleprompterPage
{
    private const int MinimumReaderLoopDelayMilliseconds = 120;
    private const int ReaderCardTransitionMilliseconds = 850;

    private Task DecreaseReaderFontSizeAsync() => ChangeReaderFontSizeAsync(-ReaderFontStep);

    private Task IncreaseReaderFontSizeAsync() => ChangeReaderFontSizeAsync(ReaderFontStep);

    private Task StepReaderBackwardAsync() => StepReaderWordAsync(ReaderBackwardStep);

    private Task StepReaderForwardAsync() => StepReaderWordAsync(ReaderForwardStep);

    private Task JumpToPreviousReaderCardAsync() => JumpReaderCardAsync(ReaderCardBackwardStep);

    private Task JumpToNextReaderCardAsync() => JumpReaderCardAsync(ReaderCardForwardStep);

    private async Task NavigateBackToEditorAsync()
    {
        StopReaderPlaybackLoop();
        var route = string.IsNullOrWhiteSpace(SessionService.State.ScriptId)
            ? AppRoutes.Editor
            : AppRoutes.EditorWithId(SessionService.State.ScriptId);
        Navigation.NavigateTo(route);
        await Task.CompletedTask;
    }

    private async Task ChangeReaderFontSizeAsync(int delta)
    {
        var nextFontSize = Math.Clamp(_readerFontSize + delta, ReaderMinFontSize, ReaderMaxFontSize);
        if (nextFontSize == _readerFontSize)
        {
            return;
        }
        _readerFontSize = nextFontSize;
        RequestReaderAlignment(instant: true);
        await PersistCurrentReaderLayoutAsync();
    }

    private async Task HandleReaderFocalPointInputAsync(ChangeEventArgs args)
    {
        _readerFocalPointPercent = ParseReaderControlValue(
            args.Value,
            ReaderMinFocalPointPercent,
            ReaderMaxFocalPointPercent,
            _readerFocalPointPercent);
        RequestReaderAlignment();
        await PersistCurrentReaderLayoutAsync();
        _isFocalGuideActive = true;
        _focalGuideVersion++;
        var guideVersion = _focalGuideVersion;
        await Task.Delay(ReaderGuideActiveDurationMilliseconds);

        if (_focalGuideVersion == guideVersion)
        {
            _isFocalGuideActive = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task HandleReaderTextWidthInputAsync(ChangeEventArgs args)
    {
        _readerTextWidth = ParseReaderControlValue(
            args.Value,
            ReaderMinTextWidth,
            ReaderMaxTextWidth,
            _readerTextWidth);
        RequestReaderAlignment();
        await PersistCurrentReaderLayoutAsync();
        _areWidthGuidesActive = true;
        _widthGuideVersion++;
        var widthGuideVersion = _widthGuideVersion;
        await Task.Delay(ReaderGuideActiveDurationMilliseconds);

        if (_widthGuideVersion == widthGuideVersion)
        {
            _areWidthGuidesActive = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task ToggleReaderPlaybackAsync()
    {
        if (_cards.Count == 0 || _isReaderCountdownActive)
        {
            return;
        }

        if (_isReaderPlaying)
        {
            StopReaderPlaybackLoop();
            return;
        }

        if (_activeReaderWordIndex >= 0)
        {
            RestartReaderPlaybackLoop(GetCurrentWordDelayMilliseconds());
            return;
        }

        await StartReaderCountdownAsync();
    }

    private async Task StartReaderCountdownAsync()
    {
        StopReaderPlaybackLoop(keepPlaybackState: true);
        _readerPlaybackCts = new CancellationTokenSource();
        var cancellationToken = _readerPlaybackCts.Token;

        try
        {
            _isReaderCountdownActive = true;
            _countdownValue = null;
            await InvokeAsync(StateHasChanged);

            await Task.Delay(ReaderCountdownPreDelayMilliseconds, cancellationToken);

            for (var countdown = 3; countdown >= 1; countdown--)
            {
                _countdownValue = countdown;
                await InvokeAsync(StateHasChanged);
                await Task.Delay(ReaderCountdownStepMilliseconds, cancellationToken);
            }

            _isReaderCountdownActive = false;
            _countdownValue = null;
            await InvokeAsync(StateHasChanged);

            await Task.Delay(ReaderFirstWordDelayMilliseconds, cancellationToken);

            await PrepareReaderCardAlignmentAsync(_activeReaderCardIndex, 0);
            _activeReaderWordIndex = 0;
            UpdateReaderDisplayState();
            _isReaderPlaying = true;
            await InvokeAsync(StateHasChanged);
            _ = RunReaderPlaybackLoopAsync(GetCurrentWordDelayMilliseconds(), cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task StepReaderWordAsync(int direction)
    {
        if (_cards.Count == 0)
        {
            return;
        }

        var resumePlayback = _isReaderPlaying;
        StopReaderPlaybackLoop(keepPlaybackState: true);

        if (direction < 0)
        {
            if (_activeReaderWordIndex > 0)
            {
                _activeReaderWordIndex--;
                UpdateReaderDisplayState();
            }
        }
        else if (_activeReaderWordIndex < 0)
        {
            _activeReaderWordIndex = 0;
            UpdateReaderDisplayState();
        }
        else
        {
            var wordCount = GetCardWordCount(_cards[_activeReaderCardIndex]);
            if (_activeReaderWordIndex < wordCount - 1)
            {
                _activeReaderWordIndex++;
                UpdateReaderDisplayState();
            }
            else
            {
                await AdvanceToCardAsync(GetNextPlaybackCardIndex(), CancellationToken.None);
            }
        }

        if (resumePlayback)
        {
            RestartReaderPlaybackLoop(GetCurrentWordDelayMilliseconds());
        }
    }

    private async Task JumpReaderCardAsync(int direction)
    {
        if (_cards.Count == 0)
        {
            return;
        }

        var resumePlayback = _isReaderPlaying;
        StopReaderPlaybackLoop(keepPlaybackState: true);
        if (direction < 0 && _activeReaderWordIndex > 1)
        {
            await PrepareReaderCardAlignmentAsync(_activeReaderCardIndex, 0);
            _activeReaderWordIndex = 0;
            UpdateReaderDisplayState();

            if (resumePlayback)
            {
                RestartReaderPlaybackLoop(GetCurrentWordDelayMilliseconds());
            }

            return;
        }

        var nextCardIndex = _activeReaderCardIndex + direction;
        if (nextCardIndex < 0 || nextCardIndex >= _cards.Count)
        {
            if (resumePlayback)
            {
                RestartReaderPlaybackLoop(GetCurrentWordDelayMilliseconds());
            }

            return;
        }

        await AdvanceToCardAsync(nextCardIndex, CancellationToken.None);

        if (resumePlayback)
        {
            RestartReaderPlaybackLoop(GetCurrentWordDelayMilliseconds());
        }
    }

    private async Task ToggleReaderCameraAsync()
    {
        _isReaderCameraActive = !_isReaderCameraActive;
        _cameraLayer = _cameraLayer with { AutoStart = _isReaderCameraActive };

        if (_isReaderCameraActive)
        {
            await AttachReaderCameraAsync();
        }
        else
        {
            await DetachReaderCameraAsync();
        }

        await PersistReaderCameraPreferenceAsync();
    }

    private async Task AttachReaderCameraAsync()
    {
        if (string.IsNullOrWhiteSpace(_cameraLayer.DeviceId))
        {
            _isReaderCameraActive = false;
            return;
        }

        await CameraPreviewInterop.AttachCameraAsync(_cameraLayer.ElementId, _cameraLayer.DeviceId);
    }

    private Task DetachReaderCameraAsync() =>
        CameraPreviewInterop.DetachCameraAsync(_cameraLayer.ElementId);

    private void RestartReaderPlaybackLoop(int initialDelayMilliseconds)
    {
        StopReaderPlaybackLoop(keepPlaybackState: true);
        _readerPlaybackCts = new CancellationTokenSource();
        _isReaderPlaying = true;
        _ = RunReaderPlaybackLoopAsync(initialDelayMilliseconds, _readerPlaybackCts.Token);
    }

    private void StopReaderPlaybackLoop(bool keepPlaybackState = false)
    {
        _readerPlaybackCts?.Cancel();
        _readerPlaybackCts?.Dispose();
        _readerPlaybackCts = null;
        _isReaderCountdownActive = false;
        _countdownValue = null;

        if (!keepPlaybackState)
        {
            _isReaderPlaying = false;
        }
    }

    private async Task RunReaderPlaybackLoopAsync(int initialDelayMilliseconds, CancellationToken cancellationToken)
    {
        try
        {
            var delayMilliseconds = Math.Max(MinimumReaderLoopDelayMilliseconds, initialDelayMilliseconds);

            while (!cancellationToken.IsCancellationRequested && _isReaderPlaying && _cards.Count > 0)
            {
                await Task.Delay(delayMilliseconds, cancellationToken);

                if (cancellationToken.IsCancellationRequested || !_isReaderPlaying)
                {
                    break;
                }

                var nextDelayMilliseconds = MinimumReaderLoopDelayMilliseconds;
                await InvokeAsync(async () => nextDelayMilliseconds = await AdvanceReaderPlaybackAsync(cancellationToken));
                delayMilliseconds = nextDelayMilliseconds;
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task<int> AdvanceReaderPlaybackAsync(CancellationToken cancellationToken)
    {
        if (_cards.Count == 0)
        {
            return MinimumReaderLoopDelayMilliseconds;
        }

        var activeCard = _cards[_activeReaderCardIndex];
        var cardWordCount = GetCardWordCount(activeCard);
        if (cardWordCount == 0)
        {
            return MinimumReaderLoopDelayMilliseconds;
        }

        if (_activeReaderWordIndex < cardWordCount - 1)
        {
            _activeReaderWordIndex++;
            UpdateReaderDisplayState();
            await InvokeAsync(StateHasChanged);
            return GetCurrentWordDelayMilliseconds();
        }

        await AdvanceToCardAsync(GetNextPlaybackCardIndex(), cancellationToken);
        return GetCurrentWordDelayMilliseconds();
    }

    private async Task AdvanceToCardAsync(int nextCardIndex, CancellationToken cancellationToken)
    {
        var previousCardIndex = _activeReaderCardIndex;
        await PrepareReaderCardTransitionAsync(nextCardIndex);
        await PrepareReaderCardAlignmentAsync(nextCardIndex, 0);
        _readerTransitionSourceCardIndex = previousCardIndex;
        _activeReaderCardIndex = nextCardIndex;
        _activeReaderWordIndex = -1;
        _preparedReaderCardIndex = null;
        UpdateReaderDisplayState();
        await InvokeAsync(StateHasChanged);

        try
        {
            await Task.Delay(ReaderCardTransitionMilliseconds, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            _activeReaderWordIndex = 0;
            UpdateReaderDisplayState();
            await InvokeAsync(StateHasChanged);
        }
        finally
        {
            await FinalizeReaderCardTransitionAsync(previousCardIndex);
        }
    }

    private int GetCurrentWordDelayMilliseconds()
    {
        if (_cards.Count == 0 || _activeReaderCardIndex >= _cards.Count)
        {
            return MinimumReaderLoopDelayMilliseconds;
        }

        var remainingIndex = _activeReaderWordIndex;
        foreach (var chunk in _cards[_activeReaderCardIndex].Chunks)
        {
            if (chunk is not ReaderGroupViewModel group)
            {
                continue;
            }

            foreach (var word in group.Words)
            {
                if (remainingIndex == 0)
                {
                    return Math.Max(MinimumReaderLoopDelayMilliseconds, word.DurationMs + word.PauseAfterMs);
                }

                remainingIndex--;
            }
        }

        return MinimumReaderLoopDelayMilliseconds;
    }

    private int GetNextPlaybackCardIndex() =>
        _cards.Count == 0
            ? 0
            : (_activeReaderCardIndex + 1) % _cards.Count;
}
