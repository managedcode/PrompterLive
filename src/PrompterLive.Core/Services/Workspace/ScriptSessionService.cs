using PrompterLive.Core.Abstractions;
using PrompterLive.Core.Models.CompiledScript;
using PrompterLive.Core.Models.Documents;
using PrompterLive.Core.Models.Workspace;
using PrompterLive.Core.Services.Preview;
using PrompterLive.Core.Services.Samples;

namespace PrompterLive.Core.Services.Workspace;

public sealed class ScriptSessionService(
    IScriptRepository repository,
    TpsParser parser,
    ScriptCompiler compiler,
    IScriptPreviewService previewService) : IScriptSessionService
{
    private readonly IScriptRepository _repository = repository;
    private readonly TpsParser _parser = parser;
    private readonly ScriptCompiler _compiler = compiler;
    private readonly IScriptPreviewService _previewService = previewService;

    public ScriptWorkspaceState State { get; private set; } = ScriptWorkspaceState.Empty with
    {
        Text = BuildStarterScript()
    };

    public event EventHandler? StateChanged;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _repository.InitializeAsync(SampleScriptCatalog.CreateSeedDocuments(), cancellationToken);
        await NewAsync(cancellationToken);
    }

    public Task NewAsync(CancellationToken cancellationToken = default)
    {
        return UpdateDraftAsync(
            title: "Fresh Take",
            text: BuildStarterScript(),
            documentName: "fresh-take.tps",
            scriptId: string.Empty,
            cancellationToken: cancellationToken);
    }

    public Task LoadSampleAsync(string sampleId, CancellationToken cancellationToken = default)
    {
        var sample = SampleScriptCatalog.GetById(sampleId);
        return OpenAsync(sample, cancellationToken);
    }

    public Task OpenAsync(StoredScriptDocument document, CancellationToken cancellationToken = default)
    {
        return UpdateDraftAsync(
            title: document.Title,
            text: document.Text,
            documentName: document.DocumentName,
            scriptId: document.Id,
            cancellationToken: cancellationToken);
    }

    public async Task UpdateDraftAsync(
        string title,
        string text,
        string? documentName = null,
        string? scriptId = null,
        CancellationToken cancellationToken = default)
    {
        title = string.IsNullOrWhiteSpace(title) ? "Untitled Script" : title.Trim();
        text ??= string.Empty;
        documentName = string.IsNullOrWhiteSpace(documentName) ? Slugify(title) : documentName;

        if (string.IsNullOrWhiteSpace(text))
        {
            State = State with
            {
                ScriptId = scriptId ?? string.Empty,
                Title = title,
                Text = text,
                DocumentName = documentName,
                ScriptData = null,
                CompiledScript = null,
                PreviewSegments = Array.Empty<SegmentPreviewModel>(),
                WordCount = 0,
                EstimatedDuration = TimeSpan.Zero,
                ErrorMessage = null
            };

            NotifyChanged();
            return;
        }

        try
        {
            var scriptData = _parser.ParseTps(text) with
            {
                ScriptId = scriptId,
                Title = title,
                Content = text
            };

            var document = await _parser.ParseAsync(text);
            var compiledScript = await _compiler.CompileAsync(document);
            var previewSegments = await _previewService.BuildPreviewAsync(text, cancellationToken);

            State = State with
            {
                ScriptId = scriptId ?? string.Empty,
                Title = title,
                Text = text,
                DocumentName = documentName,
                ScriptData = scriptData,
                CompiledScript = compiledScript,
                PreviewSegments = previewSegments,
                WordCount = CountWords(compiledScript),
                EstimatedDuration = CalculateDuration(compiledScript),
                ErrorMessage = null
            };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            State = State with
            {
                ScriptId = scriptId ?? string.Empty,
                Title = title,
                Text = text,
                DocumentName = documentName,
                ScriptData = null,
                CompiledScript = null,
                PreviewSegments = Array.Empty<SegmentPreviewModel>(),
                WordCount = 0,
                EstimatedDuration = TimeSpan.Zero,
                ErrorMessage = exception.Message
            };
        }

        NotifyChanged();
    }

    public async Task<StoredScriptDocument> SaveAsync(CancellationToken cancellationToken = default)
    {
        var document = await _repository.SaveAsync(
            State.Title,
            State.Text,
            State.DocumentName,
            existingId: State.ScriptId,
            cancellationToken: cancellationToken);

        await OpenAsync(document, cancellationToken);
        return document;
    }

    public Task UpdateReaderSettingsAsync(ReaderSettings settings)
    {
        State = State with { ReaderSettings = settings };
        NotifyChanged();
        return Task.CompletedTask;
    }

    public Task UpdateLearnSettingsAsync(LearnSettings settings)
    {
        State = State with { LearnSettings = settings };
        NotifyChanged();
        return Task.CompletedTask;
    }

    private void NotifyChanged() => StateChanged?.Invoke(this, EventArgs.Empty);

    private static int CountWords(CompiledScript? compiledScript) =>
        compiledScript?.Segments
            .SelectMany(segment => segment.Words ?? [])
            .Count(word => word.Metadata?.IsPause != true && !string.IsNullOrWhiteSpace(word.CleanText)) ?? 0;

    private static TimeSpan CalculateDuration(CompiledScript? compiledScript)
    {
        var totalMilliseconds = compiledScript?.Segments
            .SelectMany(segment => segment.Words ?? [])
            .Sum(word => word.DisplayDuration.TotalMilliseconds) ?? 0d;

        return TimeSpan.FromMilliseconds(totalMilliseconds);
    }

    private static string BuildStarterScript() =>
        """
        ---
        title: "Fresh Take"
        author: "PrompterLive"
        base_wpm: 150
        ---

        ## [Opening|140WPM|warm]
        ### [Welcome Block|140WPM]
        Welcome to PrompterLive. This shared draft is ready for editing, rehearsal, and live reading.

        ## [Next Steps|150WPM|focused]
        ### [Action Block|150WPM]
        Add your own segments, tune the pace, and shape the camera scene before you go live.
        """;

    private static string Slugify(string title)
    {
        var slug = new string(title
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray());

        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        slug = slug.Trim('-');
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "untitled-script";
        }

        return $"{slug}.tps";
    }
}
