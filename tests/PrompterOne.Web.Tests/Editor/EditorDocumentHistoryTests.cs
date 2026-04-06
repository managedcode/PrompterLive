using PrompterOne.Core.Models.Editor;
using PrompterOne.Shared.Services.Editor;

namespace PrompterOne.Web.Tests;

public sealed class EditorDocumentHistoryTests
{
    [Test]
    public void TryUndoAndTryRedo_ReplaySnapshotsInOrder()
    {
        var history = new EditorDocumentHistory();
        history.Reset("alpha", new EditorSelectionRange(5, 5));
        history.TryRecord("alpha beta", new EditorSelectionRange(10, 10));
        history.TryRecord("alpha beta gamma", new EditorSelectionRange(16, 16));

        Assert.True(history.CanUndo);
        Assert.True(history.TryUndo(out var undoSnapshot));
        Assert.Equal("alpha beta", undoSnapshot.Text);
        Assert.Equal(new EditorSelectionRange(10, 10), undoSnapshot.Selection);

        Assert.True(history.CanRedo);
        Assert.True(history.TryRedo(out var redoSnapshot));
        Assert.Equal("alpha beta gamma", redoSnapshot.Text);
        Assert.Equal(new EditorSelectionRange(16, 16), redoSnapshot.Selection);
    }

    [Test]
    public void UpdateSelection_RewritesCurrentSnapshotWithoutAddingHistoryEntries()
    {
        var history = new EditorDocumentHistory();
        history.Reset("alpha", new EditorSelectionRange(0, 0));
        history.TryRecord("alpha beta", new EditorSelectionRange(5, 5));

        history.UpdateSelection(new EditorSelectionRange(10, 10));

        Assert.True(history.TryUndo(out var undoSnapshot));
        Assert.Equal("alpha", undoSnapshot.Text);
        Assert.False(history.CanUndo);
        Assert.True(history.TryRedo(out var redoSnapshot));
        Assert.Equal(new EditorSelectionRange(10, 10), redoSnapshot.Selection);
    }
}
