using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Tests;

namespace PrompterOne.App.Tests;

public sealed class TeleprompterFidelityTests : BunitContext
{
    private const int BenefitsCardIndex = 5;
    private const int ClosingCardIndex = 7;
    private const int InspirationCardIndex = 6;
    private const int SpeedOffsetsCardIndex = 0;
    private const int StatisticsCardIndex = 2;
    private const string FastWord = "Full";
    private const string PurpleWord = "focus";
    private const string SlowWord = "elephant";
    private const string SpeedOffsetsFastWord = "flight";
    private const string SpeedOffsetsNormalWord = "center";
    private const string SpeedOffsetsResumedSlowWord = "gentle";
    private const string SpeedOffsetsSlowWord = "steady";
    private const string SpeedOffsetsSlowWpm = "126";
    private const string SpeedOffsetsFastWpm = "154";
    private const string TeleprompterWord = "teleprompter";
    private const string VisionWord = "vision";

    [Fact]
    public void TeleprompterPage_UsesReferenceSizedReaderGroupsForSecurityIncident()
    {
        var harness = TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.TeleprompterSecurityIncident);
        var cut = Render<TeleprompterPage>();

        cut.WaitForAssertion(() =>
        {
            var groups = cut.FindAll($"{BunitTestSelectors.BuildTestIdSelector(UiTestIds.Teleprompter.Card(0))} [data-testid^='{UiTestIds.Teleprompter.CardGroupPrefix(0)}']");
            var groupTexts = groups.Select(group => group.TextContent).ToArray();

            Assert.NotEmpty(groups);
            Assert.True(groups.Count >= 4);
            Assert.All(groups, group =>
            {
                var wordCount = group.QuerySelectorAll(".rd-w").Length;
                Assert.InRange(wordCount, 1, 5);
            });
            Assert.Contains(groupTexts, text => text.Contains("At 04:12 this morning", StringComparison.Ordinal));
            Assert.DoesNotContain(
                groupTexts,
                text => text.Contains(
                    "At 04:12 this morning, our monitoring systems detected unauthorized activity in a production environment",
                    StringComparison.Ordinal));
            Assert.DoesNotContain("rd-camera-overlay-", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void TeleprompterPage_PropagatesTpsWordFormattingTimingAndPronunciationMetadata()
    {
        var harness = TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.TeleprompterDemo);
        var cut = Render<TeleprompterPage>();

        cut.WaitForAssertion(() =>
        {
            var slowWord = FindReaderWordByText(cut, StatisticsCardIndex, SlowWord);
            var fastWord = FindReaderWordByText(cut, BenefitsCardIndex, FastWord);
            var visionWord = FindReaderWordByText(cut, InspirationCardIndex, VisionWord);
            var teleprompterWord = FindReaderWordByText(cut, ClosingCardIndex, TeleprompterWord);
            var purpleWord = FindReaderWordByText(cut, InspirationCardIndex, PurpleWord);

            Assert.Contains("tps-xslow", slowWord.ClassName, StringComparison.Ordinal);
            Assert.Contains("--tps-word-letter-spacing:", slowWord.GetAttribute("style"), StringComparison.Ordinal);
            Assert.Contains("Speed: 90 WPM", slowWord.GetAttribute("title"), StringComparison.Ordinal);

            Assert.Contains("tps-xfast", fastWord.ClassName, StringComparison.Ordinal);
            Assert.Contains("--tps-word-letter-spacing:-", fastWord.GetAttribute("style"), StringComparison.Ordinal);
            Assert.True(GetWordDurationMilliseconds(slowWord) > GetWordDurationMilliseconds(fastWord));

            Assert.Contains("tps-purple", purpleWord.ClassName, StringComparison.Ordinal);

            Assert.Equal("ˈviʒən", visionWord.GetAttribute("data-pronunciation"));
            Assert.Contains("Pronunciation: ˈviʒən", visionWord.GetAttribute("title"), StringComparison.Ordinal);

            Assert.Equal("TELE-promp-ter", teleprompterWord.GetAttribute("data-pronunciation"));
            Assert.Equal("180", teleprompterWord.GetAttribute("data-effective-wpm"));
            Assert.Contains("Speed: 180 WPM", teleprompterWord.GetAttribute("title"), StringComparison.Ordinal);
        });
    }

    [Fact]
    public void TeleprompterPage_UsesCustomFrontMatterSpeedOffsetsAndNormalReset()
    {
        var harness = TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.TeleprompterSpeedOffsets);
        var cut = Render<TeleprompterPage>();

        cut.WaitForAssertion(() =>
        {
            var slowWord = FindReaderWordByText(cut, SpeedOffsetsCardIndex, SpeedOffsetsSlowWord);
            var normalWord = FindReaderWordByText(cut, SpeedOffsetsCardIndex, SpeedOffsetsNormalWord);
            var resumedSlowWord = FindReaderWordByText(cut, SpeedOffsetsCardIndex, SpeedOffsetsResumedSlowWord);
            var fastWord = FindReaderWordByText(cut, SpeedOffsetsCardIndex, SpeedOffsetsFastWord);
            var normalWordClassName = normalWord.ClassName ?? string.Empty;

            Assert.Contains("tps-slow", slowWord.ClassName, StringComparison.Ordinal);
            Assert.Equal(SpeedOffsetsSlowWpm, slowWord.GetAttribute("data-effective-wpm"));
            Assert.Contains("Speed: 126 WPM", slowWord.GetAttribute("title"), StringComparison.Ordinal);
            Assert.Contains("--tps-word-letter-spacing:", slowWord.GetAttribute("style"), StringComparison.Ordinal);

            Assert.Equal("140", normalWord.GetAttribute("data-effective-wpm"));
            Assert.False(normalWordClassName.Contains("tps-slow", StringComparison.Ordinal));
            Assert.False(normalWordClassName.Contains("tps-fast", StringComparison.Ordinal));
            Assert.Null(normalWord.GetAttribute("style"));
            Assert.Null(normalWord.GetAttribute("title"));

            Assert.Contains("tps-slow", resumedSlowWord.ClassName, StringComparison.Ordinal);
            Assert.Equal(SpeedOffsetsSlowWpm, resumedSlowWord.GetAttribute("data-effective-wpm"));

            Assert.Contains("tps-fast", fastWord.ClassName, StringComparison.Ordinal);
            Assert.Equal(SpeedOffsetsFastWpm, fastWord.GetAttribute("data-effective-wpm"));
            Assert.Contains("--tps-word-letter-spacing:-", fastWord.GetAttribute("style"), StringComparison.Ordinal);

            Assert.True(GetWordDurationMilliseconds(slowWord) > GetWordDurationMilliseconds(normalWord));
            Assert.True(GetWordDurationMilliseconds(resumedSlowWord) > GetWordDurationMilliseconds(normalWord));
            Assert.True(GetWordDurationMilliseconds(normalWord) > GetWordDurationMilliseconds(fastWord));
        });
    }

    private static AngleSharp.Dom.IElement FindReaderWordByText(IRenderedComponent<TeleprompterPage> cut, int cardIndex, string text) =>
        cut.FindByTestId(UiTestIds.Teleprompter.CardText(cardIndex))
            .QuerySelectorAll(".rd-w")
            .Single(element => string.Equals(element.TextContent.Trim(), text, StringComparison.Ordinal));

    private static int GetWordDurationMilliseconds(AngleSharp.Dom.IElement word) =>
        int.Parse(word.GetAttribute("data-ms")!, CultureInfo.InvariantCulture);
}
