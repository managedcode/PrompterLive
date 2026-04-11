using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Core.AI.Models;
using PrompterOne.Core.AI.Services;
using PrompterOne.Core.Localization;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Core.Services.Editor;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Layout;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

[NotInParallel]
public sealed class MainLayoutActionTests : BunitContext
{
    private const string DisabledStateValue = "false";
    private const string EnabledStateValue = "true";
    private const string EnglishExportLabel = "Export";
    private const string EnglishAiSpotlightLabel = "AI Assistant";
    private const string EnglishGoLiveLabel = "Go Live";
    private const string EnglishImportLabel = "Import";
    private const string IntroSubtitle = "Intro";
    private const string UkrainianExportLabel = "Експорт";
    private const string UkrainianImportLabel = "Імпорт";
    private static readonly string SupportedImportAcceptValue = ScriptDocumentFileTypes.PickerAcceptValue;

    [Test]
    [Arguments(AppRoutes.Learn, AppTestData.Scripts.QuantumId)]
    [Arguments(AppRoutes.Teleprompter, AppTestData.Scripts.QuantumId)]
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

    [Test]
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

    [Test]
    [Arguments(AppRoutes.Library)]
    [Arguments(AppRoutes.Settings)]
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

    [Test]
    [Arguments(AppRoutes.Library)]
    [Arguments(AppRoutes.Editor)]
    [Arguments(AppRoutes.Settings)]
    public void MainLayout_RendersGlobalAiSpotlightAction_OnSharedHeaderScreens(string route)
    {
        _ = TestHarnessFactory.Create(this);
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(route);

        var cut = Render<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        cut.WaitForAssertion(() =>
        {
            var spotlight = cut.FindByTestId(UiTestIds.Header.AiSpotlight);

            Assert.NotNull(spotlight.QuerySelector(BunitTestSelectors.BuildTestIdSelector(UiTestIds.Header.AiSpotlightIcon)));
            Assert.Equal(EnglishAiSpotlightLabel, spotlight.GetAttribute("aria-label"));
        });
    }

    [Test]
    public void MainLayout_GlobalAiSpotlightAction_OpensOverlay()
    {
        _ = TestHarnessFactory.Create(this);
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(AppRoutes.EditorWithId(AppTestData.Scripts.QuantumId));

        var cut = Render<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        cut.FindByTestId(UiTestIds.Header.AiSpotlight).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindByTestId(UiTestIds.AiSpotlight.Overlay));
            Assert.NotNull(cut.FindByTestId(UiTestIds.AiSpotlight.Surface));
            Assert.NotNull(cut.FindByTestId(UiTestIds.AiSpotlight.PromptInput));
        });
    }

    [Test]
    public void MainLayout_GlobalAiSpotlightApproval_AppliesOnlySelectedRange()
    {
        _ = TestHarnessFactory.Create(this);
        var navigation = Services.GetRequiredService<NavigationManager>();
        var spotlight = Services.GetRequiredService<AiSpotlightService>();
        var editService = Services.GetRequiredService<ScriptDocumentEditService>();
        var sourceText = "Keep before. Rewrite me. Keep after.";
        var selectedText = "Rewrite me.";
        var selectedStart = sourceText.IndexOf(selectedText, StringComparison.Ordinal);
        var selectedRange = new ScriptDocumentRange(selectedStart, selectedStart + selectedText.Length);
        using var editRegistration = spotlight.RegisterDocumentEditTarget(plan =>
        {
            var result = editService.Apply(sourceText, plan);
            sourceText = result.Text;
            return Task.FromResult(result);
        });

        spotlight.SetContext(new ScriptArticleContext(
            Title: "Draft",
            Content: sourceText,
            Source: "PrompterOne Editor",
            Route: AppRoutes.Editor,
            Screen: AppShellScreen.Editor.ToString(),
            Editor: new ScriptEditorContext(
                DocumentId: "draft",
                DocumentTitle: "Draft",
                Content: sourceText,
                Revision: ScriptDocumentRevision.Create(sourceText),
                Cursor: ScriptDocumentPosition.FromOffset(sourceText, selectedStart),
                SelectedRange: selectedRange,
                SelectedText: selectedText,
                SelectedLineNumbers: [1])));

        navigation.NavigateTo(AppRoutes.EditorWithId(AppTestData.Scripts.QuantumId));
        var cut = Render<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        cut.FindByTestId(UiTestIds.Header.AiSpotlight).Click();
        cut.FindByTestId(UiTestIds.AiSpotlight.PromptInput).Input("rewrite this selected paragraph");
        cut.FindByTestId(UiTestIds.AiSpotlight.Submit).Click();
        cut.FindByTestId(UiTestIds.AiSpotlight.RunPlan).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(selectedText, cut.FindByTestId(UiTestIds.AiSpotlight.ApprovalBefore).TextContent, StringComparison.Ordinal);
            Assert.Contains("[warm]Rewrite me.[/warm]", cut.FindByTestId(UiTestIds.AiSpotlight.ApprovalAfter).TextContent, StringComparison.Ordinal);
        });

        cut.FindByTestId(UiTestIds.AiSpotlight.ApprovalApprove).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("Keep before. [warm]Rewrite me.[/warm] Keep after.", sourceText);
            Assert.DoesNotContain("[warm]Keep before", sourceText, StringComparison.Ordinal);
        });
    }

    [Test]
    public void MainLayout_GlobalAiSpotlightRouteContext_ClearsStaleEditorContextOutsideEditor()
    {
        _ = TestHarnessFactory.Create(this);
        var spotlight = Services.GetRequiredService<AiSpotlightService>();
        var sourceText = "Selected";

        spotlight.SetContext(new ScriptArticleContext(
            Title: "Draft",
            Content: sourceText,
            Source: "PrompterOne Editor",
            Route: AppRoutes.Editor,
            Screen: AppShellScreen.Editor.ToString(),
            Editor: new ScriptEditorContext(
                DocumentId: "draft",
                DocumentTitle: "Draft",
                Content: sourceText,
                Revision: ScriptDocumentRevision.Create(sourceText),
                SelectedRange: new ScriptDocumentRange(0, sourceText.Length),
                SelectedText: sourceText,
                SelectedLineNumbers: [1]),
            Graph: new ScriptKnowledgeGraphContext(ScriptDocumentRevision.Create(sourceText), 1, 0, ["Draft"])));

        spotlight.SetRouteContext(AppShellScreen.Settings, AppRoutes.Settings, "Settings");

        Assert.Null(spotlight.State.Context.Editor);
        Assert.Null(spotlight.State.Context.Graph);
        Assert.Equal(AppShellScreen.Settings.ToString(), spotlight.State.Context.Screen);
    }

    [Test]
    public void MainLayout_TeleprompterPlayback_MutesSharedHeaderChrome()
    {
        _ = TestHarnessFactory.Create(this);
        var navigation = Services.GetRequiredService<NavigationManager>();
        var shell = Services.GetRequiredService<AppShellService>();
        navigation.NavigateTo(AppRoutes.TeleprompterWithId(AppTestData.Scripts.QuantumId));

        var cut = Render<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        shell.ShowTeleprompter(AppTestData.Scripts.QuantumTitle, IntroSubtitle, AppTestData.Scripts.QuantumId);
        shell.SetTeleprompterPlaybackActive(true);

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(
                EnabledStateValue,
                cut.FindByTestId(UiTestIds.Header.Surface).GetAttribute("data-reader-muted"));
            Assert.Equal(
                EnabledStateValue,
                cut.FindByTestId(UiTestIds.Header.GoLive).GetAttribute("data-subdued"));
        });

        shell.SetTeleprompterPlaybackActive(false);

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(
                DisabledStateValue,
                cut.FindByTestId(UiTestIds.Header.Surface).GetAttribute("data-reader-muted"));
            Assert.Equal(
                DisabledStateValue,
                cut.FindByTestId(UiTestIds.Header.GoLive).GetAttribute("data-subdued"));
        });
    }

    [Test]
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
            var aiSpotlight = cut.FindByTestId(UiTestIds.Header.AiSpotlight);
            var openScript = cut.FindByTestId(UiTestIds.Header.LibraryOpenScript);
            var openScriptInput = cut.FindByTestId(UiTestIds.Header.LibraryOpenScriptInput);
            var newScript = cut.FindByTestId(UiTestIds.Header.LibraryNewScript);

            Assert.Equal(
                EnglishGoLiveLabel,
                goLive.QuerySelector(BunitTestSelectors.BuildTestIdSelector(UiTestIds.Header.GoLiveLabel))?.TextContent.Trim());
            Assert.NotNull(goLive.QuerySelector(BunitTestSelectors.BuildTestIdSelector(UiTestIds.Header.GoLiveDot)));
            Assert.Equal(
                UiTestIds.Header.LibraryNewScript,
                newScript.GetAttribute("data-test"));
            Assert.Equal(SupportedImportAcceptValue, openScriptInput.GetAttribute("accept"));
            Assert.Contains(EnglishImportLabel, openScript.TextContent, StringComparison.Ordinal);

            var goLiveIndex = cut.Markup.IndexOf(UiTestIds.Header.GoLive, StringComparison.Ordinal);
            var aiSpotlightIndex = cut.Markup.IndexOf(UiTestIds.Header.AiSpotlight, StringComparison.Ordinal);
            var openScriptIndex = cut.Markup.IndexOf(UiTestIds.Header.LibraryOpenScript, StringComparison.Ordinal);
            var newScriptIndex = cut.Markup.IndexOf(UiTestIds.Header.LibraryNewScript, StringComparison.Ordinal);
            Assert.True(goLiveIndex >= 0 && aiSpotlightIndex >= 0 && openScriptIndex >= 0 && newScriptIndex >= 0);
            Assert.True(goLiveIndex < aiSpotlightIndex);
            Assert.True(aiSpotlightIndex < openScriptIndex);
            Assert.True(openScriptIndex < newScriptIndex);
            Assert.NotNull(aiSpotlight);
            Assert.NotNull(openScript);
        });
    }

    [Test]
    public void MainLayout_LibraryHeader_UsesSharedBrandAndIconPrimitives()
    {
        _ = TestHarnessFactory.Create(this);
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(AppRoutes.Library);

        var cut = Render<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindByTestId(UiTestIds.Header.HomeBrandMark));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Header.GoLiveIcon));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Header.AiSpotlightIcon));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Header.LibraryOpenScriptIcon));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Header.LibraryNewScriptIcon));
        });
    }

    [Test]
    [Arguments(AppRoutes.Settings)]
    [Arguments(AppRoutes.Editor)]
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

    [Test]
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
            var openScriptButton = cut.FindByTestId(UiTestIds.Header.LibraryOpenScriptButton);
            var openScriptInput = cut.FindByTestId(UiTestIds.Header.LibraryOpenScriptInput);

            Assert.NotNull(openScriptButton);
            Assert.Equal("dialog", openScriptButton.GetAttribute("aria-haspopup"));
            Assert.Contains(EnglishImportLabel, openScriptButton.TextContent, StringComparison.Ordinal);
            Assert.Equal(UiDomIds.AppShell.LibraryOpenScriptInput, openScriptInput.GetAttribute("id"));
            Assert.Equal(SupportedImportAcceptValue, openScriptInput.GetAttribute("accept"));
            Assert.Equal(EnglishImportLabel, openScriptInput.GetAttribute("aria-label"));
            Assert.NotNull(openScriptSurface);
        });
    }

    [Test]
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
            var importAction = cut.FindByTestId(UiTestIds.Header.EditorImportScript);
            var importInput = cut.FindByTestId(UiTestIds.Header.EditorImportScriptInput);
            var exportAction = cut.FindByTestId(UiTestIds.Header.EditorSaveFile);

            Assert.NotNull(importAction);
            Assert.Contains(EnglishImportLabel, importAction.TextContent, StringComparison.Ordinal);
            Assert.Equal(UiDomIds.AppShell.EditorImportScriptInput, importInput.GetAttribute("id"));
            Assert.Equal(SupportedImportAcceptValue, importInput.GetAttribute("accept"));
            Assert.Equal(EnglishImportLabel, importInput.GetAttribute("aria-label"));
            Assert.NotNull(exportAction);
            Assert.Contains(EnglishExportLabel, exportAction.TextContent, StringComparison.Ordinal);
            Assert.NotNull(cut.FindByTestId(UiTestIds.Header.EditorLearn));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Header.EditorRead));
        });

        cut.FindByTestId(UiTestIds.Header.EditorSaveFile).Click();

        cut.WaitForAssertion(() => Assert.Equal(1, saveRequestCount));
    }

    [Test]
    [Arguments(AppRoutes.Library)]
    [Arguments(AppRoutes.Settings)]
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

    [Test]
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

        var goLiveUri = new Uri(navigation.Uri, UriKind.Absolute);
        Assert.Equal(AppRoutes.GoLive, goLiveUri.AbsolutePath);
        Assert.DoesNotContain(AppRoutes.ScriptIdQueryKey, goLiveUri.Query, StringComparison.Ordinal);
    }

    [Test]
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

    [Test]
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
            var importAction = cut.FindByTestId(UiTestIds.Header.EditorImportScript);
            var exportAction = cut.FindByTestId(UiTestIds.Header.EditorSaveFile);

            Assert.Contains(UkrainianImportLabel, importAction.TextContent, StringComparison.Ordinal);
            Assert.Contains(UkrainianExportLabel, exportAction.TextContent, StringComparison.Ordinal);
        });
    }
}
