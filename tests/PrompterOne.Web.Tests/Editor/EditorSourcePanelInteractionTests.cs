using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using PrompterOne.Core.Models.Editor;
using PrompterOne.Shared.Components.Editor;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Settings.Models;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class EditorSourcePanelInteractionTests : BunitContext
{
    private const string ConfiguredAiApiKey = "sk-test-openai";
    private const string ConfiguredAiModel = "gpt-4o";
    private readonly AppHarness _harness;

    public EditorSourcePanelInteractionTests()
    {
        _harness = TestHarnessFactory.Create(this);
    }

    [Test]
    public async Task EditorSourcePanel_FloatingToolbarStaysPinnedWhileSelectionRemainsActive()
    {
        var cut = Render<EditorSourcePanelHost>();
        var initialStyle = cut.FindByTestId(UiTestIds.Editor.FloatingBar).GetAttribute("style");

        await cut.InvokeAsync(cut.Instance.ApplyFormattedSelection);

        var updatedStyle = cut.FindByTestId(UiTestIds.Editor.FloatingBar).GetAttribute("style");

        Assert.Equal(initialStyle, updatedStyle);
    }

    [Test]
    public void EditorSourcePanel_TooltipButtonsUseCustomTooltipContractWithoutNativeTitle()
    {
        var cut = Render<EditorSourcePanelHost>();

        var emotionTrigger = cut.FindByTestId(UiTestIds.Editor.EmotionTrigger);
        AssertTooltipContract(emotionTrigger, Text(UiTextKey.EditorToolbarTooltipEmotionTrigger));

        emotionTrigger.Click();
        var motivationalEmotion = cut.FindByTestId(UiTestIds.Editor.EmotionMotivational);
        AssertTooltipContract(motivationalEmotion, Text(UiTextKey.EditorToolbarTooltipEmotionMotivational));
        emotionTrigger.Click();

        var floatingEmotionTrigger = cut.FindByTestId(UiTestIds.Editor.FloatingEmotion);
        AssertTooltipContract(floatingEmotionTrigger, Text(UiTextKey.EditorToolbarTooltipFloatingEmotionTrigger));

        floatingEmotionTrigger.Click();
        var floatingMotivationalEmotion = cut.FindByTestId(UiTestIds.Editor.FloatingEmotionMotivational);
        AssertTooltipContract(floatingMotivationalEmotion, Text(UiTextKey.EditorToolbarTooltipEmotionMotivational));
    }

    [Test]
    public void EditorSourcePanel_TooltipSurface_IsRenderedWithExpectedContent()
    {
        var cut = Render<EditorSourcePanelHost>();

        var tooltips = cut.FindAll(BunitTestSelectors.BuildTestIdSelector(UiTestIds.Editor.ToolbarTooltip));

        Assert.Contains(
            tooltips,
            tooltip => string.Equals(
                tooltip.TextContent.Trim(),
                Text(UiTextKey.EditorToolbarTooltipEmotionTrigger),
                StringComparison.Ordinal));
        Assert.Contains(
            tooltips,
            tooltip => string.Equals(
                tooltip.TextContent.Trim(),
                Text(UiTextKey.EditorToolbarTooltipEmotionMotivational),
                StringComparison.Ordinal));
    }

    [Test]
    public void EditorSourcePanel_TooltipSurface_UsesStableTooltipRoleAndPlacementContract()
    {
        var cut = Render<EditorSourcePanelHost>();
        var toolbarTooltip = cut.FindAll(BunitTestSelectors.BuildTestIdSelector(UiTestIds.Editor.ToolbarTooltip))
            .Single(tooltip => string.Equals(
                tooltip.TextContent.Trim(),
                Text(UiTextKey.EditorToolbarTooltipEmotionTrigger),
                StringComparison.Ordinal));

        Assert.Equal("tooltip", toolbarTooltip.GetAttribute("role"));
        Assert.Equal("toolbar", toolbarTooltip.GetAttribute("data-tooltip-placement"));
    }

    [Test]
    public void EditorSourcePanel_FloatingToolbarMenusExposeExtendedTpsAuthoringOptions()
    {
        var cut = Render<EditorSourcePanelHost>();

        var floatingVoiceTrigger = cut.FindByTestId(UiTestIds.Editor.FloatingVoice);
        AssertTooltipContract(floatingVoiceTrigger, Text(UiTextKey.EditorToolbarTooltipFloatingVoiceTrigger));
        floatingVoiceTrigger.Click();
        Assert.NotNull(cut.FindByTestId(UiTestIds.Editor.FloatingVoiceMenu));
        AssertTooltipContract(
            cut.FindByTestId(UiTestIds.Editor.FloatingVoiceWhisper),
            Text(UiTextKey.EditorToolbarTooltipFloatingVoiceWhisper));
        AssertTooltipContract(
            cut.FindByTestId(UiTestIds.Editor.FloatingVoiceLegato),
            Text(UiTextKey.EditorToolbarTooltipFloatingVoiceLegato));
        AssertTooltipContract(
            cut.FindByTestId(UiTestIds.Editor.FloatingVoiceEnergy),
            Text(UiTextKey.EditorToolbarTooltipFloatingVoiceEnergy));

        var floatingPauseTrigger = cut.FindByTestId(UiTestIds.Editor.FloatingPauseTrigger);
        AssertTooltipContract(floatingPauseTrigger, Text(UiTextKey.EditorToolbarTooltipFloatingPauseTrigger));
        floatingPauseTrigger.Click();
        Assert.NotNull(cut.FindByTestId(UiTestIds.Editor.FloatingPauseMenu));
        AssertTooltipContract(
            cut.FindByTestId(UiTestIds.Editor.FloatingPauseTimed),
            Text(UiTextKey.EditorToolbarTooltipFloatingPauseTimed));

        var floatingSpeedTrigger = cut.FindByTestId(UiTestIds.Editor.FloatingSpeedTrigger);
        AssertTooltipContract(floatingSpeedTrigger, Text(UiTextKey.EditorToolbarTooltipFloatingSpeedTrigger));
        floatingSpeedTrigger.Click();
        Assert.NotNull(cut.FindByTestId(UiTestIds.Editor.FloatingSpeedMenu));
        AssertTooltipContract(
            cut.FindByTestId(UiTestIds.Editor.FloatingSpeedCustomWpm),
            Text(UiTextKey.EditorToolbarTooltipFloatingSpeedCustomWpm));

        var floatingInsertTrigger = cut.FindByTestId(UiTestIds.Editor.FloatingInsert);
        AssertTooltipContract(floatingInsertTrigger, Text(UiTextKey.EditorToolbarTooltipFloatingInsertTrigger));
        floatingInsertTrigger.Click();
        Assert.NotNull(cut.FindByTestId(UiTestIds.Editor.FloatingInsertMenu));
        AssertTooltipContract(
            cut.FindByTestId(UiTestIds.Editor.FloatingInsertPronunciation),
            Text(UiTextKey.EditorToolbarTooltipFloatingInsertPronunciation));
        Assert.NotNull(cut.FindByTestId(UiTestIds.Editor.FloatingInsertSegmentMenu));
        Assert.NotNull(cut.FindByTestId(UiTestIds.Editor.FloatingInsertSegmentArchetypeMenu));
        Assert.NotNull(cut.FindByTestId(UiTestIds.Editor.FloatingInsertBlockMenu));
        Assert.NotNull(cut.FindByTestId(UiTestIds.Editor.FloatingInsertBlockArchetypeMenu));

        var insertTrigger = cut.FindByTestId(UiTestIds.Editor.InsertTrigger);
        AssertTooltipContract(insertTrigger, Text(UiTextKey.EditorToolbarTooltipMoreInsertOptions));
        insertTrigger.Click();
        Assert.NotNull(cut.FindByTestId(UiTestIds.Editor.MenuInsert));
        AssertTooltipContract(
            cut.FindByTestId(UiTestIds.Editor.InsertSegmentArchetypeMenu),
            Text(UiTextKey.EditorToolbarTooltipInsertSegmentArchetype));
        AssertTooltipContract(
            cut.FindByTestId(UiTestIds.Editor.InsertBlockArchetypeMenu),
            Text(UiTextKey.EditorToolbarTooltipInsertBlockArchetype));
    }

    [Test]
    public void EditorSourcePanel_DropdownRows_RenderSharedLeftAlignedContentClusters()
    {
        var cut = Render<EditorSourcePanelHost>();

        cut.FindByTestId(UiTestIds.Editor.ColorTrigger).Click();
        AssertMenuActionCluster(cut.FindByTestId(UiTestIds.Editor.ColorEnergy), "Energy", "[energy:8]");
        cut.FindByTestId(UiTestIds.Editor.ColorTrigger).Click();

        cut.FindByTestId(UiTestIds.Editor.FloatingVoice).Click();
        AssertMenuActionCluster(cut.FindByTestId(UiTestIds.Editor.FloatingVoiceEnergy), "Energy", "[energy:8]");
        cut.FindByTestId(UiTestIds.Editor.FloatingInsert).Click();
        AssertMenuActionCluster(cut.FindByTestId(UiTestIds.Editor.FloatingInsertSegmentArchetypeMenu), "Segment", "Archetype aware");
    }

    [Test]
    public void EditorSourcePanel_AiButtonsAreDisabled_WhenNoProviderIsConfigured()
    {
        var cut = Render<EditorSourcePanelHost>();

        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.FindByTestId(UiTestIds.Editor.Ai).HasAttribute("disabled"));
            Assert.True(cut.FindByTestId(UiTestIds.Editor.FloatingAi).HasAttribute("disabled"));
        });
    }

    [Test]
    public void EditorSourcePanel_AiButtonsAreEnabled_WhenAProviderIsConfigured()
    {
        _harness.JsRuntime.SavedValues[AiProviderSettings.StorageKey] = new AiProviderSettings
        {
            OpenAi = new OpenAiProviderSettings
            {
                ApiKey = ConfiguredAiApiKey,
                Model = ConfiguredAiModel
            }
        };

        var cut = Render<EditorSourcePanelHost>();

        cut.WaitForAssertion(() =>
        {
            Assert.False(cut.FindByTestId(UiTestIds.Editor.Ai).HasAttribute("disabled"));
            Assert.False(cut.FindByTestId(UiTestIds.Editor.FloatingAi).HasAttribute("disabled"));
        });
    }

    private static void AssertTooltipContract(IElement element, string expectedTooltip)
    {
        Assert.Null(element.GetAttribute("title"));
        Assert.Equal(expectedTooltip, element.GetAttribute("data-tip"));
        Assert.Equal(expectedTooltip, element.GetAttribute("aria-label"));
    }

    private static void AssertMenuActionCluster(IElement action, string expectedLabel, string expectedMeta)
    {
        var actionTestId = action.GetAttribute("data-test");
        Assert.False(string.IsNullOrWhiteSpace(actionTestId));

        _ = action.QuerySelector(
            BunitTestSelectors.BuildTestIdSelector(
                UiTestIds.Editor.ToolbarActionLeading(actionTestId!)));
        var label = action.QuerySelector(
            BunitTestSelectors.BuildTestIdSelector(
                UiTestIds.Editor.ToolbarActionLabel(actionTestId!)));
        var meta = action.QuerySelector(
            BunitTestSelectors.BuildTestIdSelector(
                UiTestIds.Editor.ToolbarActionMeta(actionTestId!)));

        Assert.NotNull(label);
        Assert.NotNull(meta);
        Assert.Equal(expectedLabel, label!.TextContent.Trim());
        Assert.Equal(expectedMeta, meta!.TextContent.Trim());
    }

    private string Text(UiTextKey key) =>
        Services.GetRequiredService<IStringLocalizer<SharedResource>>()[key.ToString()];

    private sealed class EditorSourcePanelHost : ComponentBase
    {
        private string _text = EditorSourcePanelInteractionTestSource.SourceText;
        private EditorSelectionViewModel _selection = EditorSourcePanelInteractionTestSource.InitialSelection;

        public Task ApplyFormattedSelection()
        {
            _text = EditorSourcePanelInteractionTestSource.FormattedSourceText;
            _selection = EditorSourcePanelInteractionTestSource.UpdatedSelection;
            StateHasChanged();
            return Task.CompletedTask;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<EditorSourcePanel>(0);
            builder.AddAttribute(1, nameof(EditorSourcePanel.Text), _text);
            builder.AddAttribute(2, nameof(EditorSourcePanel.Selection), _selection);
            builder.AddAttribute(3, nameof(EditorSourcePanel.Status), EditorSourcePanelInteractionTestSource.Status);
            builder.CloseComponent();
        }
    }

    private static class EditorSourcePanelInteractionTestSource
    {
        public const int BaseWpm = 140;
        public const int BlockCount = 1;
        public const int ColumnNumber = 1;
        public const string Duration = "0:10";
        public const string FormattedSourceText = "Alpha [emphasis]welcome[/emphasis] omega";
        public const double InitialToolbarLeft = 610;
        public const double InitialToolbarTop = 226;
        public const int LineNumber = 1;
        public const string Profile = "Actor";
        public const int SegmentCount = 1;
        public const int SelectionEnd = 13;
        public const int SelectionEndAfterFormatting = 24;
        public const int SelectionStart = 7;
        public const int SelectionStartAfterFormatting = 18;
        public const string SourceText = "Alpha welcome omega";
        public const double UpdatedToolbarLeft = 707;
        public const double UpdatedToolbarTop = 226;
        public const string Version = "1.0";
        public const int WordCount = 3;

        public static EditorSelectionViewModel InitialSelection { get; } = new(
            new EditorSelectionRange(SelectionStart, SelectionEnd),
            Line: LineNumber,
            Column: ColumnNumber,
            ToolbarTop: InitialToolbarTop,
            ToolbarLeft: InitialToolbarLeft);

        public static EditorStatusViewModel Status { get; } = new(
            LineNumber,
            ColumnNumber,
            Profile,
            BaseWpm,
            SegmentCount,
            BlockCount,
            WordCount,
            Duration,
            Version);

        public static EditorSelectionViewModel UpdatedSelection { get; } = new(
            new EditorSelectionRange(SelectionStartAfterFormatting, SelectionEndAfterFormatting),
            Line: LineNumber,
            Column: ColumnNumber,
            ToolbarTop: UpdatedToolbarTop,
            ToolbarLeft: UpdatedToolbarLeft);
    }
}
