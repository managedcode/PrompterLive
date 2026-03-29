using Bunit;
using PrompterLive.Shared.Contracts;
using PrompterLive.Shared.Pages;
using PrompterLive.Shared.Tests;

namespace PrompterLive.App.Tests;

public sealed class ScreenShellContractTests : BunitContext
{
    private readonly AppHarness _harness;

    public ScreenShellContractTests()
    {
        _harness = TestHarnessFactory.Create(this);
    }

    [Fact]
    public void LibraryPage_RendersInteractiveLibraryShell()
    {
        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindByTestId(UiTestIds.Library.Page));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Library.FolderCreateTile));
            Assert.Contains(AppTestData.Scripts.DemoTitle, cut.Markup);
            Assert.Contains(AppTestData.Scripts.TedLeadershipTitle, cut.Markup);
        });
    }

    [Fact]
    public void EditorPage_RendersBodyOnlyEditorAndMetadataRail()
    {
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() =>
        {
            var sourceInput = cut.FindByTestId(UiTestIds.Editor.SourceInput);

            Assert.DoesNotContain("profile:", sourceInput.GetAttribute("value"), StringComparison.Ordinal);
            Assert.DoesNotContain("author:", sourceInput.GetAttribute("value"), StringComparison.Ordinal);
            Assert.NotNull(cut.FindByTestId(UiTestIds.Editor.Profile));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Editor.BaseWpm));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Editor.Author));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Editor.Version));
        });
    }

    [Fact]
    public void LearnPage_RendersCenteredRsvpSurfaceAndInitializesTimeline()
    {
        var cut = Render<LearnPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindByTestId(UiTestIds.Learn.Page));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Learn.Word));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Learn.OrpLine));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Learn.ContextLeft));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Learn.ContextRight));
            Assert.Contains("PrompterLiveDesign.setRsvpTimeline", string.Join('\n', _harness.JsRuntime.Invocations));
        });
    }

    [Fact]
    public void TeleprompterPage_RendersSingleBackgroundCameraReaderShell()
    {
        var cut = Render<TeleprompterPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindByTestId(UiTestIds.Teleprompter.Page));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Teleprompter.CameraBackground));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Teleprompter.CameraToggle));
            Assert.NotNull(cut.FindByTestId($"{UiTestIds.Teleprompter.Card(0)}-text"));
            Assert.DoesNotContain("rd-camera-overlay-", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("data-total-ms=\"", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void SettingsPage_RendersOperationalStudioSections()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindByTestId(UiTestIds.Settings.Page));
            Assert.Contains("Cloud Storage", cut.Markup);
            Assert.Contains("Streaming", cut.Markup);
            Assert.Contains("Audio Sync + Routing", cut.Markup);
            Assert.Contains("Default Camera", cut.Markup);
            Assert.Contains(AppTestData.Scripts.BroadcastMic, cut.Markup);
        });
    }
}
