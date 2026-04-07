using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PrompterOne.Shared.Components;
using PrompterOne.Shared.Contracts;

namespace PrompterOne.Web.Tests;

public sealed class TooltipAnchorTests : BunitContext
{
    private const string HiddenStateValue = "false";
    private const string OwnerTestId = "tooltip-owner";
    private const string TooltipText = "Tooltip text";
    private const string VisibleStateValue = "true";

    [Test]
    public void TooltipAnchor_DefaultPlacement_RendersSharedTooltipContract()
    {
        var cut = RenderTooltipAnchor(TooltipPlacement.TopCenter);

        var anchor = cut.FindByTestId(UiTestIds.Tooltip.Anchor(OwnerTestId));
        var tooltip = cut.FindByTestId(UiTestIds.Tooltip.Surface(OwnerTestId));

        Assert.Equal(UiTestIds.Tooltip.Anchor(OwnerTestId), anchor.GetAttribute("data-test"));
        Assert.Equal(UiDomIds.Tooltip.Surface(OwnerTestId), tooltip.Id);
        Assert.Equal("tooltip", tooltip.GetAttribute("role"));
        Assert.Equal(UiTestIds.Tooltip.Surface(OwnerTestId), tooltip.GetAttribute("data-test"));
        Assert.Equal("top", tooltip.GetAttribute("data-tooltip-placement"));
        Assert.Equal(TooltipText, tooltip.TextContent.Trim());
    }

    [Test]
    public void TooltipAnchor_LeftPlacement_RendersPlacementAttribute()
    {
        var cut = RenderTooltipAnchor(TooltipPlacement.LeftCenter);

        var tooltip = cut.FindByTestId(UiTestIds.Tooltip.Surface(OwnerTestId));

        Assert.Equal("left", tooltip.GetAttribute("data-tooltip-placement"));
    }

    [Test]
    public void TooltipAnchor_MouseLeave_HidesTooltipAfterHoverReveal()
    {
        var cut = RenderTooltipAnchor(TooltipPlacement.TopCenter);

        var anchor = cut.FindByTestId(UiTestIds.Tooltip.Anchor(OwnerTestId));
        var tooltip = cut.FindByTestId(UiTestIds.Tooltip.Surface(OwnerTestId));

        anchor.TriggerEvent("onmouseenter", new MouseEventArgs());
        Assert.Equal(VisibleStateValue, tooltip.GetAttribute("data-visible"));

        anchor.TriggerEvent("onmouseleave", new MouseEventArgs());
        Assert.Equal(HiddenStateValue, tooltip.GetAttribute("data-visible"));
    }

    [Test]
    public void TooltipAnchor_PointerDown_HidesTooltipUntilPointerLeaves()
    {
        var cut = RenderTooltipAnchor(TooltipPlacement.TopCenter);

        var anchor = cut.FindByTestId(UiTestIds.Tooltip.Anchor(OwnerTestId));
        var tooltip = cut.FindByTestId(UiTestIds.Tooltip.Surface(OwnerTestId));

        anchor.TriggerEvent("onmouseenter", new MouseEventArgs());
        Assert.Equal(VisibleStateValue, tooltip.GetAttribute("data-visible"));

        anchor.TriggerEvent("onpointerdown", new PointerEventArgs());
        Assert.Equal(HiddenStateValue, tooltip.GetAttribute("data-visible"));

        anchor.TriggerEvent("onmouseenter", new MouseEventArgs());
        Assert.Equal(HiddenStateValue, tooltip.GetAttribute("data-visible"));

        anchor.TriggerEvent("onmouseleave", new MouseEventArgs());
        anchor.TriggerEvent("onmouseenter", new MouseEventArgs());
        Assert.Equal(VisibleStateValue, tooltip.GetAttribute("data-visible"));
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
