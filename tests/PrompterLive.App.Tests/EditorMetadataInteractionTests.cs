using Bunit;
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

        cut.Find("[data-testid='editor-profile']").Change("RSVP");
        cut.Find("[data-testid='editor-base-wpm']").Change("210");
        cut.Find("[data-testid='editor-author']").Change("Test Speaker");
        cut.Find("[data-testid='editor-created']").Change("2026-03-26");
        cut.Find("[data-testid='editor-version']").Change("2.0");

        cut.WaitForAssertion(() =>
        {
            var metadata = _harness.Session.State.CompiledScript?.Metadata;

            Assert.NotNull(metadata);
            Assert.Equal("RSVP", metadata!["profile"]);
            Assert.Equal("210", metadata["base_wpm"]);
            Assert.Equal("Test Speaker", metadata["author"]);
            Assert.Equal("2026-03-26", metadata["created"]);
            Assert.Equal("2.0", metadata["version"]);
            Assert.Contains("210 WPM", cut.Markup);
            Assert.Contains("Test Speaker", cut.Markup);
            Assert.Contains("TPS v2.0", cut.Markup);
        });
    }
}
