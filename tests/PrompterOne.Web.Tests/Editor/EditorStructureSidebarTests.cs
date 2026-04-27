using Bunit;
using PrompterOne.Shared.Components.Editor;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class EditorStructureSidebarTests : BunitContext
{
    private const string FocusedAccentColor = "#16A34A";
    private const string WarmEmotion = "Warm";
    private const string EpisodeTitle = "Episode 1";
    private const string BlockTitle = "Opening";
    private const string LongSegmentTitle = "Imported section title that should stay clamped inside the structure rail instead of stretching the whole sidebar";
    private const string LongBlockTitle = "Imported block title that should ellipsize inside the structure tree row";

    public EditorStructureSidebarTests()
    {
        TestHarnessFactory.Create(this);
    }

    [Test]
    public void EditorStructureSidebar_NavigationButtonsInvokeExpectedTargets()
    {
        var navigationTargets = new List<EditorNavigationTarget>();
        var cut = Render<EditorStructureSidebar>(parameters => parameters
            .Add(component => component.ActiveSegmentIndex, 0)
            .Add(component => component.ActiveBlockIndex, 0)
            .Add(component => component.Segments, BuildSegments())
            .Add(component => component.OnNavigate, target => navigationTargets.Add(target)));

        cut.FindByTestId(UiTestIds.Editor.SegmentNavigation(0)).Click();
        cut.FindByTestId(UiTestIds.Editor.BlockNavigation(0, 0)).Click();

        Assert.Equal(
            [
                new EditorNavigationTarget(0, null, 0, 20),
                new EditorNavigationTarget(0, 0, 0, 20)
            ],
            navigationTargets);
    }

    [Test]
    public void EditorStructureSidebar_LongNames_RenderClampFriendlyNameElements()
    {
        var cut = Render<EditorStructureSidebar>(parameters => parameters
            .Add(component => component.ActiveSegmentIndex, 0)
            .Add(component => component.ActiveBlockIndex, 0)
            .Add(component => component.Segments, BuildSegments(LongSegmentTitle, LongBlockTitle)));

        var segmentName = cut.Find(".ed-tree-seg__name");
        var blockName = cut.Find(".ed-tree-block__name");

        Assert.Equal(LongSegmentTitle, segmentName.TextContent.Trim());
        Assert.Equal(LongSegmentTitle, segmentName.GetAttribute("title"));
        Assert.Equal(LongBlockTitle, blockName.TextContent.Trim());
        Assert.Equal(LongBlockTitle, blockName.GetAttribute("title"));
    }

    [Test]
    public void EditorStructureSidebar_CollapsedStateKeepsPanelToggleVisible()
    {
        var cut = Render<EditorStructureSidebar>(parameters => parameters
            .Add(component => component.IsCollapsed, true)
            .Add(component => component.ToggleLabel, "Open sidebar")
            .Add(component => component.Segments, BuildSegments()));

        var sidebar = cut.FindByTestId(UiTestIds.Editor.StructureSidebar);
        var toggle = cut.FindByTestId(UiTestIds.Editor.StructureSidebarToggle);

        Assert.Equal("active", sidebar.GetAttribute("data-collapsed"));
        Assert.Equal("false", toggle.GetAttribute("aria-expanded"));
        Assert.Equal("left", toggle.GetAttribute("data-sidebar-icon"));
        Assert.DoesNotContain(UiTestIds.Editor.SegmentNavigation(0), cut.Markup, StringComparison.Ordinal);
    }

    private static IReadOnlyList<EditorOutlineSegmentViewModel> BuildSegments(
        string segmentTitle = EpisodeTitle,
        string blockTitle = BlockTitle) =>
    [
        new(
            Index: 0,
            Name: segmentTitle,
            EmotionKey: "focused",
            EmotionLabel: WarmEmotion,
            AccentColor: FocusedAccentColor,
            TargetWpm: 140,
            DurationLabel: "0:30",
            StartIndex: 0,
            EndIndex: 20,
            Blocks:
            [
                new EditorOutlineBlockViewModel(
                    Index: 0,
                    Name: blockTitle,
                    EmotionLabel: WarmEmotion,
                    TargetWpm: 140,
                    StartIndex: 0,
                    EndIndex: 20)
            ])
    ];
}
