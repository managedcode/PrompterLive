using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Layout;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class UiTraceLoggingTests : BunitContext
{
    [Test]
    public void MainLayout_LogsRouteChanges_WithoutShellBridgeInterop()
    {
        var logProvider = new RecordingLoggerProvider();
        var harness = TestHarnessFactory.Create(
            this,
            configureLogging: builder => builder.AddProvider(logProvider));

        var cut = Render<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        Assert.DoesNotContain(
            harness.JsRuntime.Invocations,
            invocation => invocation.Contains("PrompterOne.shell", StringComparison.Ordinal));

        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(AppTestData.Routes.Settings);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(
                logProvider.Entries,
                entry => entry.Category.Contains(nameof(MainLayout), StringComparison.Ordinal) &&
                    entry.Message.Contains("Route changed to", StringComparison.Ordinal) &&
                    entry.Message.Contains(AppTestData.Routes.Settings, StringComparison.Ordinal));
        });
    }

    [Test]
    public async Task LibraryPage_LogsFolderSelection_OverlayActions_AndFolderCreation()
    {
        var logProvider = new RecordingLoggerProvider();
        _ = TestHarnessFactory.Create(
            this,
            configureLogging: builder => builder.AddProvider(logProvider));

        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() => Assert.Contains(AppTestData.Scripts.DemoTitle, cut.Markup, StringComparison.Ordinal));

        cut.FindByTestId(UiTestIds.Library.Folder(AppTestData.Folders.PresentationsId)).Click();
        cut.FindByTestId(UiTestIds.Library.FolderCreateStart).Click();
        cut.FindByTestId(UiTestIds.Library.NewFolderCancel).Click();
        cut.FindByTestId(UiTestIds.Library.FolderCreateStart).Click();
        cut.FindByTestId(UiTestIds.Library.NewFolderName).Input(AppTestData.Folders.Roadshows);
        cut.FindByTestId(UiTestIds.Library.NewFolderParent).Change(AppTestData.Folders.PresentationsId);
        cut.FindByTestId(UiTestIds.Library.NewFolderSubmit).Click();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Library.Folder(AppTestData.Folders.RoadshowsId), cut.Markup, StringComparison.Ordinal));

        Assert.Contains(
            logProvider.Entries,
            entry => entry.Category.Contains(nameof(LibraryPage), StringComparison.Ordinal) &&
                entry.Message.Contains($"Selecting library folder {AppTestData.Folders.PresentationsId}.", StringComparison.Ordinal));
        Assert.Contains(
            logProvider.Entries,
            entry => entry.Category.Contains(nameof(LibraryPage), StringComparison.Ordinal) &&
                entry.Message.Contains("Opening library folder overlay", StringComparison.Ordinal) &&
                entry.Message.Contains(AppTestData.Folders.PresentationsId, StringComparison.Ordinal));
        Assert.Contains(
            logProvider.Entries,
            entry => entry.Category.Contains(nameof(LibraryPage), StringComparison.Ordinal) &&
                entry.Message.Contains("Cancelling library folder overlay.", StringComparison.Ordinal));
        Assert.Contains(
            logProvider.Entries,
            entry => entry.Category.Contains(nameof(LibraryPage), StringComparison.Ordinal) &&
                entry.Message.Contains($"Created library folder {AppTestData.Folders.RoadshowsId} under {AppTestData.Folders.PresentationsId}.", StringComparison.Ordinal));
    }
}
