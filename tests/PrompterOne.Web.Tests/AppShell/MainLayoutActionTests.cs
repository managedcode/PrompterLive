using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Core.Localization;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Core.Services.Editor;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Layout;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class MainLayoutActionTests : BunitContext
{
    private const string EnglishExportLabel = "Export";
    private const string EnglishGoLiveLabel = "Go Live";
    private const string EnglishImportLabel = "Import";
    private const string SupportedImportAcceptValue = ScriptDocumentFileTypes.AcceptValue;
    private const string UkrainianExportLabel = "Експорт";
    private const string UkrainianImportLabel = "Імпорт";

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

        cut.WaitForAssertion(() =>
        {
            var goLive = cut.FindByTestId(UiTestIds.Header.GoLive);

            Assert.Equal(GoLiveIndicatorStates.Idle, goLive.GetAttribute("data-live-state"));
            Assert.NotNull(goLive.QuerySelector(BunitTestSelectors.BuildTestIdSelector(UiTestIds.Header.GoLiveDot)));
            Assert.NotNull(goLive.QuerySelector(BunitTestSelectors.BuildTestIdSelector(UiTestIds.Header.GoLiveIcon)));
            Assert.NotNull(goLive.QuerySelector(BunitTestSelectors.BuildTestIdSelector(UiTestIds.Header.GoLiveLabel)));
            Assert.Null(goLive.QuerySelector(BunitTestSelectors.BuildTestIdSelector(UiTestIds.Header.GoLiveStatus)));
        });
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
            var openScript = cut.FindByTestId(UiTestIds.Header.LibraryOpenScript);
            var openScriptInput = cut.FindByTestId(UiTestIds.Header.LibraryOpenScriptInput);
            var newScript = cut.FindByTestId(UiTestIds.Header.LibraryNewScript);

            Assert.Equal(
                EnglishGoLiveLabel,
                goLive.QuerySelector(BunitTestSelectors.BuildTestIdSelector(UiTestIds.Header.GoLiveLabel))?.TextContent.Trim());
            Assert.NotNull(goLive.QuerySelector(BunitTestSelectors.BuildTestIdSelector(UiTestIds.Header.GoLiveDot)));
            Assert.Contains("btn-create", newScript.ClassName, StringComparison.Ordinal);
            Assert.Equal(SupportedImportAcceptValue, openScriptInput.GetAttribute("accept"));
            Assert.Contains(EnglishImportLabel, openScript.TextContent, StringComparison.Ordinal);

            var goLiveIndex = cut.Markup.IndexOf(UiTestIds.Header.GoLive, StringComparison.Ordinal);
            var openScriptIndex = cut.Markup.IndexOf(UiTestIds.Header.LibraryOpenScript, StringComparison.Ordinal);
            var newScriptIndex = cut.Markup.IndexOf(UiTestIds.Header.LibraryNewScript, StringComparison.Ordinal);
            Assert.True(goLiveIndex >= 0 && openScriptIndex >= 0 && newScriptIndex >= 0);
            Assert.True(goLiveIndex < openScriptIndex);
            Assert.True(openScriptIndex < newScriptIndex);
            Assert.NotNull(openScript);
        });
    }

    [Fact]
    public void MainLayout_LibraryHeader_UsesSharedBrandAndIconPrimitives()
    {
        _ = TestHarnessFactory.Create(this);
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(AppRoutes.Library);

        var cut = Render<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        cut.WaitForAssertion(() =>
        {
            var home = cut.FindByTestId(UiTestIds.Header.Home);
            var header = cut.Find("header");

            Assert.NotNull(home.QuerySelector(".app-brand-mark"));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Header.GoLive).QuerySelector(".ui-icon"));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Header.LibraryOpenScript).QuerySelector(".ui-icon"));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Header.LibraryNewScript).QuerySelector(".ui-icon"));
            Assert.True(header.QuerySelectorAll(".ui-icon").Length >= 3);
        });
    }

    [Theory]
    [InlineData(AppRoutes.Settings)]
    [InlineData(AppRoutes.Editor)]
    public void MainLayout_OpenScriptAction_IsHidden_OnNonLibraryScreens(string route)
    {
        _ = TestHarnessFactory.Create(this);
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(route);

        var cut = Render<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll(BunitTestSelectors.BuildTestIdSelector(UiTestIds.Header.LibraryOpenScript)));
            Assert.Empty(cut.FindAll(BunitTestSelectors.BuildTestIdSelector(UiTestIds.Header.LibraryOpenScriptInput)));
        });
    }

    [Fact]
    public void MainLayout_OpenScriptAction_UsesStableDialogButtonAndInputDomId()
    {
        _ = TestHarnessFactory.Create(this);
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(AppRoutes.Library);

        var cut = Render<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        cut.WaitForAssertion(() =>
        {
            var openScriptSurface = cut.FindByTestId(UiTestIds.Header.LibraryOpenScript);
            var openScriptButton = openScriptSurface.QuerySelector("button");
            var openScriptInput = cut.FindByTestId(UiTestIds.Header.LibraryOpenScriptInput);

            Assert.NotNull(openScriptButton);
            Assert.Equal("dialog", openScriptButton!.GetAttribute("aria-haspopup"));
            Assert.Contains(EnglishImportLabel, openScriptButton.TextContent, StringComparison.Ordinal);
            Assert.Equal(UiDomIds.AppShell.LibraryOpenScriptInput, openScriptInput.GetAttribute("id"));
            Assert.Equal(SupportedImportAcceptValue, openScriptInput.GetAttribute("accept"));
            Assert.Equal(EnglishImportLabel, openScriptInput.GetAttribute("aria-label"));
        });
    }

    [Fact]
    public void MainLayout_SaveFileAction_RendersOnEditorScreen_AndTriggersCoordinator()
    {
        _ = TestHarnessFactory.Create(this);
        var navigation = Services.GetRequiredService<NavigationManager>();
        var saveCoordinator = Services.GetRequiredService<EditorDocumentSaveCoordinator>();
        var saveRequestCount = 0;

        saveCoordinator.Register(_ =>
        {
            saveRequestCount += 1;
            return Task.CompletedTask;
        });

        navigation.NavigateTo(AppRoutes.EditorWithId(AppTestData.Scripts.QuantumId));

        var cut = Render<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        cut.WaitForAssertion(() =>
        {
            var exportAction = cut.FindByTestId(UiTestIds.Header.EditorSaveFile);

            Assert.NotNull(exportAction);
            Assert.Contains(EnglishExportLabel, exportAction.TextContent, StringComparison.Ordinal);
            Assert.NotNull(cut.FindByTestId(UiTestIds.Header.EditorLearn));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Header.EditorRead));
        });

        cut.FindByTestId(UiTestIds.Header.EditorSaveFile).Click();

        Assert.Equal(1, saveRequestCount);
    }

    [Theory]
    [InlineData(AppRoutes.Library)]
    [InlineData(AppRoutes.Settings)]
    public void MainLayout_SaveFileAction_IsHidden_OnNonEditorScreens(string route)
    {
        _ = TestHarnessFactory.Create(this);
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(route);

        var cut = Render<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        cut.WaitForAssertion(() =>
            Assert.Empty(cut.FindAll(BunitTestSelectors.BuildTestIdSelector(UiTestIds.Header.EditorSaveFile))));
    }

    [Fact]
    public void MainLayout_ActiveGenericGoLiveSession_UsesPlainGoLiveRoute_InsteadOfCurrentEditorScriptScope()
    {
        _ = TestHarnessFactory.Create(this);
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(AppRoutes.EditorWithId(AppTestData.Scripts.DemoId));
        Services.GetRequiredService<GoLiveSessionService>().SetState(new GoLiveSessionState(
            ScriptId: string.Empty,
            ScriptTitle: string.Empty,
            ScriptSubtitle: string.Empty,
            SelectedSourceId: AppTestData.Camera.FirstSourceId,
            SelectedSourceLabel: AppTestData.Camera.FrontCamera,
            ActiveSourceId: AppTestData.Camera.FirstSourceId,
            ActiveSourceLabel: AppTestData.Camera.FrontCamera,
            PrimaryMicrophoneLabel: AppTestData.Scripts.BroadcastMic,
            OutputResolution: StreamingResolutionPreset.FullHd1080p30,
            BitrateKbps: AppTestData.Streaming.BitrateKbps,
            IsStreamActive: true,
            IsRecordingActive: false,
            StreamStartedAt: DateTimeOffset.UtcNow.AddMinutes(-1),
            RecordingStartedAt: null));

        var cut = Render<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.Header.LiveWidget)));
        cut.FindByTestId(UiTestIds.Header.LiveWidget).Click();

        Assert.EndsWith(AppRoutes.GoLive, navigation.Uri, StringComparison.Ordinal);
        Assert.DoesNotContain(AppRoutes.ScriptIdQueryKey, navigation.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void MainLayout_LibraryImportAction_RendersLocalizedCopy_WhenCurrentCultureIsUkrainian()
    {
        using var cultureScope = new CultureScope(AppCultureCatalog.UkrainianCultureName);
        _ = TestHarnessFactory.Create(this);
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(AppRoutes.Library);

        var cut = Render<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        cut.WaitForAssertion(() =>
        {
            var importSurface = cut.FindByTestId(UiTestIds.Header.LibraryOpenScript);
            var importInput = cut.FindByTestId(UiTestIds.Header.LibraryOpenScriptInput);

            Assert.Contains(UkrainianImportLabel, importSurface.TextContent, StringComparison.Ordinal);
            Assert.Equal(UkrainianImportLabel, importInput.GetAttribute("aria-label"));
        });
    }

    [Fact]
    public void MainLayout_EditorExportAction_RendersLocalizedCopy_WhenCurrentCultureIsUkrainian()
    {
        using var cultureScope = new CultureScope(AppCultureCatalog.UkrainianCultureName);
        _ = TestHarnessFactory.Create(this);
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(AppRoutes.EditorWithId(AppTestData.Scripts.QuantumId));

        var cut = Render<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        cut.WaitForAssertion(() =>
        {
            var exportAction = cut.FindByTestId(UiTestIds.Header.EditorSaveFile);

            Assert.Contains(UkrainianExportLabel, exportAction.TextContent, StringComparison.Ordinal);
        });
    }
}
