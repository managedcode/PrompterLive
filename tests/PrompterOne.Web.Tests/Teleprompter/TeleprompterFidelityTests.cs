using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class TeleprompterFidelityTests : BunitContext
{
    private const int BenefitsCardIndex = 5;
    private const int ClosingCardIndex = 7;
    private const string ContinuousEmphasisCssClass = "rd-g-emphasis";
    private const string ProfessionalWord = "transformative";
    private const string HighlightWord = "solution";
    private const int IntroductionCardIndex = 4;
    private const string IntroductionWord = "comes";
    private const int InspirationCardIndex = 6;
    private const double MinimumVisibleFastLetterSpacingEm = -0.024d;
    private const double MinimumVisibleSlowLetterSpacingEm = 0.045d;
    private const string MaximumReaderWidthLabel = "100%";
    private const string MaximumReaderWidthValue = "100";
    private const string MaximumReaderContentScaleStyle = "--rd-stage-content-scale:0.97";
    private const string MaximumReaderWidthScaleStyle = "--rd-stage-width-scale:1";
    private const string NeutralWord = "Good";
    private const int OpeningCardIndex = 0;
    private const int PurposeCardIndex = 1;
    private const int SecurityIncidentResponseCardIndex = 2;
    private const string SecurityIncidentStandaloneComma = ",";
    private const int SpeedOffsetsCardIndex = 0;
    private const int StatisticsCardIndex = 2;
    private const string FastWord = "Full";
    private const string RhetoricalWord = "focus";
    private const string SecurityIncidentEmphasisPhrase = "No payment data was exposed,";
    private const string SlowWord = "elephant";
    private const string SpeedOffsetsFastWord = "flight.";
    private const string SpeedOffsetsNormalWord = "center";
    private const string SpeedOffsetsResumedSlowWord = "gentle";
    private const string SpeedOffsetsSlowWord = "steady";
    private const string SpeedOffsetsSlowWpm = "126";
    private const string SpeedOffsetsFastWpm = "154";
    private const string WordLetterSpacingVariableName = "--tps-word-letter-spacing";
    private const string TeleprompterWord = "teleprompter";
    private const string UrgentWord = "time";
    private const string VisionWord = "vision";
    private const string SoftWord = "Let";

    [Test]
    public void TeleprompterPage_UsesReferenceSizedReaderGroupsForSecurityIncident()
    {
        var harness = TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.TeleprompterSecurityIncident);
        var cut = Render<TeleprompterPage>();

        cut.WaitForAssertion(() =>
        {
            var groups = cut.FindAll($"{BunitTestSelectors.BuildTestIdSelector(UiTestIds.Teleprompter.Card(0))} [data-test^='{UiTestIds.Teleprompter.CardGroupPrefix(0)}']");
            var groupTexts = groups.Select(group => group.TextContent).ToArray();

            Assert.NotEmpty(groups);
            Assert.True(groups.Count >= 4);
            Assert.All(groups, group =>
            {
                var wordCount = group.TextContent
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Length;
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

    [Test]
    public void TeleprompterPage_StartsWithMaximumReaderWidthByDefault()
    {
        var harness = TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.TeleprompterDemo);
        var cut = Render<TeleprompterPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(MaximumReaderWidthValue, cut.FindByTestId(UiTestIds.Teleprompter.WidthSlider).GetAttribute("value"));
            Assert.Equal(MaximumReaderWidthLabel, cut.FindByTestId(UiTestIds.Teleprompter.WidthValue).TextContent.Trim());
            Assert.Contains(MaximumReaderWidthScaleStyle, cut.FindByTestId(UiTestIds.Teleprompter.Stage).GetAttribute("style"), StringComparison.Ordinal);
            Assert.Contains(MaximumReaderContentScaleStyle, cut.FindByTestId(UiTestIds.Teleprompter.Stage).GetAttribute("style"), StringComparison.Ordinal);
        });
    }

    [Test]
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
            var rhetoricalWord = FindReaderWordByText(cut, InspirationCardIndex, RhetoricalWord);

            Assert.Contains("tps-xslow", slowWord.ClassName, StringComparison.Ordinal);
            Assert.Contains("--tps-word-letter-spacing:", slowWord.GetAttribute("style"), StringComparison.Ordinal);
            Assert.Equal("90", slowWord.GetAttribute(UiDataAttributes.Teleprompter.EffectiveWordsPerMinute));
            Assert.Null(slowWord.GetAttribute("title"));
            Assert.True(GetLetterSpacingEm(slowWord) >= MinimumVisibleSlowLetterSpacingEm);

            Assert.Contains("tps-xfast", fastWord.ClassName, StringComparison.Ordinal);
            Assert.Contains("--tps-word-letter-spacing:-", fastWord.GetAttribute("style"), StringComparison.Ordinal);
            Assert.True(GetLetterSpacingEm(fastWord) <= MinimumVisibleFastLetterSpacingEm);
            Assert.True(GetWordDurationMilliseconds(slowWord) > GetWordDurationMilliseconds(fastWord));

            Assert.Contains("tps-rhetorical", rhetoricalWord.ClassName, StringComparison.Ordinal);

            Assert.Equal("ˈviʒən", visionWord.GetAttribute(UiDataAttributes.Teleprompter.Pronunciation));
            Assert.Null(visionWord.GetAttribute("title"));

            Assert.Equal("TELE-promp-ter", teleprompterWord.GetAttribute(UiDataAttributes.Teleprompter.Pronunciation));
            Assert.Equal("180", teleprompterWord.GetAttribute(UiDataAttributes.Teleprompter.EffectiveWordsPerMinute));
            Assert.Null(teleprompterWord.GetAttribute("title"));
        });
    }

    [Test]
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
            Assert.Equal(SpeedOffsetsSlowWpm, slowWord.GetAttribute(UiDataAttributes.Teleprompter.EffectiveWordsPerMinute));
            Assert.Null(slowWord.GetAttribute("title"));
            Assert.Contains("--tps-word-letter-spacing:", slowWord.GetAttribute("style"), StringComparison.Ordinal);
            Assert.True(GetLetterSpacingEm(slowWord) >= MinimumVisibleSlowLetterSpacingEm);

            Assert.Equal("140", normalWord.GetAttribute(UiDataAttributes.Teleprompter.EffectiveWordsPerMinute));
            Assert.False(normalWordClassName.Contains("tps-slow", StringComparison.Ordinal));
            Assert.False(normalWordClassName.Contains("tps-fast", StringComparison.Ordinal));
            Assert.Null(normalWord.GetAttribute("style"));
            Assert.Null(normalWord.GetAttribute("title"));

            Assert.Contains("tps-slow", resumedSlowWord.ClassName, StringComparison.Ordinal);
            Assert.Equal(SpeedOffsetsSlowWpm, resumedSlowWord.GetAttribute("data-effective-wpm"));

            Assert.Contains("tps-fast", fastWord.ClassName, StringComparison.Ordinal);
            Assert.Equal(SpeedOffsetsFastWpm, fastWord.GetAttribute("data-effective-wpm"));
            Assert.Contains("--tps-word-letter-spacing:-", fastWord.GetAttribute("style"), StringComparison.Ordinal);
            Assert.True(GetLetterSpacingEm(fastWord) <= MinimumVisibleFastLetterSpacingEm);

            Assert.True(GetWordDurationMilliseconds(slowWord) > GetWordDurationMilliseconds(normalWord));
            Assert.True(GetWordDurationMilliseconds(resumedSlowWord) > GetWordDurationMilliseconds(normalWord));
            Assert.True(GetWordDurationMilliseconds(normalWord) > GetWordDurationMilliseconds(fastWord));
        });
    }

    [Test]
    public void TeleprompterPage_StylesOnlyExplicitInlineTpsEmotionAndColorTags()
    {
        var harness = TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.TeleprompterDemo);
        var cut = Render<TeleprompterPage>();

        cut.WaitForAssertion(() =>
        {
            var neutralWord = FindReaderWordByText(cut, OpeningCardIndex, NeutralWord);
            var professionalWord = FindReaderWordByText(cut, OpeningCardIndex, ProfessionalWord);
            var highlightWord = FindReaderWordByText(cut, PurposeCardIndex, HighlightWord);
            var softWord = FindReaderWordByText(cut, InspirationCardIndex, SoftWord);
            var urgentWord = FindReaderWordByText(cut, ClosingCardIndex, UrgentWord);
            var teleprompterWord = FindReaderWordByText(cut, ClosingCardIndex, TeleprompterWord);
            var introductionWord = FindReaderWordByText(cut, IntroductionCardIndex, IntroductionWord);

            Assert.DoesNotContain("tps-neutral", neutralWord.ClassName, StringComparison.Ordinal);
            Assert.DoesNotContain("tps-warm", neutralWord.ClassName, StringComparison.Ordinal);
            Assert.DoesNotContain("tps-focused", neutralWord.ClassName, StringComparison.Ordinal);

            Assert.Contains("tps-professional", professionalWord.ClassName, StringComparison.Ordinal);
            Assert.DoesNotContain("tps-warm", professionalWord.ClassName, StringComparison.Ordinal);

            Assert.Contains("tps-highlight", highlightWord.ClassName, StringComparison.Ordinal);
            Assert.DoesNotContain("tps-warm", highlightWord.ClassName, StringComparison.Ordinal);

            Assert.Contains("tps-soft", softWord.ClassName, StringComparison.Ordinal);
            Assert.DoesNotContain("tps-motivational", softWord.ClassName, StringComparison.Ordinal);

            Assert.Contains("tps-urgent", urgentWord.ClassName, StringComparison.Ordinal);
            Assert.DoesNotContain("tps-energetic", urgentWord.ClassName, StringComparison.Ordinal);

            Assert.DoesNotContain("tps-focused", introductionWord.ClassName, StringComparison.Ordinal);
            Assert.DoesNotContain("tps-energetic", teleprompterWord.ClassName, StringComparison.Ordinal);
        });
    }

    [Test]
    public void TeleprompterPage_RendersContinuousEmphasisGroupsAndNoStandalonePunctuationWords()
    {
        var harness = TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.TeleprompterSecurityIncident);
        var cut = Render<TeleprompterPage>();

        cut.WaitForAssertion(() =>
        {
            var emphasisGroup = cut.FindByTestId(UiTestIds.Teleprompter.CardGroup(SecurityIncidentResponseCardIndex, 0));
            var responseWords = cut.FindAll($"[data-test^='{UiTestIds.Teleprompter.CardWordPrefix(SecurityIncidentResponseCardIndex)}']")
                .Select(element => element.TextContent.Trim())
                .ToArray();

            Assert.Contains(SecurityIncidentEmphasisPhrase, emphasisGroup.TextContent, StringComparison.Ordinal);
            Assert.Contains(ContinuousEmphasisCssClass, emphasisGroup.ClassName, StringComparison.Ordinal);
            Assert.Contains("exposed,", responseWords);
            Assert.DoesNotContain(SecurityIncidentStandaloneComma, responseWords);
        });
    }

    [Test]
    public void TeleprompterPage_UsesDarkReaderBackgroundForGreenArchitectureRoute()
    {
        var harness = TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.TeleprompterArchitecture);
        var cut = Render<TeleprompterPage>();

        cut.WaitForAssertion(() =>
        {
            var gradient = cut.FindByTestId(UiTestIds.Teleprompter.Gradient);
            var className = gradient.ClassName ?? string.Empty;

            Assert.DoesNotContain("focused", className, StringComparison.Ordinal);
            Assert.True(
                className.Contains("calm", StringComparison.Ordinal) ||
                className.Contains("professional", StringComparison.Ordinal),
                $"Expected the architecture route to stay on a dark reader palette, but got '{className}'.");
        });
    }

    private static AngleSharp.Dom.IElement FindReaderWordByText(IRenderedComponent<TeleprompterPage> cut, int cardIndex, string text) =>
        cut.FindAll($"[data-test^='{UiTestIds.Teleprompter.CardWordPrefix(cardIndex)}']")
            .Single(element => string.Equals(element.TextContent.Trim(), text, StringComparison.Ordinal));

    private static int GetWordDurationMilliseconds(AngleSharp.Dom.IElement word) =>
        int.Parse(word.GetAttribute("data-ms")!, CultureInfo.InvariantCulture);

    private static double GetLetterSpacingEm(AngleSharp.Dom.IElement word)
    {
        var style = word.GetAttribute("style") ?? string.Empty;
        var value = style
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(segment => segment.Split(':', 2, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length == 2 && string.Equals(parts[0], WordLetterSpacingVariableName, StringComparison.Ordinal))
            .Select(parts => parts[1].Replace("em", string.Empty, StringComparison.Ordinal).Trim())
            .SingleOrDefault();

        return double.Parse(value ?? "0", CultureInfo.InvariantCulture);
    }
}
