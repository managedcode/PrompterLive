using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Localization;

namespace PrompterOne.Shared.Pages;

public partial class TeleprompterPage
{
    private const string AttachReaderCameraOperation = "Teleprompter camera attach";
    private const int MinimumReaderLoopDelayMilliseconds = 120;
    // Keep the C# wait budget locked to `.rd-card` transition timing in
    // `10-reading-states.css` so card normalization never lags behind the
    // actual visual handoff.
    private const int ReaderCardTransitionMilliseconds = 700;

    private Task DecreaseReaderPlaybackSpeedAsync() => ChangeReaderPlaybackSpeedAsync(-ReaderPlaybackSpeedStepWpm);

    private Task IncreaseReaderPlaybackSpeedAsync() => ChangeReaderPlaybackSpeedAsync(ReaderPlaybackSpeedStepWpm);

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

    private async Task ChangeReaderPlaybackSpeedAsync(int delta)
    {
        await SetReaderPlaybackSpeedAsync(_readerPlaybackSpeedWpm + delta);
    }

    private async Task SetReaderPlaybackSpeedAsync(int speedWpm)
    {
        var nextSpeedWpm = Math.Clamp(
            speedWpm,
            ReaderMinimumPlaybackSpeedWpm,
            ReaderMaximumPlaybackSpeedWpm);
        if (nextSpeedWpm == _readerPlaybackSpeedWpm)
        {
            return;
        }

        _readerPlaybackSpeedWpm = nextSpeedWpm;
        await PersistCurrentReaderLayoutAsync();

        if (_isReaderPlaying)
        {
            RestartReaderPlaybackLoop(GetCurrentWordDelayMilliseconds());
        }
    }

    private async Task HandleReaderFontSizeInputAsync(ChangeEventArgs args)
    {
        var nextFontSize = ParseReaderControlValue(
            args.Value,
            ReaderMinFontSize,
            ReaderMaxFontSize,
            _readerFontSize);
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
        _readerTextWidthPercent = ParseReaderControlValue(
            args.Value,
            ReaderMinTextWidthPercent,
            ReaderMaxTextWidthPercent,
            _readerTextWidthPercent);
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
            //  If we stopped mid-pause-beat, advance past the pause on
            //  resume instead of waiting another full word duration and
            //  replaying the preceding word. A minimal loop tick kicks
            //  the AdvanceReaderPlaybackAsync code into the "clear pause,
            //  activate next word" branch.
            var resumeDelayMs = _activeReaderPauseChunkIndex is not null
                ? MinimumReaderLoopDelayMilliseconds
                : GetCurrentWordDelayMilliseconds();
            RestartReaderPlaybackLoop(resumeDelayMs);
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

            SetReaderPlaybackState(true);
            await ActivateReaderWordAsync(0, alignBeforeActivation: true);
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
                await ActivateReaderWordAsync(_activeReaderWordIndex - 1, alignBeforeActivation: true);
            }
        }
        else if (_activeReaderWordIndex < 0)
        {
            await ActivateReaderWordAsync(0, alignBeforeActivation: true);
        }
        else
        {
            var wordCount = GetCardWordCount(_cards[_activeReaderCardIndex]);
            if (_activeReaderWordIndex < wordCount - 1)
            {
                await ActivateReaderWordAsync(_activeReaderWordIndex + 1, alignBeforeActivation: true);
            }
            else
            {
                //  Manual forward step off the last word always advances
                //  upward — even on wrap-around.
                await AdvanceToCardAsync(
                    GetNextPlaybackCardIndex(),
                    CancellationToken.None,
                    explicitDirection: ReaderCardForwardStep);
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
            await ActivateReaderWordAsync(0, alignBeforeActivation: true);

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
        var attached = await Diagnostics.RunAsync(
            AttachReaderCameraOperation,
            Text(UiTextKey.TeleprompterAttachCameraMessage),
            () => CameraPreviewInterop.AttachCameraAsync(_cameraLayer.ElementId, _cameraLayer.DeviceId));

        if (!attached)
        {
            _isReaderCameraActive = false;
            _cameraLayer = _cameraLayer with { AutoStart = false };
        }
    }

    private Task DetachReaderCameraAsync() =>
        CameraPreviewInterop.DetachCameraAsync(_cameraLayer.ElementId);

    private void RestartReaderPlaybackLoop(int initialDelayMilliseconds)
    {
        StopReaderPlaybackLoop(keepPlaybackState: true);
        _readerPlaybackCts = new CancellationTokenSource();
        SetReaderPlaybackState(true);
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
            //  Pause-beat state is intentionally NOT cleared here — the
            //  resume path in ToggleReaderPlaybackAsync reads it so a user
            //  who stops mid-pause and hits play again skips past the
            //  already-played pause rather than re-playing the preceding
            //  word for its full duration (was a real bug found in a
            //  playback trace review).
            SetReaderPlaybackState(false);
            _ = ClearKineticEnvelopesAsync();
        }
    }

    private async Task ClearKineticEnvelopesAsync()
    {
        try
        {
            await KineticInterop.ClearAsync();
        }
        catch (JSException) { }
        catch (InvalidOperationException) { }
        catch (TaskCanceledException) { }
    }

    private void SetReaderPlaybackState(bool isPlaying)
    {
        _isReaderPlaying = isPlaying;
        Shell.SetTeleprompterPlaybackActive(isPlaying);
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

        //  Pause beat: the word we just finished is the last word of its
        //  group and is followed by a pause chunk. The pause takes over as
        //  the active beat (its own "word") so the operator sees the pill
        //  or dots light up while the previous word dims to read-state.
        if (_activeReaderPauseChunkIndex is null &&
            _activeReaderWordIndex >= 0 &&
            FindTrailingPauseChunk(_activeReaderCardIndex, _activeReaderWordIndex) is { } pauseBeat)
        {
            _activeReaderPauseChunkIndex = pauseBeat.ChunkIndex;
            await ClearKineticEnvelopesAsync();
            UpdateReaderDisplayState(requestAlignment: false);
            await InvokeAsync(StateHasChanged);
            await SlideFocusLensToPauseAsync(pauseBeat.ChunkIndex);
            return Math.Max(MinimumReaderLoopDelayMilliseconds, BuildScaledReaderDelayMilliseconds(pauseBeat.DurationMs));
        }

        //  Pause beat finished — clear the marker so the next word can take
        //  the active slot cleanly.
        _activeReaderPauseChunkIndex = null;

        if (_activeReaderWordIndex < cardWordCount - 1)
        {
            await ActivateReaderWordAsync(_activeReaderWordIndex + 1, alignBeforeActivation: true);
            return GetCurrentWordDelayMilliseconds();
        }

        if (!_isReaderAutoLoopEnabled && _activeReaderCardIndex == _cards.Count - 1)
        {
            StopReaderPlaybackLoop();
            UpdateReaderDisplayState(requestAlignment: false);
            await InvokeAsync(StateHasChanged);
            return MinimumReaderLoopDelayMilliseconds;
        }

        //  Playback loop always advances upward — even when wrapping
        //  from the last card back to index 0. Without an explicit
        //  direction, the wrap would compare 0 < lastIndex and fire
        //  the backward-descent animation during normal forward play.
        await AdvanceToCardAsync(GetNextPlaybackCardIndex(), cancellationToken, explicitDirection: ReaderCardForwardStep);
        return GetCurrentWordDelayMilliseconds();
    }

    //  Locate a trailing pause that should play as its own beat after the
    //  given word ordinal. Returns the pause's chunk index and duration if
    //  the word is the last child of its group AND the next chunk is a
    //  pause with a real duration. Breath markers (duration 0) are treated
    //  as visual-only and do not stop playback.
    private (int ChunkIndex, int DurationMs)? FindTrailingPauseChunk(int cardIndex, int wordOrdinal)
    {
        if (cardIndex < 0 || cardIndex >= _cards.Count || wordOrdinal < 0)
        {
            return null;
        }

        var card = _cards[cardIndex];
        var remaining = wordOrdinal;
        for (var chunkIndex = 0; chunkIndex < card.Chunks.Count; chunkIndex++)
        {
            if (card.Chunks[chunkIndex] is not ReaderGroupViewModel group)
            {
                continue;
            }

            if (remaining >= group.Words.Count)
            {
                remaining -= group.Words.Count;
                continue;
            }

            var isLastWordOfGroup = remaining == group.Words.Count - 1;
            if (!isLastWordOfGroup)
            {
                return null;
            }

            var nextChunkIndex = chunkIndex + 1;
            if (nextChunkIndex < card.Chunks.Count &&
                card.Chunks[nextChunkIndex] is ReaderPauseViewModel pause &&
                pause.DurationMs > 0)
            {
                return (nextChunkIndex, pause.DurationMs);
            }

            return null;
        }

        return null;
    }

    private Task AdvanceToCardAsync(int nextCardIndex, CancellationToken cancellationToken) =>
        AdvanceToCardAsync(nextCardIndex, cancellationToken, explicitDirection: null);

    private async Task AdvanceToCardAsync(
        int nextCardIndex,
        CancellationToken cancellationToken,
        int? explicitDirection)
    {
        await CancelPendingReaderCardTransitionAsync();
        var previousCardIndex = _activeReaderCardIndex;
        //  Hide the OLD card's focus lens explicitly — otherwise the
        //  `.rd-focus-lens-active` class stays on it and, if the same
        //  card comes back into view later, the lens would be visible
        //  at a stale position until the first word re-activates.
        await HideFocusLensAsync(previousCardIndex);
        var transition = BeginReaderCardTransitionScope(cancellationToken);
        await PrepareReaderCardTransitionAsync(nextCardIndex, explicitDirection);
        await PrepareReaderCardAlignmentAsync(nextCardIndex, 0);
        _readerTransitionSourceCardIndex = previousCardIndex;
        _activeReaderCardIndex = nextCardIndex;
        _activeReaderWordIndex = -1;
        _activeReaderPauseChunkIndex = null;
        _preparedReaderCardIndex = null;
        UpdateReaderDisplayState(requestAlignment: false);
        await InvokeAsync(StateHasChanged);

        try
        {
            await Task.Delay(ReaderCardTransitionMilliseconds, transition.Token);

            if (transition.Token.IsCancellationRequested)
            {
                return;
            }

            await ActivateReaderWordAsync(0, alignBeforeActivation: false);
        }
        catch (OperationCanceledException) when (transition.Token.IsCancellationRequested)
        {
        }
        finally
        {
            CompleteReaderCardTransitionScope(transition.Source);
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
                    //  PauseAfterMs is NOT added here — trailing pauses run
                    //  as their own beat (see FindTrailingPauseChunk). The
                    //  word's active state owns only its spoken time.
                    return BuildScaledReaderDelayMilliseconds(word.DurationMs);
                }

                remainingIndex--;
            }
        }

        return MinimumReaderLoopDelayMilliseconds;
    }

    private async Task ActivateReaderWordAsync(int wordIndex, bool alignBeforeActivation)
    {
        if (alignBeforeActivation)
        {
            await AlignReaderWordBeforeActivationAsync(_activeReaderCardIndex, wordIndex);
        }

        //  Any pending pause beat ends the moment a word takes the stage.
        _activeReaderPauseChunkIndex = null;
        _activeReaderWordIndex = wordIndex;
        UpdateReaderDisplayState(requestAlignment: false);
        await InvokeAsync(StateHasChanged);
        await FireKineticWordEnvelopeAsync(wordIndex);
        await SlideFocusLensToActiveWordAsync(wordIndex);
    }

    private async Task SlideFocusLensToActiveWordAsync(int wordIndex)
    {
        if (!TryGetAlignmentWordId(_activeReaderCardIndex, wordIndex, out var wordId))
        {
            return;
        }

        //  Word's cue tags drive the lens transition character; its
        //  scaled wall-clock duration sizes the glide so the lens never
        //  over- or under-shoots the word's own time budget.
        var word = ResolveReaderWordAt(_activeReaderCardIndex, wordIndex);
        var cueTags = word is null
            ? Array.Empty<string>()
            : ExtractKineticCueTags(word.CssClass);
        var scaledWordDurationMs = word is null
            ? MinimumReaderLoopDelayMilliseconds
            : BuildScaledReaderDelayMilliseconds(word.DurationMs);

        await InvokeLensPositionAsync(wordId, cueTags, scaledWordDurationMs);
    }

    private Task SlideFocusLensToPauseAsync(int chunkIndex)
    {
        //  Pauses advance the lens as their own "beat". The lens glide
        //  occupies ~0.9× the pause duration (default ratio), so short
        //  pauses get a quick slide and long silence pills get a gentle
        //  settle matching the operator's breathing moment.
        var chunkId = UiDomIds.Teleprompter.CardChunk(_activeReaderCardIndex, chunkIndex);
        var pauseDurationMs = ResolvePauseChunkDurationMs(_activeReaderCardIndex, chunkIndex);
        var scaledPauseDurationMs = BuildScaledReaderDelayMilliseconds(pauseDurationMs);
        return InvokeLensPositionAsync(chunkId, Array.Empty<string>(), scaledPauseDurationMs);
    }

    private int ResolvePauseChunkDurationMs(int cardIndex, int chunkIndex)
    {
        if (cardIndex < 0 || cardIndex >= _cards.Count)
        {
            return MinimumReaderLoopDelayMilliseconds;
        }

        var chunks = _cards[cardIndex].Chunks;
        if (chunkIndex < 0 || chunkIndex >= chunks.Count)
        {
            return MinimumReaderLoopDelayMilliseconds;
        }

        return chunks[chunkIndex] is ReaderPauseViewModel pause
            ? Math.Max(MinimumReaderLoopDelayMilliseconds, pause.DurationMs)
            : MinimumReaderLoopDelayMilliseconds;
    }

    private async Task InvokeLensPositionAsync(
        string targetId,
        IReadOnlyList<string> cueTags,
        int targetDurationMs)
    {
        var lensId = UiDomIds.Teleprompter.FocusLens(_activeReaderCardIndex);
        try
        {
            await KineticInterop.PositionLensAsync(lensId, targetId, cueTags, targetDurationMs);
        }
        catch (JSException) { }
        catch (InvalidOperationException) { }
        catch (TaskCanceledException) { }
    }

    private async Task HideFocusLensAsync(int cardIndex)
    {
        if (cardIndex < 0 || cardIndex >= _cards.Count)
        {
            return;
        }

        var lensId = UiDomIds.Teleprompter.FocusLens(cardIndex);
        try
        {
            await KineticInterop.HideLensAsync(lensId);
        }
        catch (JSException) { }
        catch (InvalidOperationException) { }
        catch (TaskCanceledException) { }
    }

    //  Dispatch the kinetic envelope for the freshly-activated word. The
    //  duration is the same scaled wall-clock budget the reader loop will
    //  wait before advancing, so WAAPI finishes exactly on the handoff.
    //  Cue tags come from the word's composed CSS class so there is one
    //  authoritative source for cue metadata and no stringly-typed map.
    private async Task FireKineticWordEnvelopeAsync(int wordIndex)
    {
        var word = ResolveReaderWordAt(_activeReaderCardIndex, wordIndex);
        if (word is null)
        {
            return;
        }

        var wordDuration = word.DurationMs > 0 ? word.DurationMs : MinimumReaderLoopDelayMilliseconds;
        var scaledDurationMs = BuildScaledReaderDelayMilliseconds(wordDuration);
        var cueTags = ExtractKineticCueTags(word.CssClass);

        try
        {
            await KineticInterop.ActivateWordAsync(scaledDurationMs, cueTags, 1d);
        }
        catch (JSException)
        {
            //  JS module not loaded yet (prerender or before index.html
            //  script tag settles). Safe to swallow — next word will
            //  pick up once the bridge is ready.
        }
        catch (InvalidOperationException)
        {
            //  JSRuntime is unavailable during Blazor prerender.
        }
        catch (TaskCanceledException)
        {
        }
    }

    private ReaderWordViewModel? ResolveReaderWordAt(int cardIndex, int wordIndex)
    {
        if (cardIndex < 0 || cardIndex >= _cards.Count || wordIndex < 0)
        {
            return null;
        }

        var remaining = wordIndex;
        foreach (var chunk in _cards[cardIndex].Chunks)
        {
            if (chunk is not ReaderGroupViewModel group)
            {
                continue;
            }

            foreach (var word in group.Words)
            {
                if (remaining == 0)
                {
                    return word;
                }
                remaining--;
            }
        }

        return null;
    }

    private static IReadOnlyList<string> ExtractKineticCueTags(string cssClass)
    {
        if (string.IsNullOrWhiteSpace(cssClass))
        {
            return Array.Empty<string>();
        }

        var tokens = cssClass.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var tags = new List<string>(tokens.Length);
        foreach (var token in tokens)
        {
            if (token.StartsWith(KineticCueClassPrefix, StringComparison.Ordinal))
            {
                var tag = token[KineticCueClassPrefix.Length..];
                if (tag.Length > 0)
                {
                    tags.Add(tag);
                }
            }
        }
        return tags;
    }

    private const string KineticCueClassPrefix = "tps-";

    private int GetNextPlaybackCardIndex() =>
        _cards.Count == 0
            ? 0
            : (_activeReaderCardIndex + 1) % _cards.Count;

    private int BuildScaledReaderDelayMilliseconds(int rawDelayMilliseconds)
    {
        var safeDelay = Math.Max(MinimumReaderLoopDelayMilliseconds, rawDelayMilliseconds);
        var speedRatio = _readerBaseTpsWpm / (double)_readerPlaybackSpeedWpm;
        return Math.Max(
            MinimumReaderLoopDelayMilliseconds,
            (int)Math.Round(safeDelay * speedRatio, MidpointRounding.AwayFromZero));
    }
}
