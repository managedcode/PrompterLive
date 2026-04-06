using Bunit;
using PrompterOne.Shared.Components;

namespace PrompterOne.Web.Tests;

public sealed class UiIconRenderingTests : BunitContext
{
    [Fact]
    public void HelpCircle_UsesRoundedQuestionStrokeAndFilledDot()
    {
        var cut = Render<UiIcon>(parameters => parameters
            .Add(icon => icon.Kind, UiIconKind.HelpCircle)
            .Add(icon => icon.Size, 16));

        var svg = cut.Find("svg");

        Assert.Equal("round", svg.GetAttribute("stroke-linecap"));
        Assert.Equal("round", svg.GetAttribute("stroke-linejoin"));
        Assert.NotNull(svg.QuerySelector("path[d='M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3']"));

        var dot = svg.QuerySelector("circle[cx='12'][cy='17'][r='1']");
        Assert.NotNull(dot);
        Assert.Equal("currentColor", dot!.GetAttribute("fill"));
        Assert.Equal("none", dot.GetAttribute("stroke"));
        Assert.Null(svg.QuerySelector("line[x1='12'][y1='17'][x2='12.01'][y2='17']"));
    }
}
