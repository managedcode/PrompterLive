using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrompterLive.Shared.Layout;
using PrompterLive.Shared.Pages;
using PrompterLive.Shared.Tests;

namespace PrompterLive.App.Tests;

public sealed class UiTraceLoggingTests : BunitContext
{
    [Fact]
    public void MainLayout_LogsNavigatorAttach_AndRouteChanges()
    {
        var logProvider = new RecordingLoggerProvider();
        _ = TestHarnessFactory.Create(
            this,
            configureLogging: builder => builder.AddProvider(logProvider));

        var cut = Render<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(
                logProvider.Entries,
                entry => entry.Category.Contains(nameof(MainLayout), StringComparison.Ordinal) &&
                    entry.Message.Contains("Attached SPA navigator bridge.", StringComparison.Ordinal));
        });

        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/settings");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(
                logProvider.Entries,
                entry => entry.Category.Contains(nameof(MainLayout), StringComparison.Ordinal) &&
                    entry.Message.Contains("Route changed to", StringComparison.Ordinal) &&
                    entry.Message.Contains("/settings", StringComparison.Ordinal));
        });
    }

    [Fact]
    public async Task LibraryPage_LogsFolderSelection_OverlayActions_AndFolderCreation()
    {
        var logProvider = new RecordingLoggerProvider();
        _ = TestHarnessFactory.Create(
            this,
            configureLogging: builder => builder.AddProvider(logProvider));

        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() => Assert.Contains("Product Launch", cut.Markup));

        cut.Find("[data-testid='library-folder-presentations']").Click();
        cut.Find("[data-testid='library-folder-create-start']").Click();
        cut.Find("[data-testid='library-new-folder-cancel']").Click();
        cut.Find("[data-testid='library-folder-create-start']").Click();
        cut.Find("[data-testid='library-new-folder-name']").Input("Roadshows");
        cut.Find("[data-testid='library-new-folder-parent']").Change("presentations");
        cut.Find("[data-testid='library-new-folder-submit']").Click();

        cut.WaitForAssertion(() => Assert.Contains("library-folder-roadshows", cut.Markup));

        Assert.Contains(
            logProvider.Entries,
            entry => entry.Category.Contains(nameof(LibraryPage), StringComparison.Ordinal) &&
                entry.Message.Contains("Selecting library folder presentations.", StringComparison.Ordinal));
        Assert.Contains(
            logProvider.Entries,
            entry => entry.Category.Contains(nameof(LibraryPage), StringComparison.Ordinal) &&
                entry.Message.Contains("Opening library folder overlay", StringComparison.Ordinal) &&
                entry.Message.Contains("presentations", StringComparison.Ordinal));
        Assert.Contains(
            logProvider.Entries,
            entry => entry.Category.Contains(nameof(LibraryPage), StringComparison.Ordinal) &&
                entry.Message.Contains("Cancelling library folder overlay.", StringComparison.Ordinal));
        Assert.Contains(
            logProvider.Entries,
            entry => entry.Category.Contains(nameof(LibraryPage), StringComparison.Ordinal) &&
                entry.Message.Contains("Created library folder roadshows under presentations.", StringComparison.Ordinal));
    }
}
