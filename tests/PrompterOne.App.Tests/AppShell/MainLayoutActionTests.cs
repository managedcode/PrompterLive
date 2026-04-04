using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Layout;
using PrompterOne.Shared.Tests;

namespace PrompterOne.App.Tests;

public sealed class MainLayoutActionTests : BunitContext
{
    [Theory]
    [InlineData(AppRoutes.Learn, AppTestData.Scripts.QuantumId)]
    [InlineData(AppRoutes.Teleprompter, AppTestData.Scripts.QuantumId)]
    public void MainLayout_HeaderBack_UsesScopedEditorRoute_ForPlaybackScreens(string route, string scriptId)
    {
        _ = TestHarnessFactory.Create(this);
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(string.Concat(route, "?", AppRoutes.ScriptIdQueryKey, "=", scriptId));

        var cut = Render<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        cut.FindByTestId(UiTestIds.Header.Back).Click();

        Assert.EndsWith(AppRoutes.EditorWithId(scriptId), navigation.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void MainLayout_HeaderBack_UsesOriginRoute_ForSettingsScreen()
    {
        _ = TestHarnessFactory.Create(this);
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(AppRoutes.GoLiveWithId(AppTestData.Scripts.DemoId));

        var cut = Render<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        navigation.NavigateTo(AppRoutes.Settings);
        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.Header.Back)));

        cut.FindByTestId(UiTestIds.Header.Back).Click();

        Assert.EndsWith(AppRoutes.GoLiveWithId(AppTestData.Scripts.DemoId), navigation.Uri, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(AppRoutes.Library)]
    [InlineData(AppRoutes.Settings)]
    public void MainLayout_RendersGoLiveAction_OnEveryNonGoLiveScreen(string route)
    {
        _ = TestHarnessFactory.Create(this);
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(route);

        var cut = Render<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.Header.GoLive)));
    }

    [Fact]
    public void MainLayout_LibraryHeaderMatchesReferenceActionOrder()
    {
        _ = TestHarnessFactory.Create(this);
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(AppRoutes.Library);

        var cut = Render<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        cut.WaitForAssertion(() =>
        {
            var goLive = cut.FindByTestId(UiTestIds.Header.GoLive);
            var newScript = cut.FindByTestId(UiTestIds.Header.LibraryNewScript);

            Assert.Contains("btn-golive-header", goLive.ClassName, StringComparison.Ordinal);
            Assert.Contains("btn-create", newScript.ClassName, StringComparison.Ordinal);

            var goLiveIndex = cut.Markup.IndexOf(UiTestIds.Header.GoLive, StringComparison.Ordinal);
            var newScriptIndex = cut.Markup.IndexOf(UiTestIds.Header.LibraryNewScript, StringComparison.Ordinal);
            Assert.True(goLiveIndex >= 0 && newScriptIndex >= 0 && goLiveIndex < newScriptIndex);
        });
    }
}
