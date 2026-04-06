using PrompterOne.Shared.Services;
using PrompterOne.Shared.Services.Editor;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class EditorLocalRevisionStoreTests
{
    private const string ScriptId = "history-script";
    private static readonly DateTimeOffset BaseTimestamp = new(2026, 4, 5, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task RecordAsync_DeduplicatesLatestRevision_AndTrimsOldEntries()
    {
        var store = CreateStore();

        for (var index = 0; index < EditorLocalRevisionStoreTestSource.RevisionSeedCount; index++)
        {
            await store.RecordAsync(CreateDraft(index));
        }

        await store.RecordAsync(CreateDraft(EditorLocalRevisionStoreTestSource.LatestDuplicateIndex));

        var revisions = await store.LoadAsync(ScriptId);

        Assert.Equal(EditorLocalRevisionStoreTestSource.MaximumRetainedRevisions, revisions.Count);
        Assert.Equal(EditorLocalRevisionStoreTestSource.NewestRevisionTitle, revisions[0].Title);
        Assert.DoesNotContain(
            revisions,
            revision => string.Equals(
                revision.Title,
                EditorLocalRevisionStoreTestSource.TrimmedRevisionTitle,
                StringComparison.Ordinal));
    }

    [Test]
    public async Task GetAsync_ReturnsStoredRevisionByIdentifier()
    {
        var store = CreateStore();

        var expected = (await store.RecordAsync(CreateDraft(0))).Single();
        var actual = await store.GetAsync(ScriptId, expected.Id);

        Assert.NotNull(actual);
        Assert.Equal(expected.Title, actual!.Title);
        Assert.Equal(expected.PersistedText, actual.PersistedText);
    }

    private static EditorLocalRevisionStore CreateStore()
    {
        var jsRuntime = new TestJsRuntime();
        var messageBus = new CrossTabMessageBus(jsRuntime);
        var settingsStore = new BrowserSettingsStore(jsRuntime, messageBus);
        return new EditorLocalRevisionStore(settingsStore);
    }

    private static EditorLocalRevisionDraft CreateDraft(int index) =>
        new(
            ScriptId,
            $"Revision {index}",
            $"{ScriptId}.tps",
            $"---\ntitle: \"Revision {index}\"\n---\n\nLine {index}",
            BaseTimestamp.AddMinutes(index));

    private static class EditorLocalRevisionStoreTestSource
    {
        public const int LatestDuplicateIndex = 13;
        public const int MaximumRetainedRevisions = 12;
        public const string NewestRevisionTitle = "Revision 13";
        public const int RevisionSeedCount = 14;
        public const string TrimmedRevisionTitle = "Revision 1";
    }
}
