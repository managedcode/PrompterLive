using System.Globalization;
using PrompterOne.Core.Models.Documents;
using PrompterOne.Shared.Components.Editor;
using PrompterOne.Shared.Services.Editor;

namespace PrompterOne.Shared.Pages;

public partial class EditorPage
{
    private const string LocalRevisionTimestampFormat = "yyyy-MM-dd HH:mm:ss";
    private const int LocalRevisionDiffPreviewLineLimit = 8;

    private async Task LoadEditorFileWorkflowAsync(CancellationToken cancellationToken = default)
    {
        _fileStorageSettings = await BrowserFileStorageStore.LoadSettingsAsync(cancellationToken);
        await LoadLocalHistoryAsync(SessionService.State.ScriptId, cancellationToken);
    }

    private async Task LoadLocalHistoryAsync(string? scriptId, CancellationToken cancellationToken = default)
    {
        var revisions = await EditorLocalRevisionStore.LoadAsync(scriptId, cancellationToken);
        ApplyLocalHistory(revisions);
    }

    private async Task CaptureLocalRevisionAsync(
        StoredScriptDocument savedDocument,
        CancellationToken cancellationToken)
    {
        _lastLocalSaveAt = savedDocument.UpdatedAt;
        if (!_fileStorageSettings.FileBackupCopiesEnabled)
        {
            return;
        }

        var revisions = await EditorLocalRevisionStore.RecordAsync(
            new EditorLocalRevisionDraft(
                savedDocument.Id,
                savedDocument.Title,
                savedDocument.DocumentName,
                savedDocument.Text,
                savedDocument.UpdatedAt),
            cancellationToken);

        ApplyLocalHistory(revisions);
    }

    private async Task OnLocalHistoryRestoreRequestedAsync(string revisionId)
    {
        var revision = await EditorLocalRevisionStore.GetAsync(SessionService.State.ScriptId, revisionId);
        if (revision is null)
        {
            return;
        }

        _selection = EditorSelectionViewModel.Empty;
        _ = TryImportFrontMatterFromSource(revision.PersistedText, out var bodyText);
        _history.TryRecord(bodyText, _selection.Range);
        await PersistDraftAsync(bodyText, revision.DocumentName);
        await LoadLocalHistoryAsync(SessionService.State.ScriptId);
    }

    private void ApplyLocalHistory(IReadOnlyList<EditorLocalRevisionRecord> revisions)
    {
        _localHistory = revisions
            .Select(CreateLocalRevisionViewModel)
            .ToList();

        _lastLocalSaveAt = revisions.Count > 0
            ? revisions[0].SavedAt
            : null;
    }

    private EditorLocalRevisionViewModel CreateLocalRevisionViewModel(EditorLocalRevisionRecord revision)
    {
        var diff = BuildLocalRevisionDiff(_sourceText, ReadRevisionBody(revision.PersistedText));
        return new(
            revision.Id,
            revision.SavedAt.LocalDateTime.ToString(LocalRevisionTimestampFormat, CultureInfo.InvariantCulture),
            revision.Title,
            revision.DocumentName,
            diff.AddedCount,
            diff.RemovedCount,
            diff.Lines);
    }

    private string ReadRevisionBody(string persistedText) =>
        _frontMatterService.Parse(persistedText).Body;

    private static LocalRevisionDiff BuildLocalRevisionDiff(string currentText, string revisionText)
    {
        var currentLines = SplitDiffLines(currentText);
        var revisionLines = SplitDiffLines(revisionText);
        var table = BuildLongestCommonSubsequenceTable(currentLines, revisionLines);
        var lines = new List<EditorLocalRevisionDiffLineViewModel>();
        var addedCount = 0;
        var removedCount = 0;
        var currentIndex = 0;
        var revisionIndex = 0;

        while (currentIndex < currentLines.Length && revisionIndex < revisionLines.Length)
        {
            if (string.Equals(currentLines[currentIndex], revisionLines[revisionIndex], StringComparison.Ordinal))
            {
                currentIndex++;
                revisionIndex++;
                continue;
            }

            if (table[currentIndex + 1, revisionIndex] >= table[currentIndex, revisionIndex + 1])
            {
                removedCount++;
                AddDiffPreviewLine(lines, new("removed", "-", currentLines[currentIndex]));
                currentIndex++;
            }
            else
            {
                addedCount++;
                AddDiffPreviewLine(lines, new("added", "+", revisionLines[revisionIndex]));
                revisionIndex++;
            }
        }

        while (currentIndex < currentLines.Length)
        {
            removedCount++;
            AddDiffPreviewLine(lines, new("removed", "-", currentLines[currentIndex]));
            currentIndex++;
        }

        while (revisionIndex < revisionLines.Length)
        {
            addedCount++;
            AddDiffPreviewLine(lines, new("added", "+", revisionLines[revisionIndex]));
            revisionIndex++;
        }

        return new(addedCount, removedCount, lines);
    }

    private static void AddDiffPreviewLine(
        ICollection<EditorLocalRevisionDiffLineViewModel> lines,
        EditorLocalRevisionDiffLineViewModel line)
    {
        if (lines.Count < LocalRevisionDiffPreviewLineLimit)
        {
            lines.Add(line);
        }
    }

    private static string[] SplitDiffLines(string text) =>
        (text ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');

    private static int[,] BuildLongestCommonSubsequenceTable(string[] currentLines, string[] revisionLines)
    {
        var table = new int[currentLines.Length + 1, revisionLines.Length + 1];
        for (var currentIndex = currentLines.Length - 1; currentIndex >= 0; currentIndex--)
        {
            for (var revisionIndex = revisionLines.Length - 1; revisionIndex >= 0; revisionIndex--)
            {
                table[currentIndex, revisionIndex] = string.Equals(currentLines[currentIndex], revisionLines[revisionIndex], StringComparison.Ordinal)
                    ? table[currentIndex + 1, revisionIndex + 1] + 1
                    : Math.Max(table[currentIndex + 1, revisionIndex], table[currentIndex, revisionIndex + 1]);
            }
        }

        return table;
    }

    private sealed record LocalRevisionDiff(
        int AddedCount,
        int RemovedCount,
        IReadOnlyList<EditorLocalRevisionDiffLineViewModel> Lines);
}
