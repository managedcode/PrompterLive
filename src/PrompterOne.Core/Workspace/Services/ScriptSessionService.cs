using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PrompterOne.Core.Abstractions;
using PrompterOne.Core.Models.CompiledScript;
using PrompterOne.Core.Models.Documents;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Core.Services.Preview;

namespace PrompterOne.Core.Services.Workspace;

public sealed class ScriptSessionService(
    IScriptRepository repository,
    TpsParser parser,
    ScriptCompiler compiler,
    IScriptPreviewService previewService,
    ILogger<ScriptSessionService>? logger = null) : IScriptSessionService
{
    private readonly IScriptRepository _repository = repository;
    private readonly TpsParser _parser = parser;
    private readonly ScriptCompiler _compiler = compiler;
    private readonly IScriptPreviewService _previewService = previewService;
    private readonly ILogger<ScriptSessionService> _logger = logger ?? NullLogger<ScriptSessionService>.Instance;

    public ScriptWorkspaceState State { get; private set; } = ScriptWorkspaceState.Empty;

    public event EventHandler? StateChanged;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing script session draft state.");
        await NewAsync(cancellationToken);
    }

    public Task NewAsync(CancellationToken cancellationToken = default)
    {
        return UpdateDraftAsync(
            title: ScriptWorkspaceState.UntitledScriptTitle,
            text: string.Empty,
            documentName: ScriptWorkspaceState.UntitledScriptDocumentName,
            scriptId: string.Empty,
            cancellationToken: cancellationToken);
    }

    public Task OpenAsync(StoredScriptDocument document, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Opening script {ScriptId} ({Title}).", document.Id, document.Title);
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
        title = string.IsNullOrWhiteSpace(title) ? ScriptWorkspaceState.UntitledScriptTitle : title.Trim();
        text ??= string.Empty;
        documentName = string.IsNullOrWhiteSpace(documentName) ? Slugify(title) : documentName;
        _logger.LogDebug("Updating draft for {Title}.", title);

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

            _logger.LogDebug("Draft updated successfully for {Title}.", title);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Failed to update draft for {Title}.", title);
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

    public void StageDraftText(
        string title,
        string text,
        string? documentName = null,
        string? scriptId = null,
        string? errorMessage = null)
    {
        title = string.IsNullOrWhiteSpace(title) ? ScriptWorkspaceState.UntitledScriptTitle : title.Trim();
        text ??= string.Empty;
        documentName = string.IsNullOrWhiteSpace(documentName) ? Slugify(title) : documentName;

        State = State with
        {
            ScriptId = scriptId ?? string.Empty,
            Title = title,
            Text = text,
            DocumentName = documentName,
            ErrorMessage = errorMessage
        };

        NotifyChanged();
    }

    public async Task<StoredScriptDocument> SaveAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Saving script {ScriptId} ({Title}).", State.ScriptId, State.Title);
        var document = await _repository.SaveAsync(
            State.Title,
            State.Text,
            State.DocumentName,
            existingId: State.ScriptId,
            cancellationToken: cancellationToken);

        State = State with
        {
            ScriptId = document.Id,
            Title = document.Title,
            Text = document.Text,
            DocumentName = document.DocumentName
        };

        NotifyChanged();
        _logger.LogInformation("Saved script {ScriptId}.", document.Id);
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
