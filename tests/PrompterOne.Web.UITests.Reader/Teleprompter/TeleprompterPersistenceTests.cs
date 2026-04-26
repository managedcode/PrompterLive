using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Storage;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class TeleprompterPersistenceTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture)
{
    private const int MinimumReaderSpeedWpm = 60;
    private const int ReaderSpeedStepWpm = 10;
    private const string WordsPerMinuteSuffix = "WPM";
    private const string PersistedFocalPointValue = "37";
    private const string PersistedFontValue = "52";
    private const string PersistedFocalGuideStyle = "top:37%;";
    private const string PersistedFontStyleFragment = "--rd-font-size:52px";
    private const string PersistedTextWidthLabel = "79%";
    private const string PersistedTextWidthValue = "79";
    private const decimal ReaderFontBaselinePixels = 36m;
    private const int StoredReaderSettingPrecisionDigits = 2;
    private const string PersistedTextAlignmentValue = BrowserTestConstants.TeleprompterFlow.AlignmentJustifyValue;
    private const string StoredReaderSettingsKey = BrowserStorageKeys.SettingsPrefix + BrowserAppSettingsKeys.ReaderSettings;
    private static readonly Regex ReaderFirstBlockIndicator = new(@"^1 / \d+$", RegexOptions.Compiled);

    [Test]
    public Task Teleprompter_PersistsWidthAndFocalSettingsAcrossReload() =>
        RunPageAsync(async page =>
        {
            await ReaderRouteDriver.OpenTeleprompterAsync(page, BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await SetRangeValueAsync(page.GetByTestId(UiTestIds.Teleprompter.FontSlider), PersistedFontValue);
            await SetRangeValueAsync(page.GetByTestId(UiTestIds.Teleprompter.WidthSlider), PersistedTextWidthValue);
            await SetRangeValueAsync(page.GetByTestId(UiTestIds.Teleprompter.FocalSlider), PersistedFocalPointValue);
            await page.GetByTestId(UiTestIds.Teleprompter.AlignmentJustify).ClickAsync();
            await page.GetByTestId(UiTestIds.Teleprompter.AutoLoopToggle).ClickAsync();
            var speedValue = page.GetByTestId(UiTestIds.Teleprompter.SpeedValue);
            var baselineSpeedText = await speedValue.TextContentAsync() ?? string.Empty;
            var baselineSpeedWpm = ParseWordsPerMinuteValue(baselineSpeedText);
            var expectedSpeedWpm = baselineSpeedWpm + ReaderSpeedStepWpm;
            await page.GetByTestId(UiTestIds.Teleprompter.SpeedUp).ClickAsync();

            await Expect(page.GetByTestId(UiTestIds.Teleprompter.FontValue)).ToHaveTextAsync(PersistedFontValue);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.WidthValue)).ToHaveTextAsync(PersistedTextWidthLabel);
            await Expect(speedValue).ToHaveTextAsync(BuildWordsPerMinuteLabel(expectedSpeedWpm));
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.FocalGuide)).ToHaveAttributeAsync("style", PersistedFocalGuideStyle);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.ClusterWrap))
                .ToHaveAttributeAsync("style", new Regex(Regex.Escape(PersistedFontStyleFragment), RegexOptions.Compiled));
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.ClusterWrap))
                .ToHaveAttributeAsync(BrowserTestConstants.TeleprompterFlow.ReaderTextAlignmentAttribute, PersistedTextAlignmentValue);

            var storedJson = await page.EvaluateAsync<string>(
                "(storageKey) => localStorage.getItem(storageKey) ?? ''",
                StoredReaderSettingsKey);
            var storedSettings = JsonSerializer.Deserialize<ReaderSettings>(storedJson);
            var expectedFontScale = RoundStoredReaderSetting(
                decimal.Parse(PersistedFontValue, CultureInfo.InvariantCulture) / ReaderFontBaselinePixels);
            var expectedTextWidth = RoundStoredReaderSetting(
                decimal.Parse(PersistedTextWidthValue, CultureInfo.InvariantCulture) / 100m);

            await Assert.That(storedSettings).IsNotNull();
            await Assert.That(storedSettings.FocalPointPercent).IsEqualTo(int.Parse(PersistedFocalPointValue, CultureInfo.InvariantCulture));
            await Assert.That(RoundStoredReaderSetting((decimal)storedSettings.FontScale)).IsEqualTo(expectedFontScale);
            await Assert.That(RoundStoredReaderSetting((decimal)storedSettings.TextWidth)).IsEqualTo(expectedTextWidth);
            await Assert.That(storedSettings.ScrollSpeed).IsEqualTo(expectedSpeedWpm);
            await Assert.That(storedSettings.TextAlignment).IsEqualTo(ReaderTextAlignment.Justify);
            await Assert.That(storedSettings.AutoLoop).IsFalse();

            await BrowserRouteDriver.ReloadPageAsync(
                page,
                BrowserTestConstants.Routes.TeleprompterDemo,
                UiTestIds.Teleprompter.Page,
                $"{nameof(Teleprompter_PersistsWidthAndFocalSettingsAcrossReload)}-reload");
            await PlaybackRouteDriver.WaitForTeleprompterReadyAsync(page, BrowserTestConstants.Routes.TeleprompterDemo);

            await Expect(page.GetByTestId(UiTestIds.Teleprompter.FontSlider)).ToHaveValueAsync(PersistedFontValue);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.WidthSlider)).ToHaveValueAsync(PersistedTextWidthValue);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.FocalSlider)).ToHaveValueAsync(PersistedFocalPointValue);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.FontValue)).ToHaveTextAsync(PersistedFontValue);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.WidthValue)).ToHaveTextAsync(PersistedTextWidthLabel);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.SpeedValue)).ToHaveTextAsync(BuildWordsPerMinuteLabel(expectedSpeedWpm));
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.FocalGuide)).ToHaveAttributeAsync("style", PersistedFocalGuideStyle);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.ClusterWrap))
                .ToHaveAttributeAsync("style", new Regex(Regex.Escape(PersistedFontStyleFragment), RegexOptions.Compiled));
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.ClusterWrap))
                .ToHaveAttributeAsync(BrowserTestConstants.TeleprompterFlow.ReaderTextAlignmentAttribute, PersistedTextAlignmentValue);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.AutoLoopToggle))
                .ToHaveAttributeAsync("aria-pressed", "false");
        });

    private static decimal RoundStoredReaderSetting(decimal value) =>
        Math.Round(value, StoredReaderSettingPrecisionDigits);

    [Test]
    public Task Teleprompter_BackwardBlockJump_ReversesOutgoingCardDirection() =>
        RunPageAsync(async page =>
        {
            await ReaderRouteDriver.OpenTeleprompterAsync(page, BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            var firstCard = page.GetByTestId(UiTestIds.Teleprompter.Card(0));
            var secondCard = page.GetByTestId(UiTestIds.Teleprompter.Card(1));

            await page.GetByTestId(UiTestIds.Teleprompter.NextBlock).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.BlockIndicator))
                .ToHaveTextAsync(BrowserTestConstants.Regexes.ReaderSecondBlockIndicator);
            await Expect(secondCard)
                .ToHaveAttributeAsync(UiDataAttributes.Teleprompter.CardState, UiDataAttributes.Teleprompter.ActiveState);

            await page.GetByTestId(UiTestIds.Teleprompter.PreviousBlock).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.BlockIndicator))
                .ToHaveTextAsync(ReaderFirstBlockIndicator);
            await Expect(firstCard)
                .ToHaveAttributeAsync(UiDataAttributes.Teleprompter.CardState, UiDataAttributes.Teleprompter.ActiveState);
            await Expect(secondCard).ToHaveAttributeAsync(
                UiDataAttributes.Teleprompter.CardState,
                UiDataAttributes.Teleprompter.NextState,
                new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });

            var secondCardState = await secondCard.GetAttributeAsync(UiDataAttributes.Teleprompter.CardState);
            await Assert.That(secondCardState).IsEqualTo(UiDataAttributes.Teleprompter.NextState);
        });

    [Test]
    public Task Teleprompter_SpeedDown_ClampsAtSixtyWordsPerMinute() =>
        RunPageAsync(async page =>
        {
            await ReaderRouteDriver.OpenTeleprompterAsync(page, BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            var speedValue = page.GetByTestId(UiTestIds.Teleprompter.SpeedValue);
            var baselineSpeedText = await speedValue.TextContentAsync() ?? string.Empty;
            var baselineSpeedWpm = ParseWordsPerMinuteValue(baselineSpeedText);
            var speedReductionClicks = Math.Max(0, (baselineSpeedWpm - MinimumReaderSpeedWpm) / ReaderSpeedStepWpm);

            for (var clickIndex = 0; clickIndex < speedReductionClicks + 1; clickIndex++)
            {
                await page.GetByTestId(UiTestIds.Teleprompter.SpeedDown).ClickAsync();
            }

            await Expect(speedValue).ToHaveTextAsync(BuildWordsPerMinuteLabel(MinimumReaderSpeedWpm));

            var storedJson = await page.EvaluateAsync<string>(
                "(storageKey) => localStorage.getItem(storageKey) ?? ''",
                StoredReaderSettingsKey);
            var storedSettings = JsonSerializer.Deserialize<ReaderSettings>(storedJson);

            await Assert.That(storedSettings).IsNotNull();
            await Assert.That(storedSettings.ScrollSpeed).IsEqualTo(MinimumReaderSpeedWpm);
        });

    private static Task SetRangeValueAsync(Microsoft.Playwright.ILocator locator, string value) =>
        locator.EvaluateAsync(
            """
            (element, nextValue) => {
                element.value = nextValue;
                element.dispatchEvent(new Event("input", { bubbles: true }));
                element.dispatchEvent(new Event("change", { bubbles: true }));
            }
            """,
            value);

    private static string BuildWordsPerMinuteLabel(int speedWpm) =>
        $"{speedWpm} {WordsPerMinuteSuffix}";

    private static int ParseWordsPerMinuteValue(string speedText)
    {
        var tokens = speedText
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0 || !int.TryParse(tokens[0], out var parsedWpm))
        {
            throw new InvalidOperationException($"Unable to parse teleprompter speed value from '{speedText}'.");
        }

        return parsedWpm;
    }
}
