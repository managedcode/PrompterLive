using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class ScreenShellContractTests : BunitContext
{
    private readonly AppHarness _harness;

    public ScreenShellContractTests()
    {
        _harness = TestHarnessFactory.Create(this);
    }

    [Test]
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

    [Test]
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
            Assert.NotNull(cut.FindByTestId(UiTestIds.Editor.Title));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Editor.Profile));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Editor.BaseWpm));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Editor.Author));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Editor.Version));
        });
    }

    [Test]
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
            Assert.NotNull(cut.Find("#" + UiDomIds.Learn.ProgressFill + ".ui-percent-fill"));
            Assert.NotNull(cut.Find(".rsvp-crosshair .ui-crosshair-guide"));
            Assert.True(cut.FindAll(".rsvp-bottom .ui-icon").Count >= 7);
            Assert.Equal("0", cut.FindByTestId(UiTestIds.Learn.Page).GetAttribute("tabindex"));
        });
    }

    [Test]
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

    [Test]
    public void SettingsPage_RendersOperationalStudioSections()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindByTestId(UiTestIds.Settings.Page));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Settings.CloudPanel));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Settings.CamerasPanel));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Settings.MicsPanel));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Settings.AppearancePanel));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Settings.LanguagePanel));
            Assert.Contains(Text(UiTextKey.SettingsNavCloud), cut.Markup);
            Assert.Contains(Text(UiTextKey.SettingsNavLanguage), cut.Markup);
            Assert.Contains(Text(UiTextKey.SettingsCamerasSectionDescription), cut.Markup);
            Assert.Contains(Text(UiTextKey.SettingsMicrophonesMuteWhenNotInGoLive), cut.Markup);
            Assert.Contains(Text(UiTextKey.SettingsAppearanceTeleprompterTitle), cut.Markup);
            Assert.NotNull(cut.FindByTestId(UiTestIds.Settings.MicPreviewCard));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Settings.CameraPrimaryAction(AppTestData.Camera.FirstDeviceId)));
        });
    }

    [Test]
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
            Assert.Empty(cut.FindAll($"[data-testid='{UiTestIds.GoLive.ProviderCard(GoLiveTargetCatalog.TargetIds.Recording)}']"));
        });
    }

    private string Text(UiTextKey key) =>
        Services.GetRequiredService<IStringLocalizer<SharedResource>>()[key.ToString()];
}
