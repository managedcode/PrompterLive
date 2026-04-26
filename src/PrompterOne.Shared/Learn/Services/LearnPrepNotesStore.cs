using PrompterOne.Core.Abstractions;
using PrompterOne.Shared.Storage;

namespace PrompterOne.Shared.Learn.Services;

public sealed class LearnPrepNotesStore(IUserSettingsStore settingsStore)
{
    private const int MaxNoteLength = 2_000;

    private readonly IUserSettingsStore _settingsStore = settingsStore;

    public async Task<IReadOnlyList<LearnPrepNote>> LoadForScriptAsync(
        string scriptKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scriptKey))
        {
            return [];
        }

        var snapshot = await LoadSnapshotAsync(cancellationToken);
        return snapshot.Notes
            .Where(note => string.Equals(note.ScriptKey, scriptKey, StringComparison.Ordinal))
            .OrderBy(note => note.WordIndex)
            .ThenBy(note => note.CreatedAt)
            .ToArray();
    }

    public async Task<LearnPrepNote?> AddAsync(
        LearnPrepNoteDraft draft,
        CancellationToken cancellationToken = default)
    {
        var text = NormalizeText(draft.Text);
        if (string.IsNullOrWhiteSpace(draft.ScriptKey) || string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var note = new LearnPrepNote(
            Id: Guid.NewGuid().ToString("N"),
            ScriptKey: draft.ScriptKey,
            SectionKey: draft.SectionKey,
            SectionLabel: draft.SectionLabel,
            AnchorWord: draft.AnchorWord,
            WordIndex: draft.WordIndex,
            Text: text,
            CreatedAt: now);

        var snapshot = await LoadSnapshotAsync(cancellationToken);
        var notes = snapshot.Notes.ToList();
        notes.Add(note);
        await SaveSnapshotAsync(new LearnPrepNotesSnapshot(notes), cancellationToken);
        return note;
    }

    private async Task<LearnPrepNotesSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken)
    {
        var snapshot = await _settingsStore.LoadAsync<LearnPrepNotesSnapshot>(
            BrowserAppSettingsKeys.LearnPrepNotes,
            cancellationToken);

        return snapshot?.Normalize() ?? LearnPrepNotesSnapshot.Empty;
    }

    private Task SaveSnapshotAsync(LearnPrepNotesSnapshot snapshot, CancellationToken cancellationToken) =>
        _settingsStore.SaveAsync(BrowserAppSettingsKeys.LearnPrepNotes, snapshot.Normalize(), cancellationToken);

    private static string NormalizeText(string? text)
    {
        var normalized = string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
        return normalized.Length <= MaxNoteLength
            ? normalized
            : normalized[..MaxNoteLength];
    }
}

public sealed record LearnPrepNote(
    string Id,
    string ScriptKey,
    string SectionKey,
    string SectionLabel,
    string AnchorWord,
    int WordIndex,
    string Text,
    DateTimeOffset CreatedAt);

public sealed record LearnPrepNoteDraft(
    string ScriptKey,
    string SectionKey,
    string SectionLabel,
    string AnchorWord,
    int WordIndex,
    string Text);

public sealed record LearnPrepNotesSnapshot(IReadOnlyList<LearnPrepNote> Notes)
{
    public static LearnPrepNotesSnapshot Empty { get; } = new([]);

    public LearnPrepNotesSnapshot Normalize() =>
        this with { Notes = Notes?.Where(IsValid).ToArray() ?? [] };

    private static bool IsValid(LearnPrepNote? note) =>
        note is not null &&
        !string.IsNullOrWhiteSpace(note.Id) &&
        !string.IsNullOrWhiteSpace(note.ScriptKey) &&
        !string.IsNullOrWhiteSpace(note.Text);
}
