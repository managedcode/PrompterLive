using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using PrompterOne.Core.Models.Documents;
using PrompterOne.Shared.Components;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class LibraryCardTooltipTests : BunitContext
{
    private static readonly DateTimeOffset UpdatedAt = new(2026, 4, 5, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void LibraryCard_DeleteAction_UsesSharedTooltipSurface()
    {
        TestHarnessFactory.Create(this);

        var deleteTestId = $"{Summary.LoadAutomationId}-delete";
        var expectedTooltip = Services.GetRequiredService<IStringLocalizer<SharedResource>>()[UiTextKey.TooltipDeleteScript.ToString()];
        var cut = Render<LibraryCard>(parameters => parameters.Add(component => component.Summary, Summary));

        var deleteButton = cut.FindByTestId(deleteTestId);
        var tooltip = cut.FindByTestId(UiTestIds.Tooltip.Surface(deleteTestId));

        Assert.Null(deleteButton.GetAttribute("title"));
        Assert.Equal(expectedTooltip.Value, deleteButton.GetAttribute("aria-label"));
        Assert.Equal(expectedTooltip.Value, tooltip.TextContent.Trim());
        Assert.Equal("tooltip", tooltip.GetAttribute("role"));
    }

    private static StoredScriptSummary Summary { get; } = new(
        "library-card-tooltip-script",
        "Tooltip Card",
        "tooltip-card.tps",
        UpdatedAt,
        120);
}
