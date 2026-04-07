using System;
using Bunit;
using PrompterOne.Shared.Components;

namespace PrompterOne.Web.Tests;

public sealed class UiIconRenderingTests : BunitContext
{
    [Test]
    public void HelpCircle_UsesRoundedQuestionStrokeAndFilledDot()
    {
        var cut = Render<UiIcon>(parameters => parameters
            .Add(icon => icon.Kind, UiIconKind.HelpCircle)
            .Add(icon => icon.Size, 16));

        var markup = cut.Markup;

        Assert.Contains("stroke-linecap=\"round\"", markup, StringComparison.Ordinal);
        Assert.Contains("stroke-linejoin=\"round\"", markup, StringComparison.Ordinal);
        Assert.Contains("path d=\"M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3\"", markup, StringComparison.Ordinal);
        Assert.Contains("circle cx=\"12\" cy=\"17\" r=\"1\" fill=\"currentColor\" stroke=\"none\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("line x1=\"12\" y1=\"17\" x2=\"12.01\" y2=\"17\"", markup, StringComparison.Ordinal);
    }
}
