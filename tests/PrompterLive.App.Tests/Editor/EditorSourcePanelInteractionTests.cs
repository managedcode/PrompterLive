using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using PrompterLive.Core.Models.Editor;
using PrompterLive.Shared.Components.Editor;
using PrompterLive.Shared.Contracts;
using PrompterLive.Shared.Tests;

namespace PrompterLive.App.Tests;

public sealed class EditorSourcePanelInteractionTests : BunitContext
{
    public EditorSourcePanelInteractionTests()
    {
        _ = TestHarnessFactory.Create(this);
    }

    [Fact]
    public async Task EditorSourcePanel_FloatingToolbarStaysPinnedWhileSelectionRemainsActive()
    {
        var cut = Render<EditorSourcePanelHost>();
        var initialStyle = cut.FindByTestId(UiTestIds.Editor.FloatingBar).GetAttribute("style");

        await cut.InvokeAsync(cut.Instance.ApplyFormattedSelection);

        var updatedStyle = cut.FindByTestId(UiTestIds.Editor.FloatingBar).GetAttribute("style");

        Assert.Equal(initialStyle, updatedStyle);
    }

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
