using Microsoft.AspNetCore.Components;
using PrompterOne.Core.Abstractions;
using PrompterOne.Core.Services.Rsvp;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Services.Diagnostics;

namespace PrompterOne.Shared.Pages;

public partial class LearnPage : IAsyncDisposable
{
    private const int DefaultContextWordCount = 3;
    private const string EndOfScriptPhrase = "End of script.";
    private const string LoadLearnMessage = "Unable to load RSVP rehearsal right now.";
    private const string LoadLearnOperation = "Learn load";
    private const int MinimumLoopDelayMilliseconds = 60;
    private const int MinimumWordDurationMilliseconds = 60;
    private const string NeutralEmotion = "neutral";
    private const string ReadyWord = "Ready";
    private const int PreviewWordCount = 10;
    private const int RsvpMaxSpeed = 600;
    private const int RsvpMinSpeed = 100;
    private const int RsvpSpeedStep = 10;
    private const int RsvpStepLarge = 5;
    private const int RsvpStepSmall = 1;
    private const string WpmSuffix = " WPM";
    [Inject] private AppBootstrapper Bootstrapper { get; set; } = null!;
    [Inject] private IUserSettingsStore UserSettingsStore { get; set; } = null!;
    [Inject] private UiDiagnosticsService Diagnostics { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private RsvpPlaybackEngine PlaybackEngine { get; set; } = null!;
    [Inject] private IScriptRepository ScriptRepository { get; set; } = null!;
    [Inject] private IScriptSessionService SessionService { get; set; } = null!;
    [Inject] private AppShellService Shell { get; set; } = null!;
    [Inject] private LearnRsvpLayoutInterop LearnRsvpLayoutInterop { get; set; } = null!;
    [Inject] private RsvpTextProcessor TextProcessor { get; set; } = null!;

    [SupplyParameterFromQuery(Name = AppRoutes.ScriptIdQueryKey)]
    public string? ScriptId { get; set; }

    private CancellationTokenSource? _playbackCts;
    private ElementReference _focusRow;
    private ElementReference _screenRoot;
    private string _nextPhrase = string.Empty;
    private string _progressFillWidth = "0%";
    private string _progressLabel = string.Empty;
    private string _screenSubtitle = string.Empty;
    private string _screenTitle = string.Empty;
    private int _contextWordCount = DefaultContextWordCount;
    private int _currentIndex;
    private int _speed = 300;
    private bool _isPlaying;
    private bool _isLoopEnabled;
    private bool _loadState = true;
    private bool _focusScreenAfterRender = true;
    private bool _syncFocusLayoutAfterRender;
    private bool _startPlaybackAfterLayoutSync;
    private string _currentWordLeading = string.Empty;
    private string _currentWordOrp = string.Empty;
    private string _currentWordTrailing = string.Empty;
    private IReadOnlyList<string> _leftContextWords = [];
    private IReadOnlyList<string> _rightContextWords = [];
    private IReadOnlyList<RsvpTimelineEntry> _timeline = [];

    protected override Task OnParametersSetAsync()
    {
        StopPlaybackLoop();
        _loadState = true;
        _focusScreenAfterRender = true;
        return Task.CompletedTask;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_loadState)
        {
            _loadState = false;
            await Diagnostics.RunAsync(
                LoadLearnOperation,
                LoadLearnMessage,
                async () =>
                {
                    await Bootstrapper.EnsureReadyAsync();
                    await EnsureSessionLoadedAsync();
                    PopulateLearnState();
                    StateHasChanged();
                });
            return;
        }

        if (_focusScreenAfterRender)
        {
            _focusScreenAfterRender = false;
            await _screenRoot.FocusAsync();
        }

        if (_syncFocusLayoutAfterRender)
        {
            _syncFocusLayoutAfterRender = false;
            await LearnRsvpLayoutInterop.SyncLayoutAsync(_focusRow);
        }

        if (_startPlaybackAfterLayoutSync)
        {
            _startPlaybackAfterLayoutSync = false;
            RestartPlaybackLoop();
        }
    }

    private void PopulateLearnState()
    {
        var processed = TextProcessor.ParseScript(SessionService.State.Text);
        var segments = processed.Segments;
        var fallbackSpeed = segments.FirstOrDefault()?.Speed ?? _speed;
        var learnSettings = SessionService.State.LearnSettings;

        _screenTitle = SessionService.State.Title;
        _screenSubtitle = segments.FirstOrDefault()?.Title ?? string.Empty;
        _speed = learnSettings.WordsPerMinute > 0
            ? learnSettings.WordsPerMinute
            : fallbackSpeed;
        _contextWordCount = learnSettings.ContextWords > 0
            ? learnSettings.ContextWords
            : DefaultContextWordCount;
        PlaybackEngine.WordsPerMinute = _speed;
        PlaybackEngine.LoadTimeline(processed);

        _timeline = BuildTimeline(processed, _speed);
        _currentIndex = 0;
        _isPlaying = learnSettings.AutoPlay;
        _isLoopEnabled = learnSettings.LoopPlayback;
        UpdateDisplayedState();
        UpdateShellState();
        _startPlaybackAfterLayoutSync = _isPlaying;
    }

    private Task DecreaseRsvpSpeedAsync() => ChangeRsvpSpeedAsync(-RsvpSpeedStep);

    private Task IncreaseRsvpSpeedAsync() => ChangeRsvpSpeedAsync(RsvpSpeedStep);

    private Task StepRsvpBackwardLargeAsync() => StepRsvpWordAsync(-RsvpStepLarge);

    private Task StepRsvpBackwardAsync() => StepRsvpWordAsync(-RsvpStepSmall);

    private Task StepRsvpForwardAsync() => StepRsvpWordAsync(RsvpStepSmall);

    private Task StepRsvpForwardLargeAsync() => StepRsvpWordAsync(RsvpStepLarge);

    private async Task NavigateBackToEditorAsync()
    {
        var route = string.IsNullOrWhiteSpace(SessionService.State.ScriptId)
            ? AppRoutes.Editor
            : AppRoutes.EditorWithId(SessionService.State.ScriptId);
        Navigation.NavigateTo(route);
        await Task.CompletedTask;
    }

    private void UpdateDisplayedState()
    {
        if (_timeline.Count == 0)
        {
            _currentWordLeading = string.Empty;
            _currentWordOrp = ReadyWord;
            _currentWordTrailing = string.Empty;
            _leftContextWords = [];
            _rightContextWords = [];
            _nextPhrase = EndOfScriptPhrase;
            _progressFillWidth = "0%";
            _progressLabel = string.Empty;
            _syncFocusLayoutAfterRender = true;
            return;
        }

        _currentIndex = Math.Clamp(_currentIndex, 0, _timeline.Count - 1);
        var entry = _timeline[_currentIndex];
        var displayWord = NormalizeDisplayWord(entry.Word);
        var focusWord = BuildFocusWord(string.IsNullOrWhiteSpace(displayWord) ? entry.Word : displayWord);
        var sentenceRange = ResolveSentenceRange(_timeline, _currentIndex);
        _currentWordLeading = focusWord.Leading;
        _currentWordOrp = focusWord.Orp;
        _currentWordTrailing = focusWord.Trailing;
        _leftContextWords = BuildDisplayContextWords(
            _timeline,
            Math.Max(sentenceRange.StartIndex, _currentIndex - _contextWordCount),
            _currentIndex);
        _rightContextWords = BuildDisplayContextWords(
            _timeline,
            _currentIndex + 1,
            Math.Min(sentenceRange.EndIndex + 1, _currentIndex + 1 + _contextWordCount));
        var rawPreviewText = string.IsNullOrWhiteSpace(entry.NextPhrase)
            ? ResolveFallbackNextPhrase(_timeline, _currentIndex)
            : entry.NextPhrase;
        _nextPhrase = BuildDisplayPreviewText(rawPreviewText);
        _progressFillWidth = $"{((_currentIndex + 1) * 100d / _timeline.Count):0.##}%";
        _progressLabel = BuildProgressLabel(_timeline, _currentIndex, _speed);
        _syncFocusLayoutAfterRender = true;
    }

    private async Task EnsureSessionLoadedAsync()
    {
        if (!string.IsNullOrWhiteSpace(ScriptId))
        {
            var document = await ScriptRepository.GetAsync(ScriptId);
            if (document is not null &&
                !string.Equals(SessionService.State.ScriptId, document.Id, StringComparison.Ordinal))
            {
                await SessionService.OpenAsync(document);
            }

            return;
        }

    }

    private void UpdateShellState() =>
        Shell.ShowLearn(_screenTitle, _screenSubtitle, BuildWpmLabel(_speed), SessionService.State.ScriptId);

    private int GetScaledDuration(int sourceMilliseconds, int baseWpm, bool allowZero = false)
    {
        if (sourceMilliseconds <= 0)
        {
            return allowZero ? 0 : MinimumWordDurationMilliseconds;
        }

        var effectiveBaseWpm = baseWpm > 0 ? baseWpm : _speed;
        var scaledDuration = sourceMilliseconds * (effectiveBaseWpm / (double)Math.Max(_speed, 1));
        var roundedDuration = (int)Math.Round(scaledDuration);
        return allowZero
            ? Math.Max(0, roundedDuration)
            : Math.Max(MinimumWordDurationMilliseconds, roundedDuration);
    }
}
