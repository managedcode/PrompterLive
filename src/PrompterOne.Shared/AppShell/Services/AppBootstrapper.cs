using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PrompterOne.Core.Abstractions;
using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Storage;

namespace PrompterOne.Shared.Services;

public sealed class AppBootstrapper(
    IScriptSessionService sessionService,
    IScriptRepository scriptRepository,
    ILibraryFolderRepository libraryFolderRepository,
    IMediaSceneService mediaSceneService,
    IUserSettingsStore settingsStore,
    ILogger<AppBootstrapper>? logger = null)
{
    private readonly IScriptSessionService _sessionService = sessionService;
    private readonly IScriptRepository _scriptRepository = scriptRepository;
    private readonly ILibraryFolderRepository _libraryFolderRepository = libraryFolderRepository;
    private readonly IMediaSceneService _mediaSceneService = mediaSceneService;
    private readonly IUserSettingsStore _settingsStore = settingsStore;
    private readonly ILogger<AppBootstrapper> _logger = logger ?? NullLogger<AppBootstrapper>.Instance;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _initialized;

    public async Task EnsureReadyAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            _logger.LogDebug("App bootstrap requested after initialization; skipping.");
            return;
        }

        await _gate.WaitAsync(cancellationToken);

        try
        {
            if (_initialized)
            {
                _logger.LogDebug("App bootstrap completed while waiting on the gate; skipping.");
                return;
            }

            _logger.LogInformation("Initializing PrompterOne browser state.");
            await _libraryFolderRepository.InitializeAsync(RuntimeLibrarySeedCatalog.CreateFolders(), cancellationToken);
            await _scriptRepository.InitializeAsync(RuntimeLibrarySeedCatalog.CreateDocuments(), cancellationToken);
            await _sessionService.InitializeAsync(cancellationToken);

            var readerSettings = await _settingsStore.LoadAsync<ReaderSettings>(BrowserAppSettingsKeys.ReaderSettings, cancellationToken);
            if (readerSettings is not null)
            {
                _logger.LogInformation("Restoring reader settings from browser storage.");
                await _sessionService.UpdateReaderSettingsAsync(readerSettings);
            }

            var learnSettings = await _settingsStore.LoadAsync<LearnSettings>(BrowserAppSettingsKeys.LearnSettings, cancellationToken);
            if (learnSettings is not null)
            {
                var normalizedLearnSettings = NormalizeLearnSettings(learnSettings);
                if (normalizedLearnSettings != learnSettings)
                {
                    _logger.LogInformation("Normalizing legacy learn settings from browser storage.");
                    await _settingsStore.SaveAsync(BrowserAppSettingsKeys.LearnSettings, normalizedLearnSettings, cancellationToken);
                }

                _logger.LogInformation("Restoring learn settings from browser storage.");
                await _sessionService.UpdateLearnSettingsAsync(normalizedLearnSettings);
            }

            var mediaScene = await _settingsStore.LoadAsync<MediaSceneState>(BrowserAppSettingsKeys.SceneSettings, cancellationToken);
            if (mediaScene is not null)
            {
                var (normalizedMediaScene, mediaSceneChanged) = NormalizeMediaScene(mediaScene);
                if (mediaSceneChanged)
                {
                    _logger.LogInformation("Normalizing media scene labels from browser storage.");
                    await _settingsStore.SaveAsync(BrowserAppSettingsKeys.SceneSettings, normalizedMediaScene, cancellationToken);
                }

                _logger.LogInformation("Restoring media scene from browser storage.");
                _mediaSceneService.ApplyState(normalizedMediaScene);
            }

            _initialized = true;
            _logger.LogInformation("PrompterOne bootstrap completed.");
        }
        finally
        {
            _gate.Release();
        }
    }

    private static LearnSettings NormalizeLearnSettings(LearnSettings settings)
    {
        var normalizedWordsPerMinute = settings.HasCustomizedWordsPerMinute
            ? NormalizeLearnWordsPerMinute(settings.WordsPerMinute, migrateLegacyDefault: false)
            : NormalizeLearnWordsPerMinute(settings.WordsPerMinute, migrateLegacyDefault: true);

        return settings with { WordsPerMinute = normalizedWordsPerMinute };
    }

    private static int NormalizeLearnWordsPerMinute(int wordsPerMinute, bool migrateLegacyDefault)
    {
        if (wordsPerMinute <= 0)
        {
            return LearnSettingsDefaults.WordsPerMinute;
        }

        if (migrateLegacyDefault && wordsPerMinute == LearnSettingsDefaults.LegacyWordsPerMinute)
        {
            return LearnSettingsDefaults.WordsPerMinute;
        }

        return wordsPerMinute;
    }

    private static (MediaSceneState State, bool Changed) NormalizeMediaScene(MediaSceneState state)
    {
        var changed = false;
        var normalizedCameras = state.Cameras
            .Select(camera =>
            {
                var normalizedLabel = MediaDeviceLabelSanitizer.Sanitize(camera.Label);
                if (string.Equals(normalizedLabel, camera.Label, StringComparison.Ordinal))
                {
                    return camera;
                }

                changed = true;
                return camera with { Label = normalizedLabel };
            })
            .ToList();

        var normalizedPrimaryMicrophoneLabel = NormalizeOptionalLabel(state.PrimaryMicrophoneLabel, ref changed);
        var normalizedAudioInputs = state.AudioBus.Inputs
            .Select(input =>
            {
                var normalizedLabel = MediaDeviceLabelSanitizer.Sanitize(input.Label);
                if (string.Equals(normalizedLabel, input.Label, StringComparison.Ordinal))
                {
                    return input;
                }

                changed = true;
                return input with { Label = normalizedLabel };
            })
            .ToList();

        if (!changed)
        {
            return (state, false);
        }

        return (
            state with
            {
                Cameras = normalizedCameras,
                PrimaryMicrophoneLabel = normalizedPrimaryMicrophoneLabel,
                AudioBus = state.AudioBus with { Inputs = normalizedAudioInputs }
            },
            true);
    }

    private static string? NormalizeOptionalLabel(string? value, ref bool changed)
    {
        if (value is null)
        {
            return null;
        }

        var normalized = MediaDeviceLabelSanitizer.Sanitize(value);
        if (!string.Equals(normalized, value, StringComparison.Ordinal))
        {
            changed = true;
        }

        return normalized;
    }
}
