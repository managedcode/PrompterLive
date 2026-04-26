using Microsoft.AspNetCore.Components;
using PrompterOne.Shared.Learn.Services;
using PrompterOne.Shared.Localization;

namespace PrompterOne.Shared.Pages;

public partial class LearnPage
{
    private sealed record LearnPrepNoteContext(
        string ScriptKey,
        string SectionKey,
        string SectionLabel,
        string AnchorWord,
        int WordIndex);

    private IReadOnlyList<LearnPrepNote> CurrentSectionPrepNotes =>
        _prepNotes
            .Where(note => string.Equals(note.SectionKey, CurrentPrepNoteContext.SectionKey, StringComparison.Ordinal))
            .OrderBy(note => note.CreatedAt)
            .ToArray();

    private LearnPrepNoteContext CurrentPrepNoteContext => BuildCurrentPrepNoteContext();

    private string CurrentPrepNotesSectionLabel =>
        Format(UiTextKey.LearnNotesSectionFormat, CurrentPrepNoteContext.SectionLabel);

    private bool CanAddPrepNote => !string.IsNullOrWhiteSpace(_prepNoteDraft);

    private async Task LoadPrepNotesAsync()
    {
        var scriptKey = ResolvePrepNotesScriptKey();
        _prepNotes = string.IsNullOrWhiteSpace(scriptKey)
            ? []
            : await PrepNotesStore.LoadForScriptAsync(scriptKey);
    }

    private void HandlePrepNoteDraftChanged(ChangeEventArgs args) =>
        _prepNoteDraft = args.Value?.ToString() ?? string.Empty;

    private async Task AddPrepNoteAsync()
    {
        if (!CanAddPrepNote)
        {
            return;
        }

        var context = CurrentPrepNoteContext;
        var note = await PrepNotesStore.AddAsync(new LearnPrepNoteDraft(
            context.ScriptKey,
            context.SectionKey,
            context.SectionLabel,
            context.AnchorWord,
            context.WordIndex,
            _prepNoteDraft));

        if (note is null)
        {
            return;
        }

        _prepNoteDraft = string.Empty;
        _prepNotes = _prepNotes.Append(note).ToArray();
    }

    private LearnPrepNoteContext BuildCurrentPrepNoteContext()
    {
        var scriptKey = ResolvePrepNotesScriptKey();
        if (_timeline.Count == 0)
        {
            return new LearnPrepNoteContext(
                scriptKey,
                "empty",
                Text(UiTextKey.LearnReadyWord),
                Text(UiTextKey.LearnReadyWord),
                0);
        }

        var safeIndex = Math.Clamp(_currentIndex, 0, _timeline.Count - 1);
        var entry = _timeline[safeIndex];
        var sentenceRange = ResolveSentenceRange(_timeline, safeIndex);
        var sectionLabel = BuildSectionLabel(sentenceRange.StartIndex, sentenceRange.EndIndex);
        var anchorWord = NormalizeDisplayWord(entry.Word);
        return new LearnPrepNoteContext(
            scriptKey,
            $"{sentenceRange.StartIndex}-{sentenceRange.EndIndex}",
            sectionLabel,
            string.IsNullOrWhiteSpace(anchorWord) ? entry.Word : anchorWord,
            Math.Max(0, entry.WordIndex));
    }

    private string BuildSectionLabel(int startIndex, int endIndex)
    {
        var words = BuildDisplayContextWords(_timeline, startIndex, endIndex + 1);
        var label = words.Count == 0 ? _screenSubtitle : string.Join(' ', words);
        if (string.IsNullOrWhiteSpace(label))
        {
            label = _screenTitle;
        }

        return string.IsNullOrWhiteSpace(label)
            ? Text(UiTextKey.LearnReadyWord)
            : label;
    }

    private string ResolvePrepNotesScriptKey()
    {
        if (!string.IsNullOrWhiteSpace(SessionService.State.ScriptId))
        {
            return SessionService.State.ScriptId;
        }

        if (!string.IsNullOrWhiteSpace(SessionService.State.DocumentName))
        {
            return SessionService.State.DocumentName;
        }

        return SessionService.State.Title;
    }
}
