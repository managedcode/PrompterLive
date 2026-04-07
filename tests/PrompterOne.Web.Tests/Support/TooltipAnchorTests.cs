using Bunit;
using Microsoft.AspNetCore.Components;
using PrompterOne.Shared.Components;
using PrompterOne.Shared.Contracts;

namespace PrompterOne.Web.Tests;

public sealed class TooltipAnchorTests : BunitContext
{
    private const string OwnerTestId = "tooltip-owner";
    private const string TooltipText = "Tooltip text";

    [Test]
    public void TooltipAnchor_DefaultPlacement_RendersSharedTooltipContract()
    {
        var cut = RenderTooltipAnchor(TooltipPlacement.TopCenter);

        var tooltip = cut.FindByTestId(UiTestIds.Tooltip.Surface(OwnerTestId));

        Assert.Equal(UiDomIds.Tooltip.Surface(OwnerTestId), tooltip.Id);
        Assert.Equal("tooltip", tooltip.GetAttribute("role"));
        Assert.Equal(UiTestIds.Tooltip.Surface(OwnerTestId), tooltip.GetAttribute("data-test"));
        Assert.Equal(UiTestIds.Tooltip.Surface(OwnerTestId), tooltip.GetAttribute("data-test"));
        Assert.Equal("top", tooltip.GetAttribute("data-tooltip-placement"));
        Assert.Equal(TooltipText, tooltip.TextContent.Trim());
    }

    [Test]
    public void TooltipAnchor_LeftPlacement_RendersPlacementAttribute()
    {
        var cut = RenderTooltipAnchor(TooltipPlacement.LeftCenter);

        var tooltip = cut.FindByTestId(UiTestIds.Tooltip.Surface(OwnerTestId));

        Assert.Contains("po-tooltip-surface--left", tooltip.ClassName, StringComparison.Ordinal);
        Assert.Equal("left", tooltip.GetAttribute("data-tooltip-placement"));
    }

    private IRenderedComponent<TooltipAnchor> RenderTooltipAnchor(TooltipPlacement placement) =>
        Render<TooltipAnchor>(parameters => parameters
            .Add(component => component.OwnerTestId, OwnerTestId)
            .Add(component => component.Placement, placement)
            .Add(component => component.Text, TooltipText)
            .Add(component => component.ChildContent, BuildTriggerContent()));

    private static RenderFragment BuildTriggerContent() =>
        builder =>
        {
            builder.OpenElement(0, "button");
            builder.AddContent(1, "Trigger");
            builder.CloseElement();
        };
}
