using Bunit;
using PrompterLive.Shared.Pages;
using PrompterLive.Shared.Tests;

namespace PrompterLive.App.Tests;

public sealed class PageSmokeTests : BunitContext
{
    private readonly AppHarness _harness;

    public PageSmokeTests()
    {
        _harness = TestHarnessFactory.Create(this);
    }

    [Fact]
    public void LibraryPage_RendersExactDesignShell()
    {
        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("screen-library", cut.Markup);
            Assert.Contains("Product Launch", cut.Markup);
            Assert.Contains("TED: Leadership", cut.Markup);
            Assert.Contains("New Script", cut.Markup);
        });
    }

    [Fact]
    public void EditorPage_RendersToolbarStructureAndMetadataRail()
    {
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("STRUCTURE", cut.Markup);
            Assert.Contains("TPS Emotions", cut.Markup);
            Assert.Contains("Call to Action", cut.Markup);
            Assert.Contains("METADATA", cut.Markup);
        });
    }

    [Fact]
    public void LearnPage_RendersRsvpSurface()
    {
        var cut = Render<LearnPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("screen-rsvp", cut.Markup);
            Assert.Contains("rsvp-speed", cut.Markup);
            Assert.Contains("rsvp-progress-label", cut.Markup);
            Assert.Contains("PrompterLiveDesign.setRsvpTimeline", string.Join('\n', _harness.JsRuntime.Invocations));
        });
    }

    [Fact]
    public void TeleprompterPage_RendersReaderControls()
    {
        var cut = Render<TeleprompterPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("screen-teleprompter", cut.Markup);
            Assert.Contains("rd-countdown", cut.Markup);
            Assert.Contains("rd-block-indicator", cut.Markup);
            Assert.Contains("Opening Block", cut.Markup);
            Assert.Contains("data-camera-device-id=\"cam-1\"", cut.Markup);
            Assert.Contains("data-ms=\"", cut.Markup);
            Assert.Contains("data-total-ms=\"", cut.Markup);
        });
    }

    [Fact]
    public void SettingsPage_RendersSectionPanels()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Cloud Storage", cut.Markup);
            Assert.Contains("File Storage", cut.Markup);
            Assert.Contains("Streaming", cut.Markup);
            Assert.Contains("AI Provider", cut.Markup);
            Assert.Contains("Appearance", cut.Markup);
            Assert.Contains("About", cut.Markup);
            Assert.Contains("Front camera", cut.Markup);
            Assert.Contains("Broadcast mic", cut.Markup);
            Assert.Contains("Audio Sync + Routing", cut.Markup);
        });
    }
}
