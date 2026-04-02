using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Tests;

namespace PrompterOne.App.Tests;

public sealed class ReaderStartupStateTests : Bunit.BunitContext
{
    [Fact]
    public void LearnPage_DoesNotRenderPlaceholderCopyBeforeScriptLoadCompletes()
    {
        TestHarnessFactory.Create(this, jsInvocationDelay: ReaderStartupTestDelays.JavascriptLoadDelay);
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.LearnQuantum);

        var cut = Render<LearnPage>();

        Assert.DoesNotContain(ReaderStartupPlaceholderTexts.LearnFocusWord, cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain(ReaderStartupPlaceholderTexts.LearnNextPhrase, cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void TeleprompterPage_DoesNotRenderPlaceholderCopyBeforeScriptLoadCompletes()
    {
        TestHarnessFactory.Create(this, jsInvocationDelay: ReaderStartupTestDelays.JavascriptLoadDelay);
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.TeleprompterQuantum);

        var cut = Render<TeleprompterPage>();

        Assert.Empty(cut.FindAll(BunitTestSelectors.BuildTestIdSelector(UiTestIds.Teleprompter.Card(0))));
        Assert.Empty(cut.FindAll(BunitTestSelectors.BuildTestIdSelector(UiTestIds.Teleprompter.CardText(0))));
    }

    private static class ReaderStartupPlaceholderTexts
    {
        public const string LearnFocusWord = "transformative";
        public const string LearnNextPhrase = "Today, we're not just launching a product";
    }

    private static class ReaderStartupTestDelays
    {
        public static TimeSpan JavascriptLoadDelay => TimeSpan.FromMilliseconds(300);
    }
}
