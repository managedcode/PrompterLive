using System.Text.RegularExpressions;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[System.Obsolete]

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed partial class EditorInteractionTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
    public async Task EditorScreen_ShowsFloatingBarAndAppliesFormattingToSelectedSourceText()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await EditorIsolatedDraftDriver.CreateSeededDraftAsync(page, BrowserTestConstants.Scripts.DemoId);
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync();
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await EditorMonacoDriver.SetSelectionByTextAsync(page, BrowserTestConstants.Editor.Welcome);

            await Expect(page.GetByTestId(UiTestIds.Editor.FloatingBar)).ToBeVisibleAsync();
            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Editor.FloatEmphasis),
                noWaitAfter: true);

            var value = await EditorMonacoDriver.SourceInput(page).InputValueAsync();
            await Assert.That(value).Contains(BrowserTestConstants.Editor.EmphasisFragment);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceHighlight)).ToContainTextAsync(BrowserTestConstants.Editor.EmphasisFragment);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_UndoAndRedoWorkFromToolbarAndKeyboard()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await EditorIsolatedDraftDriver.CreateSeededDraftAsync(page, BrowserTestConstants.Scripts.DemoId);
            var sourceInput = EditorMonacoDriver.SourceInput(page);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);

            var initialValue = await sourceInput.InputValueAsync();

            await EditorMonacoDriver.SetCaretAtEndAsync(page);
            await UiInteractionDriver.ClickAndWaitForVisibleAsync(
                page.GetByTestId(UiTestIds.Editor.PauseTrigger),
                page.GetByTestId(UiTestIds.Editor.MenuPause),
                noWaitAfter: true);
            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Editor.PauseTwoSeconds),
                noWaitAfter: true);

            await Expect(sourceInput).ToHaveValueAsync(BrowserTestConstants.Regexes.EndsWithPause);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Editor.Undo),
                noWaitAfter: true);
            await Expect(sourceInput).ToHaveValueAsync(initialValue);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Editor.Redo),
                noWaitAfter: true);
            await Expect(sourceInput).ToHaveValueAsync(BrowserTestConstants.Regexes.EndsWithPause);

            await EditorMonacoDriver.ClickAsync(page);
            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.Undo);
            await Expect(sourceInput).ToHaveValueAsync(initialValue);

            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.Redo);
            await Expect(sourceInput).ToHaveValueAsync(BrowserTestConstants.Regexes.EndsWithPause);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_FloatingToolbarPersistsSelectionFormatting()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await EditorIsolatedDraftDriver.CreateSeededDraftAsync(page, BrowserTestConstants.Scripts.DemoId);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await EditorMonacoDriver.SetSelectionByTextAsync(page, BrowserTestConstants.Editor.Welcome);

            await Expect(page.GetByTestId(UiTestIds.Editor.FloatingBar)).ToBeVisibleAsync();

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Editor.FloatEmphasis),
                noWaitAfter: true);
            await Expect(EditorMonacoDriver.SourceInput(page)).ToHaveValueAsync(
                new Regex(Regex.Escape(BrowserTestConstants.Editor.EmphasisFragment)));

            await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.PersistDelayMs);
            await page.ReloadAsync();
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await Expect(EditorMonacoDriver.SourceInput(page)).ToHaveValueAsync(
                new Regex(Regex.Escape(BrowserTestConstants.Editor.EmphasisFragment)));
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_FloatingEmotionMenuAppliesSelectedEmotion()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await EditorIsolatedDraftDriver.CreateSeededDraftAsync(page, BrowserTestConstants.Scripts.DemoId);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await EditorMonacoDriver.SetSelectionByTextAsync(page, BrowserTestConstants.Editor.TransformativeMoment);

            await Expect(page.GetByTestId(UiTestIds.Editor.FloatingBar)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Editor.FloatingEmotion).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.FloatingEmotionMenu)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Editor.FloatingEmotionProfessional).ClickAsync();

            await Expect(EditorMonacoDriver.SourceInput(page)).ToHaveValueAsync(
                new Regex(Regex.Escape(BrowserTestConstants.Editor.ProfessionalFragment)));
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_FloatingMenusExposeExpandedTpsSurface()
    {
        const string scenarioName = "editor-floating-tps-surface";
        var page = await _fixture.NewPageAsync(additionalContext: true);
        UiScenarioArtifacts.ResetScenario(scenarioName);

        try
        {
            await EditorIsolatedDraftDriver.CreateSeededDraftAsync(page, BrowserTestConstants.Scripts.DemoId);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await EditorMonacoDriver.SetSelectionByTextAsync(page, BrowserTestConstants.Editor.TransformativeMoment);

            await Expect(page.GetByTestId(UiTestIds.Editor.FloatingBar)).ToBeVisibleAsync();

            await OpenFloatingMenuAsync(page, UiTestIds.Editor.FloatingVoice, UiTestIds.Editor.FloatingVoiceMenu);
            await OpenFloatingMenuAsync(page, UiTestIds.Editor.FloatingEmotion, UiTestIds.Editor.FloatingEmotionMenu);
            await OpenFloatingMenuAsync(page, UiTestIds.Editor.FloatingPauseTrigger, UiTestIds.Editor.FloatingPauseMenu);
            await OpenFloatingMenuAsync(page, UiTestIds.Editor.FloatingSpeedTrigger, UiTestIds.Editor.FloatingSpeedMenu);
            await OpenFloatingMenuAsync(page, UiTestIds.Editor.FloatingInsert, UiTestIds.Editor.FloatingInsertMenu);
            await UiScenarioArtifacts.CapturePageAsync(page, scenarioName, "01-floating-menu-expanded");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_FloatingVoiceMenuAppliesWhisperCue()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await EditorIsolatedDraftDriver.CreateSeededDraftAsync(page, BrowserTestConstants.Scripts.DemoId);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await EditorMonacoDriver.SetSelectionByTextAsync(page, BrowserTestConstants.Editor.OurCompany);

            await Expect(page.GetByTestId(UiTestIds.Editor.FloatingBar)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Editor.FloatingVoice).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.FloatingVoiceMenu)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Editor.FloatingVoiceWhisper).ClickAsync();

            await Expect(EditorMonacoDriver.SourceInput(page)).ToHaveValueAsync(
                new Regex(Regex.Escape(BrowserTestConstants.Editor.WhisperCompanyFragment)));
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_FloatingEmotionMenuAppliesDeliveryModeCue()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await EditorIsolatedDraftDriver.CreateSeededDraftAsync(page, BrowserTestConstants.Scripts.DemoId);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await EditorMonacoDriver.SetSelectionByTextAsync(page, BrowserTestConstants.Editor.TransformativeMoment);

            await Expect(page.GetByTestId(UiTestIds.Editor.FloatingBar)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Editor.FloatingEmotion).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.FloatingEmotionMenu)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Editor.FloatingDeliverySarcasm).ClickAsync();

            await Expect(EditorMonacoDriver.SourceInput(page)).ToHaveValueAsync(
                new Regex(Regex.Escape(BrowserTestConstants.Editor.SarcasmMomentFragment)));
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_FloatingPauseSpeedAndInsertMenusApplyExtendedTpsCues()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await EditorIsolatedDraftDriver.CreateSeededDraftAsync(page, BrowserTestConstants.Scripts.DemoId);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await EditorMonacoDriver.SetSelectionByTextAsync(page, BrowserTestConstants.Editor.TransformativeMoment);

            await Expect(page.GetByTestId(UiTestIds.Editor.FloatingBar)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Editor.FloatingPauseTrigger).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.FloatingPauseMenu)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Editor.FloatingPauseTimed).ClickAsync();
            await Expect(EditorMonacoDriver.SourceInput(page)).ToHaveValueAsync(
                new Regex(Regex.Escape(BrowserTestConstants.Editor.TimedPauseFragment)));

            await EditorMonacoDriver.SetSelectionByTextAsync(page, BrowserTestConstants.Editor.OurCompany);
            await page.GetByTestId(UiTestIds.Editor.FloatingSpeedTrigger).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.FloatingSpeedMenu)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Editor.FloatingSpeedCustomWpm).ClickAsync();
            await Expect(EditorMonacoDriver.SourceInput(page)).ToHaveValueAsync(
                new Regex(Regex.Escape(BrowserTestConstants.Editor.CustomWpmCompanyFragment)));

            await EditorMonacoDriver.SetSelectionByTextAsync(page, BrowserTestConstants.Editor.OurCompany);
            await page.GetByTestId(UiTestIds.Editor.FloatingInsert).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.FloatingInsertMenu)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Editor.FloatingInsertPronunciation).ClickAsync();
            await Expect(EditorMonacoDriver.SourceInput(page)).ToHaveValueAsync(
                new Regex(Regex.Escape(BrowserTestConstants.Editor.PronunciationCompanyFragment)));
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_DoesNotRenderLegacyStructureInspectorPanel()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await EditorIsolatedDraftDriver.CreateSeededDraftAsync(page, BrowserTestConstants.Scripts.QuantumId);
            await Expect(page.GetByTestId(UiTestIds.Editor.ActiveSegmentName)).ToHaveCountAsync(0);
            await Expect(page.GetByTestId(UiTestIds.Editor.ActiveBlockName)).ToHaveCountAsync(0);
            await Expect(page.GetByTestId(UiTestIds.Editor.SegmentNavigation(0))).ToContainTextAsync("Introduction");
            await Expect(page.GetByTestId(UiTestIds.Editor.BlockNavigation(0, 0))).ToContainTextAsync("Overview Block");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_HidesFrontMatterFromVisibleEditorBody()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await EditorIsolatedDraftDriver.CreateSeededDraftAsync(page, BrowserTestConstants.Scripts.DemoId);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);

            var visibleSource = await EditorMonacoDriver.SourceInput(page).InputValueAsync();
            await Assert.That(visibleSource).DoesNotContain("---");
            await Assert.That(visibleSource).DoesNotContain("title:");
            await Assert.That(visibleSource).DoesNotContain("author:");
            await Assert.That(visibleSource).Contains("## [Intro|140WPM|warm]");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_MetadataDurationPersistsAfterReload()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await EditorIsolatedDraftDriver.CreateSeededDraftAsync(page, BrowserTestConstants.Scripts.DemoId);
            await Expect(page.GetByTestId(UiTestIds.Editor.Duration)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.Editor.Duration).EvaluateAsync(
                """
                (element, value) => {
                    element.focus();
                    element.value = value;
                    element.dispatchEvent(new Event("change", { bubbles: true }));
                }
                """,
                BrowserTestConstants.Editor.DisplayDuration);

            await Expect(page.GetByTestId(UiTestIds.Editor.Duration)).ToHaveValueAsync(BrowserTestConstants.Editor.DisplayDuration);
            await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.PersistReloadDelayMs);
            await page.ReloadAsync();
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await Expect(page.GetByTestId(UiTestIds.Editor.Duration)).ToHaveValueAsync(BrowserTestConstants.Editor.DisplayDuration);

            var visibleSource = await EditorMonacoDriver.SourceInput(page).InputValueAsync();
            await Assert.That(visibleSource).DoesNotContain(BrowserTestConstants.Editor.DurationField);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_ClickableMenusAndAiButtonsApplyCommands()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await EditorIsolatedDraftDriver.CreateSeededDraftAsync(page, BrowserTestConstants.Scripts.DemoId);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await EditorMonacoDriver.SetSelectionByTextAsync(page, BrowserTestConstants.Editor.Welcome);

            await UiInteractionDriver.ClickAndWaitForVisibleAsync(
                page.GetByTestId(UiTestIds.Editor.FormatTrigger),
                page.GetByTestId(UiTestIds.Editor.MenuFormat),
                noWaitAfter: true);
            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Editor.FormatHighlight),
                noWaitAfter: true);
            await Expect(EditorMonacoDriver.SourceInput(page)).ToHaveValueAsync(
                new Regex(Regex.Escape(BrowserTestConstants.Editor.HighlightFragment)));

            await UiInteractionDriver.ClickAndWaitForVisibleAsync(
                page.GetByTestId(UiTestIds.Editor.PauseTrigger),
                page.GetByTestId(UiTestIds.Editor.MenuPause),
                noWaitAfter: true);
            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Editor.PauseTwoSeconds),
                noWaitAfter: true);
            await Expect(EditorMonacoDriver.SourceInput(page)).ToHaveValueAsync(new Regex(Regex.Escape(BrowserTestConstants.Editor.PauseFragment)));

            await EditorMonacoDriver.SetCaretAtEndAsync(page);

            await UiInteractionDriver.ClickAndWaitForVisibleAsync(
                page.GetByTestId(UiTestIds.Editor.InsertTrigger),
                page.GetByTestId(UiTestIds.Editor.MenuInsert),
                noWaitAfter: true);
            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Editor.InsertBlockMenu),
                noWaitAfter: true);
            await Expect(EditorMonacoDriver.SourceInput(page)).ToHaveValueAsync(
                new Regex(Regex.Escape(BrowserTestConstants.Editor.StructureBlockToken)));

            await Expect(page.GetByTestId(UiTestIds.Header.AiSpotlight)).ToBeVisibleAsync();
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_TopToolbarShowsVisibleStructureButtons()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await EditorIsolatedDraftDriver.CreateDraftAsync(page, BrowserTestConstants.Editor.Welcome);
            var sourceInput = EditorMonacoDriver.SourceInput(page);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await Expect(page.GetByTestId(UiTestIds.Editor.InsertSegment)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.InsertBlock)).ToBeVisibleAsync();

            await EditorMonacoDriver.SetCaretAtEndAsync(page);
            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Editor.InsertSegment),
                noWaitAfter: true);
            await Expect(sourceInput).ToHaveValueAsync(new Regex(Regex.Escape(BrowserTestConstants.Editor.StructureSegmentToken)));

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Editor.InsertBlock),
                noWaitAfter: true);
            await Expect(sourceInput).ToHaveValueAsync(new Regex(Regex.Escape(BrowserTestConstants.Editor.StructureBlockToken)));
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_FullToolbarSurfaceSupportsExtendedCommands()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await EditorIsolatedDraftDriver.CreateSeededDraftAsync(page, BrowserTestConstants.Scripts.DemoId);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await EditorMonacoDriver.SetSelectionByTextAsync(page, BrowserTestConstants.Editor.Welcome);

            await page.GetByTestId(UiTestIds.Editor.ColorTrigger).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.MenuColor)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Editor.ColorGreen).ClickAsync();
            await Expect(EditorMonacoDriver.SourceInput(page)).ToHaveValueAsync(
                new Regex(Regex.Escape(BrowserTestConstants.Editor.LoudFragment)));

            await EditorMonacoDriver.SetSelectionByTextAsync(page, BrowserTestConstants.Editor.LoudFragment);

            await page.GetByTestId(UiTestIds.Editor.ColorTrigger).ClickAsync();
            await page.GetByTestId(UiTestIds.Editor.ColorClear).ClickAsync();
            await Expect(EditorMonacoDriver.SourceInput(page)).Not.ToHaveValueAsync(
                new Regex(Regex.Escape(BrowserTestConstants.Editor.LoudFragment)));

            await EditorMonacoDriver.SetSelectionByTextAsync(page, BrowserTestConstants.Editor.TransformativeMoment);

            await page.GetByTestId(UiTestIds.Editor.EmotionTrigger).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.MenuEmotion)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Editor.EmotionProfessional).ClickAsync();
            await Expect(EditorMonacoDriver.SourceInput(page)).ToHaveValueAsync(
                new Regex(Regex.Escape(BrowserTestConstants.Editor.ProfessionalFragment)));

            await page.GetByTestId(UiTestIds.Editor.SpeedTrigger).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.MenuSpeed)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Editor.SpeedCustomWpm).ClickAsync();
            await Expect(EditorMonacoDriver.SourceInput(page)).ToHaveValueAsync(
                new Regex(Regex.Escape(BrowserTestConstants.Editor.CustomWpmToken)));

            await page.GetByTestId(UiTestIds.Editor.InsertTrigger).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.MenuInsert)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Editor.InsertPronunciation).ClickAsync();
            await Expect(EditorMonacoDriver.SourceInput(page)).ToHaveValueAsync(
                new Regex(Regex.Escape(BrowserTestConstants.Editor.PronunciationToken)));
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_ToolbarDropdownsCloseCentrallyAcrossCommandsAndOutsideClicks()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await EditorIsolatedDraftDriver.CreateSeededDraftAsync(page, BrowserTestConstants.Scripts.DemoId);
            var sourceInput = EditorMonacoDriver.SourceInput(page);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await EditorMonacoDriver.SetSelectionByTextAsync(page, BrowserTestConstants.Editor.Welcome);

            var formatMenu = page.GetByTestId(UiTestIds.Editor.MenuFormat);
            var colorMenu = page.GetByTestId(UiTestIds.Editor.MenuColor);
            var pauseMenu = page.GetByTestId(UiTestIds.Editor.MenuPause);

            await page.GetByTestId(UiTestIds.Editor.FormatTrigger).ClickAsync();
            await Expect(formatMenu).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.Editor.ColorTrigger).ClickAsync();
            await Expect(colorMenu).ToBeVisibleAsync();
            await Expect(formatMenu).ToBeHiddenAsync();

            await page.GetByTestId(UiTestIds.Editor.PauseTrigger).ClickAsync();
            await Expect(pauseMenu).ToBeVisibleAsync();
            await Expect(colorMenu).ToBeHiddenAsync();

            await page.GetByTestId(UiTestIds.Editor.PauseTwoSeconds).ClickAsync();
            await Expect(pauseMenu).ToBeHiddenAsync();
            await Expect(sourceInput).ToHaveValueAsync(new Regex(Regex.Escape(BrowserTestConstants.Editor.PauseFragment)));

            await page.GetByTestId(UiTestIds.Editor.ColorTrigger).ClickAsync();
            await Expect(colorMenu).ToBeVisibleAsync();

            await EditorMonacoDriver.ClickUncoveredStageAreaAsync(page);
            await Expect(colorMenu).ToBeHiddenAsync();
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
