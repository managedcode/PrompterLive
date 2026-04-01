using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Storage;

namespace PrompterOne.Shared.Pages;

public partial class LearnPage
{
    private const string LoopToggleButtonBaseCssClass = "rsvp-btn rsvp-btn-sm rsvp-loop-toggle";
    private const string LoopToggleButtonActiveCssClass = LoopToggleButtonBaseCssClass + " is-active";
    private const string LoopToggleTitleDisabled = "Loop playback is off";
    private const string LoopToggleTitleEnabled = "Loop playback is on";

    private string LoopToggleButtonCssClass => _isLoopEnabled
        ? LoopToggleButtonActiveCssClass
        : LoopToggleButtonBaseCssClass;

    private string LoopToggleButtonTitle => _isLoopEnabled
        ? LoopToggleTitleEnabled
        : LoopToggleTitleDisabled;

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
        await PersistCurrentLearnSettingsAsync();

        UpdateDisplayedState();
        UpdateShellState();
        RestartPlaybackLoopIfActive();
    }

    private async Task StepRsvpWordAsync(int delta)
    {
        if (_timeline.Count == 0)
        {
            return;
        }

        _currentIndex = ResolveNavigationIndex(_currentIndex + delta);
        UpdateDisplayedState();
        RestartPlaybackLoopIfActive();
        await InvokeAsync(StateHasChanged);
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
                var delayMilliseconds = Math.Max(
                    MinimumLoopDelayMilliseconds,
                    GetScaledDuration(currentEntry.DurationMs, currentEntry.BaseWpm) +
                    GetScaledDuration(currentEntry.PauseAfterMs, currentEntry.BaseWpm, allowZero: true));

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
