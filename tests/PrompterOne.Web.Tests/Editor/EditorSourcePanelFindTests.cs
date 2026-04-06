using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using PrompterOne.Shared.Components.Editor;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class EditorSourcePanelFindTests : BunitContext
{
    private const string MissingQuery = "nonexistent";
    private const string RepeatedQuery = "welcome";
    private const string ResultSummary = "1 / 2";

    public EditorSourcePanelFindTests()
    {
        _ = TestHarnessFactory.Create(this);
    }

    [Test]
    public void EditorSourcePanel_FindBarOpens_AndReportsMatchCount()
    {
        var cut = Render<EditorSourcePanelFindHost>();

        cut.FindByTestId(UiTestIds.Editor.FindToggle).Click();
        cut.FindByTestId(UiTestIds.Editor.FindInput).Input(RepeatedQuery);

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindByTestId(UiTestIds.Editor.FindBar));
            Assert.Equal(ResultSummary, cut.FindByTestId(UiTestIds.Editor.FindResult).TextContent.Trim());
        });
    }

    [Test]
    public void EditorSourcePanel_FindBarShowsNoMatchesState()
    {
        var cut = Render<EditorSourcePanelFindHost>();

        cut.FindByTestId(UiTestIds.Editor.FindToggle).Click();
        cut.FindByTestId(UiTestIds.Editor.FindInput).Input(MissingQuery);

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("No matches", cut.FindByTestId(UiTestIds.Editor.FindResult).TextContent.Trim());
            Assert.True(cut.FindByTestId(UiTestIds.Editor.FindNext).HasAttribute("disabled"));
            Assert.True(cut.FindByTestId(UiTestIds.Editor.FindPrevious).HasAttribute("disabled"));
        });
    }

    private sealed class EditorSourcePanelFindHost : ComponentBase
    {
        private const string SourceText = "Alpha welcome beta welcome gamma";

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<EditorSourcePanel>(0);
            builder.AddAttribute(1, nameof(EditorSourcePanel.Text), SourceText);
            builder.AddAttribute(2, nameof(EditorSourcePanel.Selection), EditorSelectionViewModel.Empty);
            builder.AddAttribute(3, nameof(EditorSourcePanel.Status), new EditorStatusViewModel(1, 1, "Actor", 140, 5, 1, 1, "0:05", "1.0"));
            builder.CloseComponent();
        }
    }
}
