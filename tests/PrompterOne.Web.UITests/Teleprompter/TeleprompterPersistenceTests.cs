using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Storage;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

public sealed class TeleprompterPersistenceTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture), IClassFixture<StandaloneAppFixture>
{
    private const string PersistedFocalPointValue = "37";
    private const string PersistedFontValue = "52";
    private const string PersistedFocalGuideStyle = "top:37%;";
    private const string PersistedFontStyleFragment = "--rd-font-size:52px";
    private const string PersistedTextWidthValue = "980";
    private const double ReaderFontBaselinePixels = 36d;
    private const double ReaderWidthBaselinePixels = 1240d;
    private const string PersistedTextAlignmentValue = BrowserTestConstants.TeleprompterFlow.AlignmentJustifyValue;
    private const string ReaderCardActiveClass = "rd-card-active";
    private const string ReaderCardNextClass = "rd-card-next";
    private const string StoredReaderSettingsKey = BrowserStorageKeys.SettingsPrefix + BrowserAppSettingsKeys.ReaderSettings;
    private static readonly Regex ReaderFirstBlockIndicator = new(@"^1 / \d+$", RegexOptions.Compiled);
    private static readonly Regex ReaderCardActiveClassRegex = new(@"\brd-card-active\b", RegexOptions.Compiled);
    private static readonly Regex ReaderCardNextClassRegex = new(@"\brd-card-next\b", RegexOptions.Compiled);

    [Fact]
    public Task Teleprompter_PersistsWidthAndFocalSettingsAcrossReload() =>
        RunPageAsync(async page =>
        {
            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await SetRangeValueAsync(page.GetByTestId(UiTestIds.Teleprompter.FontSlider), PersistedFontValue);
            await SetRangeValueAsync(page.GetByTestId(UiTestIds.Teleprompter.WidthSlider), PersistedTextWidthValue);
            await SetRangeValueAsync(page.GetByTestId(UiTestIds.Teleprompter.FocalSlider), PersistedFocalPointValue);
            await page.GetByTestId(UiTestIds.Teleprompter.AlignmentJustify).ClickAsync();

            await Expect(page.Locator($"#{UiDomIds.Teleprompter.FontLabel}")).ToHaveTextAsync(PersistedFontValue);
            await Expect(page.Locator($"#{UiDomIds.Teleprompter.WidthValue}")).ToHaveTextAsync(PersistedTextWidthValue);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.FocalGuide)).ToHaveAttributeAsync("style", PersistedFocalGuideStyle);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.ClusterWrap))
                .ToHaveAttributeAsync("style", new Regex(Regex.Escape(PersistedFontStyleFragment), RegexOptions.Compiled));
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.ClusterWrap))
                .ToHaveAttributeAsync(BrowserTestConstants.TeleprompterFlow.ReaderTextAlignmentAttribute, PersistedTextAlignmentValue);

            var storedJson = await page.EvaluateAsync<string>(
                "(storageKey) => localStorage.getItem(storageKey) ?? ''",
                StoredReaderSettingsKey);
            var storedSettings = JsonSerializer.Deserialize<ReaderSettings>(storedJson);

            Assert.NotNull(storedSettings);
            Assert.Equal(int.Parse(PersistedFocalPointValue, CultureInfo.InvariantCulture), storedSettings.FocalPointPercent);
            Assert.Equal(double.Parse(PersistedFontValue, CultureInfo.InvariantCulture) / ReaderFontBaselinePixels, storedSettings.FontScale, 2);
            Assert.Equal(double.Parse(PersistedTextWidthValue, CultureInfo.InvariantCulture) / ReaderWidthBaselinePixels, storedSettings.TextWidth, 4);
            Assert.Equal(ReaderTextAlignment.Justify, storedSettings.TextAlignment);

            await page.ReloadAsync();
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await Expect(page.GetByTestId(UiTestIds.Teleprompter.FontSlider)).ToHaveValueAsync(PersistedFontValue);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.WidthSlider)).ToHaveValueAsync(PersistedTextWidthValue);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.FocalSlider)).ToHaveValueAsync(PersistedFocalPointValue);
            await Expect(page.Locator($"#{UiDomIds.Teleprompter.FontLabel}")).ToHaveTextAsync(PersistedFontValue);
            await Expect(page.Locator($"#{UiDomIds.Teleprompter.WidthValue}")).ToHaveTextAsync(PersistedTextWidthValue);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.FocalGuide)).ToHaveAttributeAsync("style", PersistedFocalGuideStyle);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.ClusterWrap))
                .ToHaveAttributeAsync("style", new Regex(Regex.Escape(PersistedFontStyleFragment), RegexOptions.Compiled));
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.ClusterWrap))
                .ToHaveAttributeAsync(BrowserTestConstants.TeleprompterFlow.ReaderTextAlignmentAttribute, PersistedTextAlignmentValue);
        });

    [Fact]
    public Task Teleprompter_BackwardBlockJump_ReversesOutgoingCardDirection() =>
        RunPageAsync(async page =>
        {
            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            var firstCard = page.GetByTestId(UiTestIds.Teleprompter.Card(0));
            var secondCard = page.GetByTestId(UiTestIds.Teleprompter.Card(1));

            await page.GetByTestId(UiTestIds.Teleprompter.NextBlock).ClickAsync();
            await Expect(page.Locator($"#{UiDomIds.Teleprompter.BlockIndicator}"))
                .ToHaveTextAsync(BrowserTestConstants.Regexes.ReaderSecondBlockIndicator);
            await Expect(secondCard).ToHaveClassAsync(ReaderCardActiveClassRegex);

            await page.GetByTestId(UiTestIds.Teleprompter.PreviousBlock).ClickAsync();
            await Expect(page.Locator($"#{UiDomIds.Teleprompter.BlockIndicator}"))
                .ToHaveTextAsync(ReaderFirstBlockIndicator);
            await Expect(firstCard).ToHaveClassAsync(ReaderCardActiveClassRegex);
            await Expect(secondCard).ToHaveClassAsync(
                ReaderCardNextClassRegex,
                new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });

            var secondCardClasses = await secondCard.GetAttributeAsync("class") ?? string.Empty;
            Assert.Contains(ReaderCardNextClass, secondCardClasses, StringComparison.Ordinal);
            Assert.DoesNotContain(ReaderCardActiveClass, secondCardClasses, StringComparison.Ordinal);
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
}
