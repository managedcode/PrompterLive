using Bunit;
using PrompterLive.Shared.Contracts;
using PrompterLive.Shared.Pages;
using PrompterLive.Shared.Tests;

namespace PrompterLive.App.Tests;

public sealed class EditorMetadataInteractionTests : BunitContext
{
    private readonly AppHarness _harness;

    public EditorMetadataInteractionTests()
    {
        _harness = TestHarnessFactory.Create(this);
    }

    [Fact]
    public void EditorPage_UpdatesFrontMatterWhenMetadataChanges()
    {
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() => Assert.Contains("Product Launch", cut.Markup));

        cut.FindByTestId(UiTestIds.Editor.Profile).Change(EditorMetadataTestSource.ProfileRsvp);
        cut.FindByTestId(UiTestIds.Editor.BaseWpm).Change(EditorMetadataTestSource.BaseWpm210);
        cut.FindByTestId(UiTestIds.Editor.Author).Change(AppTestData.Editor.TestSpeaker);
        cut.FindByTestId(UiTestIds.Editor.Created).Change(AppTestData.Editor.CreatedDate);
        cut.FindByTestId(UiTestIds.Editor.Version).Change(AppTestData.Editor.Version);

        cut.WaitForAssertion(() =>
        {
            var metadata = _harness.Session.State.CompiledScript?.Metadata;
            var visibleSource = cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value") ?? string.Empty;
            var persistedText = _harness.Session.State.Text;

            Assert.NotNull(metadata);
            Assert.Equal(EditorMetadataTestSource.ProfileRsvp, metadata!["profile"]);
            Assert.Equal(EditorMetadataTestSource.BaseWpm210, metadata["base_wpm"]);
            Assert.Equal(AppTestData.Editor.TestSpeaker, metadata["author"]);
            Assert.Equal(AppTestData.Editor.CreatedDate, metadata["created"]);
            Assert.Equal(AppTestData.Editor.Version, metadata["version"]);
            Assert.Contains(EditorMetadataTestSource.WpmSummary, cut.Markup);
            Assert.Contains(EditorMetadataTestSource.VersionSummary, cut.Markup);
            Assert.DoesNotContain(EditorMetadataTestSource.AuthorField, visibleSource, StringComparison.Ordinal);
            Assert.DoesNotContain(EditorMetadataTestSource.VersionField, visibleSource, StringComparison.Ordinal);
            Assert.Contains(EditorMetadataTestSource.AuthorPersistenceLine, persistedText, StringComparison.Ordinal);
            Assert.Contains(EditorMetadataTestSource.VersionPersistenceLine, persistedText, StringComparison.Ordinal);
        });
    }

    private static class EditorMetadataTestSource
    {
        public const string AuthorField = "author:";
        public const string AuthorPersistenceLine = "author: \"Test Speaker\"";
        public const string BaseWpm210 = "210";
        public const string ProfileRsvp = "RSVP";
        public const string VersionField = "version:";
        public const string VersionPersistenceLine = "version: \"2.0\"";
        public const string VersionSummary = "TPS v2.0";
        public const string WpmSummary = "210 WPM";
    }
}
