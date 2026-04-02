using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Tests;

namespace PrompterOne.App.Tests;

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
            var sourceValue = sourceInput.GetAttribute("value") ?? string.Empty;

            Assert.Equal(string.Empty, sourceValue);
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
            Assert.NotNull(cut.FindByTestId(UiTestIds.Learn.Display));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Learn.Word));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Learn.WordShell));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Learn.OrpLine));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Learn.ContextLeft));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Learn.ContextRight));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Learn.ProgressLabel));
            Assert.Equal("0", cut.FindByTestId(UiTestIds.Learn.Page).GetAttribute("tabindex"));
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
            Assert.NotNull(cut.FindByTestId(UiTestIds.Teleprompter.Stage));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Teleprompter.Controls));
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
            Assert.Contains("Choose which cameras are available in GO LIVE", cut.Markup);
            Assert.Contains("Mute all microphones when not in GO LIVE", cut.Markup);
            Assert.Contains("Teleprompter Display", cut.Markup);
            Assert.NotNull(cut.FindByTestId(UiTestIds.Settings.MicPreviewCard));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Settings.CameraPrimaryAction(AppTestData.Camera.FirstDeviceId)));
        });
    }

    [Fact]
    public void GoLivePage_RendersDedicatedLiveRoutingSurface()
    {
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.Page));
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.ProgramCard));
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.SourcesCard));
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.OpenSettings));
            Assert.Empty(cut.FindAll($"[data-testid='{UiTestIds.GoLive.ProviderCard(GoLiveTargetCatalog.TargetIds.Obs)}']"));
            Assert.Empty(cut.FindAll($"[data-testid='{UiTestIds.GoLive.ProviderCard(GoLiveTargetCatalog.TargetIds.Ndi)}']"));
            Assert.Empty(cut.FindAll($"[data-testid='{UiTestIds.GoLive.ProviderCard(GoLiveTargetCatalog.TargetIds.Recording)}']"));
        });
    }
}
