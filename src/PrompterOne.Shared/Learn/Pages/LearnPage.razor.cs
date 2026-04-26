using Microsoft.AspNetCore.Components;
using PrompterOne.Core.Abstractions;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Core.Services.Rsvp;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Learn.Services;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Services.Diagnostics;

namespace PrompterOne.Shared.Pages;

public partial class LearnPage : IAsyncDisposable
{
    private const int DefaultContextWordCount = LearnSettingsDefaults.ContextWords;
    private const string LoadLearnOperation = "Learn load";
    private const int MinimumLoopDelayMilliseconds = 60;
    private const int MinimumWordDurationMilliseconds = 60;
    private const string NeutralEmotion = "neutral";
    private const int ReadyWordDurationMilliseconds = 240;
    private const int PreviewWordCount = 10;
    private const int RsvpMaxSpeed = 600;
    private const int RsvpMinSpeed = 100;
    private const int RsvpSpeedStep = 10;
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
    [Inject] private LearnPrepNotesStore PrepNotesStore { get; set; } = null!;
    [Inject] private RsvpTextProcessor TextProcessor { get; set; } = null!;

    [Parameter]
    [SupplyParameterFromQuery(Name = AppRoutes.ScriptIdQueryKey)]
    public string? ScriptId { get; set; }

    private CancellationTokenSource? _playbackCts;
    private ElementReference _displayRoot;
    private ElementReference _focusOrp;
    private ElementReference _focusRow;
    private ElementReference _focusWord;
    private ElementReference _screenRoot;
    private string _nextPhrase = string.Empty;
    private double _progressPercent;
    private string _progressLabel = string.Empty;
    private string _screenSubtitle = string.Empty;
    private string _screenTitle = string.Empty;
    private int _contextWordCount = DefaultContextWordCount;
    private int _currentIndex;
    private int _speed = LearnSettingsDefaults.WordsPerMinute;
    private bool _isPlaying;
    private bool _isLoopEnabled;
    private bool _loadState = true;
    private bool _arePrepNotesExpanded = true;
    private bool _focusScreenAfterRender = true;
    private bool _syncFocusLayoutAfterRender;
    private bool _startPlaybackAfterLayoutSync;
    private string _currentWordLeading = string.Empty;
    private string _currentWordOrp = string.Empty;
    private string _currentWordTrailing = string.Empty;
    private string _prepNoteDraft = string.Empty;
    private IReadOnlyList<string> _leftContextWords = [];
    private IReadOnlyList<LearnPrepNote> _prepNotes = [];
    private IReadOnlyList<string> _rightContextWords = [];
    private IReadOnlyList<RsvpTimelineEntry> _timeline = [];

    protected override Task OnParametersSetAsync()
    {
        StopPlaybackLoop();
        _loadState = true;
        _focusScreenAfterRender = true;
        MarkFocusLayoutDirty();
        return Task.CompletedTask;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_loadState)
        {
            _loadState = false;
            await Diagnostics.RunAsync(
                LoadLearnOperation,
                Text(UiTextKey.LearnLoadMessage),
                async () =>
                {
                    await Bootstrapper.EnsureReadyAsync();
                    await EnsureSessionLoadedAsync();
                    PopulateLearnState();
                    await LoadPrepNotesAsync();
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
            var didSync = await LearnRsvpLayoutInterop.SyncLayoutAsync(_displayRoot, _focusRow, _focusWord, _focusOrp);
            CompletePendingFocusLayoutSync(didSync);
            await InvokeAsync(StateHasChanged);
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

        _timeline = BuildTimeline(processed);
        _currentIndex = 0;
        _isPlaying = learnSettings.AutoPlay;
        _isLoopEnabled = learnSettings.LoopPlayback;
        UpdateDisplayedState();
        UpdateShellState();
        _startPlaybackAfterLayoutSync = _isPlaying;
    }

    private Task DecreaseRsvpSpeedAsync() => ChangeRsvpSpeedAsync(-RsvpSpeedStep);

    private Task IncreaseRsvpSpeedAsync() => ChangeRsvpSpeedAsync(RsvpSpeedStep);

    private Task StepRsvpBackwardLargeAsync() => StepRsvpToIndexAsync(ResolvePreviousPhraseIndex());

    private Task StepRsvpBackwardAsync() => StepRsvpWordAsync(-RsvpStepSmall);

    private Task StepRsvpForwardAsync() => StepRsvpWordAsync(RsvpStepSmall);

    private Task StepRsvpForwardLargeAsync() => StepRsvpToIndexAsync(ResolveNextPhraseIndex());

    private Task RestartCurrentPhraseAsync() => StepRsvpToIndexAsync(ResolveCurrentPhraseStartIndex());

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
            MarkFocusLayoutDirty();
            _currentWordLeading = string.Empty;
            _currentWordOrp = Text(UiTextKey.LearnReadyWord);
            _currentWordTrailing = string.Empty;
            _leftContextWords = [];
            _rightContextWords = [];
            _nextPhrase = Text(UiTextKey.LearnEndOfScript);
            _progressPercent = 0d;
            _progressLabel = string.Empty;
            _syncFocusLayoutAfterRender = true;
            return;
        }

        _currentIndex = Math.Clamp(_currentIndex, 0, _timeline.Count - 1);
        var entry = _timeline[_currentIndex];
        MarkFocusLayoutDirty();
        var displayWord = NormalizeDisplayWord(entry.Word);
        var focusWord = BuildFocusWord(string.IsNullOrWhiteSpace(displayWord) ? entry.Word : displayWord);
        var sentenceRange = ResolveSentenceRange(_timeline, _currentIndex);
        _currentWordLeading = focusWord.Leading;
        _currentWordOrp = focusWord.Orp;
        _currentWordTrailing = focusWord.Trailing;
        _leftContextWords = BuildDisplayContextWindowWords(
            _timeline,
            sentenceRange.StartIndex,
            _currentIndex,
            _contextWordCount,
            takeTrailingWords: true);
        _rightContextWords = BuildDisplayContextWindowWords(
            _timeline,
            _currentIndex + 1,
            sentenceRange.EndIndex + 1,
            _contextWordCount,
            takeTrailingWords: false);
        var rawPreviewText = string.IsNullOrWhiteSpace(entry.NextPhrase)
            ? ResolveFallbackNextPhrase(_timeline, _currentIndex)
            : entry.NextPhrase;
        _nextPhrase = BuildDisplayPreviewText(rawPreviewText);
        _progressPercent = (_currentIndex + 1) * 100d / _timeline.Count;
        _progressLabel = BuildProgressLabel(_timeline, _currentIndex);
        _syncFocusLayoutAfterRender = true;
    }

    private async Task EnsureSessionLoadedAsync()
    {
        var requestedScriptId = ScriptRouteSessionLoader.ResolveRequestedScriptId(ScriptId, Navigation.Uri);
        if (!string.IsNullOrWhiteSpace(requestedScriptId))
        {
            await ScriptRouteSessionLoader.EnsureRequestedSessionAsync(
                requestedScriptId,
                ScriptRepository,
                SessionService);
            return;
        }
    }

    private void UpdateShellState() =>
        Shell.ShowLearn(_screenTitle, _screenSubtitle, BuildWpmLabel(_speed), SessionService.State.ScriptId);
}
