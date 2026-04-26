using Bunit;
using Microsoft.AspNetCore.Components;
using PrompterOne.Shared.AppShell.Components;
using PrompterOne.Shared.Contracts;

namespace PrompterOne.Web.Tests;

public sealed class MainLayoutHeaderImportProgressTests : BunitContext
{
    private const string HomeIconKindValue = "home";
    private const string HomeLabel = "Home";
    private const string ImportFileName = "board-review.docx";
    private const string ImportLabel = "Import";
    private const string ProgressLabel = "Preparing script";
    private const string ProgressStepLabel = "2/3";
    private const string ProgressWidth = "66%";
    private const string SubtitleBackLabel = "This is a text description";
    private const string SupportedImportAcceptValue = ".txt,.md,.docx";
    private const string LongHeaderTitle = "Imported file title from a very long file name that should clamp inside the shared editor header without pushing shell actions away";

    [Test]
    public void MainLayoutHeader_ImportProgress_RendersVisibleStatusAndDisablesTrigger()
    {
        var cut = RenderHeader(parameters => parameters
            .Add(component => component.IsImportInProgress, true)
            .Add(component => component.ImportProgressFileName, ImportFileName)
            .Add(component => component.ImportProgressLabel, ProgressLabel)
            .Add(component => component.ImportProgressStepLabel, ProgressStepLabel)
            .Add(component => component.ImportProgressWidth, ProgressWidth));

        var importSurface = cut.FindByTestId(UiTestIds.Header.EditorImportScript);
        var importButton = cut.FindByTestId(UiTestIds.Header.EditorImportScriptButton);

        Assert.Equal("true", importSurface.GetAttribute("data-busy"));
        Assert.Equal("true", importButton.GetAttribute("aria-busy"));
        Assert.NotNull(importButton.GetAttribute("disabled"));
        Assert.Equal(ProgressLabel, cut.FindByTestId(UiTestIds.Header.ImportProgressLabel).TextContent.Trim());
        Assert.Equal(ProgressStepLabel, cut.FindByTestId(UiTestIds.Header.ImportProgressStep).TextContent.Trim());
        Assert.Equal(ImportFileName, cut.FindByTestId(UiTestIds.Header.ImportProgressFile).TextContent.Trim());
        Assert.Contains(
            "width: 66%;",
            cut.FindByTestId(UiTestIds.Header.ImportProgressFill).GetAttribute("style"),
            StringComparison.Ordinal);
    }

    [Test]
    public void MainLayoutHeader_LongEditorTitle_RendersTooltipAndClampClass()
    {
        var cut = RenderHeader(headerTitle: LongHeaderTitle);

        var title = cut.FindByTestId(UiTestIds.Header.Title);

        Assert.Contains("app-header-title", title.ClassList);
        Assert.Equal(LongHeaderTitle, title.GetAttribute("title"));
        Assert.Equal(LongHeaderTitle, title.TextContent.Trim());
    }

    [Test]
    public void MainLayoutHeader_HomeControl_UsesRecognizableHomeIcon()
    {
        var cut = RenderHeader();

        var home = cut.FindByTestId(UiTestIds.Header.Home);
        var homeIcon = cut.FindByTestId(UiTestIds.Header.HomeBrandMark);

        Assert.Equal(HomeLabel, home.GetAttribute("aria-label"));
        Assert.Equal(HomeLabel, home.GetAttribute("title"));
        Assert.Equal(HomeIconKindValue, homeIcon.GetAttribute("data-icon-kind"));
        Assert.Contains("<svg", homeIcon.InnerHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("PrompterOne", cut.FindByTestId(UiTestIds.Header.Brand).TextContent.Trim());
    }

    [Test]
    public void MainLayoutHeader_SubtitleBackText_InvokesBackAction()
    {
        var backClicked = false;
        var cut = RenderHeader(parameters => parameters
            .Add(component => component.ShowBackButton, true)
            .Add(component => component.ShowHeaderSubtitle, true)
            .Add(component => component.OnBackClick, EventCallback.Factory.Create(this, () => backClicked = true)),
            headerSubtitle: SubtitleBackLabel);

        var subtitle = cut.FindByTestId(UiTestIds.Header.Subtitle);

        Assert.Equal("button", subtitle.TagName.ToLowerInvariant());
        Assert.Contains("app-header-subtitle-back", subtitle.ClassList);

        subtitle.Click();

        Assert.True(backClicked);
    }

    private IRenderedComponent<MainLayoutHeader> RenderHeader(
        Action<ComponentParameterCollectionBuilder<MainLayoutHeader>>? configure = null,
        string headerTitle = "Editor",
        string headerSubtitle = "")
    {
        return Render<MainLayoutHeader>(parameters =>
        {
            parameters.Add(component => component.CssClass, "app-header");
            parameters.Add(component => component.HeaderSubtitle, headerSubtitle);
            parameters.Add(component => component.HeaderTitle, headerTitle);
            parameters.Add(component => component.HomeLabel, HomeLabel);
            parameters.Add(component => component.ImportActionButtonTestId, UiTestIds.Header.EditorImportScriptButton);
            parameters.Add(component => component.ImportActionIconTestId, UiTestIds.Header.EditorImportScriptIcon);
            parameters.Add(component => component.ImportActionInputId, UiDomIds.AppShell.EditorImportScriptInput);
            parameters.Add(component => component.ImportActionInputTestId, UiTestIds.Header.EditorImportScriptInput);
            parameters.Add(component => component.ImportActionLabel, ImportLabel);
            parameters.Add(component => component.ImportActionSurfaceTestId, UiTestIds.Header.EditorImportScript);
            parameters.Add(component => component.LearnLabel, "Learn");
            parameters.Add(component => component.LibraryAllScriptsLabel, "All Scripts");
            parameters.Add(component => component.LibraryBreadcrumbCurrentLabel, "All Scripts");
            parameters.Add(component => component.GoLiveLabel, "Go Live");
            parameters.Add(component => component.GoLiveState, "idle");
            parameters.Add(component => component.GoLiveStatus, string.Empty);
            parameters.Add(component => component.NewScriptLabel, "New Script");
            parameters.Add(component => component.ReadLabel, "Read");
            parameters.Add(component => component.RestartTourLabel, "Restart tour");
            parameters.Add(component => component.SaveFileLabel, "Export");
            parameters.Add(component => component.SearchPlaceholder, "Search");
            parameters.Add(component => component.SearchText, string.Empty);
            parameters.Add(component => component.ShowImportAction, true);
            parameters.Add(component => component.SupportedImportAcceptValue, SupportedImportAcceptValue);
            parameters.Add(component => component.WpmLabel, string.Empty);

            configure?.Invoke(parameters);
        });
    }
}
