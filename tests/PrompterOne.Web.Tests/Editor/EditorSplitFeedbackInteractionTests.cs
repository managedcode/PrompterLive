using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

[NotInParallel]
public sealed class EditorSplitFeedbackInteractionTests : BunitContext
{
    public EditorSplitFeedbackInteractionTests()
    {
        TestHarnessFactory.Create(this);
    }

    [Test]
    public void EditorPage_SplitFeedbackStaysVisibleAcrossRedundantSourceChangeEvents()
    {
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(AppTestData.Routes.EditorDemo);
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() =>
        {
            var source = cut.FindByTestId(UiTestIds.Editor.SourceInput);
            Assert.Contains(AppTestData.Editor.BodyHeading, source.GetAttribute("value"));
        });

        cut.FindByTestId(UiTestIds.Editor.SourceInput).Input(EditorSplitFeedbackInteractionTestSource.SplitSource);
        cut.FindByTestId(UiTestIds.Editor.SplitSegment).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(
                EditorSplitFeedbackInteractionTestSource.SplitFeedbackTitle,
                cut.FindByTestId(UiTestIds.Editor.SplitResultTitle).TextContent.Trim());
            Assert.Equal(
                EditorSplitFeedbackInteractionTestSource.SplitActionLabel,
                cut.FindByTestId(UiTestIds.Editor.SplitResultOpenLibrary).TextContent.Trim());
        });

        cut.FindByTestId(UiTestIds.Editor.SourceInput).Input(EditorSplitFeedbackInteractionTestSource.SplitSource);

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(
                EditorSplitFeedbackInteractionTestSource.SplitActionLabel,
                cut.FindByTestId(UiTestIds.Editor.SplitResultOpenLibrary).TextContent.Trim());
        });

        cut.FindByTestId(UiTestIds.Editor.SplitResultOpenLibrary).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.EndsWith(AppRoutes.Library, navigationManager.Uri, StringComparison.Ordinal);
        });
    }

    private static class EditorSplitFeedbackInteractionTestSource
    {
        public const string SplitActionLabel = "Open in Library";
        public const string SplitFeedbackTitle = "Split complete";
        public const string SplitSource =
            """
            ## [Episode 1 - How to Think About Systems|140WPM|Professional]
            Before you write code, / you need to think about the system. //

            ## [Episode 2 - How Systems Talk to Each Other|140WPM|Professional]
            APIs, events, and retries matter. //
            """;
    }
}
