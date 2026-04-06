using Bunit;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class LibraryCardColorTests : BunitContext
{
    private const string CssColorVariableName = "--emo:";
    private const string NeutralAccentCssValue = "#2563EB";
    private const string MotivationalAccentCssValue = "#7C3AED";
    private const string CalmAccentCssValue = "#0F766E";
    private const string UrgentAccentCssValue = "#DC2626";

    public LibraryCardColorTests()
    {
        TestHarnessFactory.Create(this);
    }

    [Test]
    public void LibraryPage_UsesCssSafeAccentColorsForEmotionCards()
    {
        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() =>
        {
            AssertCardAccent(cut, AppTestData.Scripts.QuantumId, NeutralAccentCssValue);
            AssertCardAccent(cut, AppTestData.Scripts.LeadershipId, MotivationalAccentCssValue);
            AssertCardAccent(cut, AppTestData.Scripts.ArchitectureId, CalmAccentCssValue);
            AssertCardAccent(cut, AppTestData.Scripts.SecurityIncidentId, UrgentAccentCssValue);
        });
    }

    private static void AssertCardAccent(IRenderedComponent<LibraryPage> cut, string scriptId, string expectedColor)
    {
        var card = cut.FindByTestId(UiTestIds.Library.Card(scriptId));
        var style = card.GetAttribute("style");

        Assert.Contains(
            string.Concat(CssColorVariableName, expectedColor),
            style,
            StringComparison.Ordinal);
    }
}
