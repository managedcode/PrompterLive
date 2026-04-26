using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Storage;

namespace PrompterOne.Shared.Pages;

public partial class LearnPage
{
    private const string LoopToggleButtonBaseCssClass = "rsvp-btn rsvp-btn-sm rsvp-loop-toggle";
    private const string LoopToggleButtonActiveCssClass = LoopToggleButtonBaseCssClass + " is-active";

    private string LoopToggleButtonCssClass => _isLoopEnabled
        ? LoopToggleButtonActiveCssClass
        : LoopToggleButtonBaseCssClass;

    private string LoopToggleButtonTitle => _isLoopEnabled
        ? Text(UiTextKey.TooltipLoopPlaybackOn)
        : Text(UiTextKey.TooltipLoopPlaybackOff);

    private bool IsOnFinalTimelineEntry() => _timeline.Count > 0 && _currentIndex >= _timeline.Count - 1;

    private Task PersistCurrentLearnSettingsAsync() =>
        PersistLearnSettingsAsync(settings => settings with
        {
            AutoPlay = _isPlaying,
            ContextWords = _contextWordCount,
            LoopPlayback = _isLoopEnabled,
            WordsPerMinute = _speed
        });

    private async Task ToggleLoopPlaybackAsync()
    {
        _isLoopEnabled = !_isLoopEnabled;
        await PersistCurrentLearnSettingsAsync();
        RestartPlaybackLoopIfActive();
    }

    private async Task ToggleRsvpPlaybackAsync()
    {
        if (!_isPlaying && !_isLoopEnabled && IsOnFinalTimelineEntry())
        {
            return;
        }

        _isPlaying = !_isPlaying;
        await PersistCurrentLearnSettingsAsync();

        if (_isPlaying)
        {
            RestartPlaybackLoop();
            return;
        }

        StopPlaybackLoop();
    }

    private async Task ChangeRsvpSpeedAsync(int delta)
    {
        _speed = Math.Clamp(_speed + delta, RsvpMinSpeed, RsvpMaxSpeed);
        PlaybackEngine.WordsPerMinute = _speed;
        await PersistLearnSettingsAsync(settings => settings with
        {
            HasCustomizedWordsPerMinute = true,
            WordsPerMinute = _speed
        });

        UpdateDisplayedState();
        UpdateShellState();
        await InvokeAsync(StateHasChanged);
        await AwaitPendingFocusLayoutSyncAsync();
        RestartPlaybackLoopIfActive();
    }

    private async Task StepRsvpWordAsync(int delta)
    {
        await StepRsvpToIndexAsync(_currentIndex + delta);
    }

    private async Task StepRsvpToIndexAsync(int index)
    {
        if (_timeline.Count == 0)
        {
            return;
        }

        _currentIndex = ResolveNavigationIndex(index);
        UpdateDisplayedState();
        RestartPlaybackLoopIfActive();
        await InvokeAsync(StateHasChanged);
        await AwaitPendingFocusLayoutSyncAsync();
    }

    private int ResolveCurrentPhraseStartIndex()
    {
        if (_timeline.Count == 0)
        {
            return 0;
        }

        var currentEntry = _timeline[Math.Clamp(_currentIndex, 0, _timeline.Count - 1)];
        return currentEntry.SentenceStartIndex;
    }

    private int ResolveNextPhraseIndex()
    {
        if (_timeline.Count == 0)
        {
            return 0;
        }

        var currentEntry = _timeline[Math.Clamp(_currentIndex, 0, _timeline.Count - 1)];
        return currentEntry.SentenceEndIndex + 1;
    }

    private int ResolvePreviousPhraseIndex()
    {
        if (_timeline.Count == 0)
        {
            return 0;
        }

        var currentEntry = _timeline[Math.Clamp(_currentIndex, 0, _timeline.Count - 1)];
        if (_currentIndex > currentEntry.SentenceStartIndex)
        {
            return currentEntry.SentenceStartIndex;
        }

        var previousIndex = currentEntry.SentenceStartIndex - 1;
        if (previousIndex < 0)
        {
            return _isLoopEnabled
                ? _timeline[^1].SentenceStartIndex
                : 0;
        }

        return _timeline[previousIndex].SentenceStartIndex;
    }

    private void RestartPlaybackLoopIfActive()
    {
        if (_isPlaying)
        {
            RestartPlaybackLoop();
        }
    }

    private void RestartPlaybackLoop()
    {
        StopPlaybackLoop();
        if (!_isPlaying || _timeline.Count == 0)
        {
            return;
        }

        _playbackCts = new CancellationTokenSource();
        _ = RunPlaybackLoopAsync(_playbackCts.Token);
    }

    private void StopPlaybackLoop()
    {
        if (_playbackCts is null)
        {
            return;
        }

        _playbackCts.Cancel();
        _playbackCts.Dispose();
        _playbackCts = null;
    }

    private async Task RunPlaybackLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _timeline.Count > 0)
            {
                var currentEntry = _timeline[_currentIndex];
                var delayMilliseconds = GetTimelineEntryDelayMilliseconds(currentEntry);

                await Task.Delay(delayMilliseconds, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var shouldContinue = true;
                await InvokeAsync(async () => shouldContinue = await AdvancePlaybackFrameAsync());
                if (!shouldContinue)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task<bool> AdvancePlaybackFrameAsync()
    {
        if (!_isLoopEnabled && IsOnFinalTimelineEntry())
        {
            _isPlaying = false;
            UpdateDisplayedState();
            await PersistCurrentLearnSettingsAsync();
            StopPlaybackLoop();
            StateHasChanged();
            return false;
        }

        _currentIndex = ResolveNavigationIndex(_currentIndex + 1);
        UpdateDisplayedState();
        StateHasChanged();
        return true;
    }

    private async Task PersistLearnSettingsAsync(Func<LearnSettings, LearnSettings> update)
    {
        var currentSettings = SessionService.State.LearnSettings;
        var nextSettings = update(currentSettings);
        await SessionService.UpdateLearnSettingsAsync(nextSettings);
        await UserSettingsStore.SaveAsync(BrowserAppSettingsKeys.LearnSettings, nextSettings);
    }

    private int ResolveNavigationIndex(int index)
    {
        if (_timeline.Count == 0)
        {
            return 0;
        }

        return _isLoopEnabled
            ? WrapTimelineIndex(index)
            : Math.Clamp(index, 0, _timeline.Count - 1);
    }

    private int WrapTimelineIndex(int index)
    {
        if (_timeline.Count == 0)
        {
            return 0;
        }

        var wrappedIndex = index % _timeline.Count;
        return wrappedIndex < 0
            ? wrappedIndex + _timeline.Count
            : wrappedIndex;
    }
}
