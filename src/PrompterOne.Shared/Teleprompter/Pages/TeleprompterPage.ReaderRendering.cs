using System.Globalization;

namespace PrompterOne.Shared.Pages;

public partial class TeleprompterPage
{
    private const string ActiveCssClass = "active";
    private const string ReaderCameraTintCssClass = "rd-camera-tint";
    private const string ReaderCardActiveCssClass = "rd-card-active";
    private const string ReaderCardCssClass = "rd-card";
    private const string ReaderCardNextCssClass = "rd-card-next";
    private const string ReaderCardPreviousCssClass = "rd-card-prev";
    private const string ReaderControlButtonCssClass = "rd-ctrl-btn";
    private const string ReaderCountdownCssClass = "rd-countdown";
    private const string ReaderGroupActiveCssClass = "rd-g-active";
    private const string ReaderGroupCssClass = "rd-g";
    private const string ReaderHorizontalGuideCssClass = "rd-guide-h";
    private const string ReaderVerticalGuideCssClass = "rd-guide-v";
    private const string ReaderVerticalGuideLeftCssClass = "rd-guide-v-l";
    private const string ReaderVerticalGuideRightCssClass = "rd-guide-v-r";
    private const string ReaderWordActiveCssClass = "rd-now";
    private const string ReaderWordCssClass = "rd-w";
    private const string ReaderWordReadCssClass = "rd-read";

    private void UpdateReaderDisplayState(bool instantAlignment = false)
    {
        if (_cards.Count == 0)
        {
            _gradientClass = string.Empty;
            _edgeSectionLabel = string.Empty;
            _readerProgressFillWidth = "0%";
            _elapsedLabel = "0:00 / 0:00";
            _screenSubtitle = string.Empty;
            _screenTitle = SessionService.State.Title;
            Shell.ShowTeleprompter(_screenTitle, _screenSubtitle, SessionService.State.ScriptId);
            return;
        }

        _activeReaderCardIndex = Math.Clamp(_activeReaderCardIndex, 0, _cards.Count - 1);
        _activeReaderWordIndex = Math.Clamp(_activeReaderWordIndex, -1, GetCardWordCount(_cards[_activeReaderCardIndex]) - 1);

        var activeCard = _cards[_activeReaderCardIndex];
        _screenTitle = SessionService.State.Title;
        _screenSubtitle = string.Equals(activeCard.SectionName, activeCard.DisplayName, StringComparison.Ordinal)
            ? $"{activeCard.DisplayName} · {activeCard.EmotionLabel}"
            : $"{activeCard.SectionName} · {activeCard.DisplayName}";
        _gradientClass = activeCard.BackgroundClass;
        _edgeSectionLabel = activeCard.DisplayName;
        _readerProgressFillWidth = $"{BuildProgressPercent():0.##}%";
        _elapsedLabel = $"{FormatDurationLabel(GetElapsedMilliseconds())} / {FormatDurationLabel(_totalDurationMilliseconds)}";
        RequestReaderAlignment(instantAlignment);

        Shell.ShowTeleprompter(_screenTitle, _screenSubtitle, SessionService.State.ScriptId);
    }

    private string BuildBlockIndicatorLabel() =>
        _cards.Count == 0
            ? string.Empty
            : $"{_activeReaderCardIndex + 1} / {_cards.Count}";

    private string BuildCameraCssClass() =>
        BuildClassList(_cameraLayer.CssClass, _isReaderCameraActive ? ActiveCssClass : null);

    private string BuildCameraTintCssClass() =>
        BuildClassList(ReaderCameraTintCssClass, _isReaderCameraActive ? ActiveCssClass : null);

    private string BuildCameraButtonCssClass() =>
        BuildClassList(ReaderControlButtonCssClass, _isReaderCameraActive ? ActiveCssClass : null);

    private string BuildCountdownCssClass() =>
        BuildClassList(ReaderCountdownCssClass, _isReaderCountdownActive ? ActiveCssClass : null);

    private string BuildCountdownLabel() => _countdownValue?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    private string BuildFocalGuideCssClass() =>
        BuildClassList(ReaderHorizontalGuideCssClass, _isFocalGuideActive ? ActiveCssClass : null);

    private string BuildFocalGuideStyle() =>
        $"top:{_readerFocalPointPercent.ToString(CultureInfo.InvariantCulture)}%;";

    private string BuildWidthGuideCssClass(bool isLeft) =>
        BuildClassList(
            ReaderVerticalGuideCssClass,
            isLeft ? ReaderVerticalGuideLeftCssClass : ReaderVerticalGuideRightCssClass,
            _areWidthGuidesActive ? ActiveCssClass : null);

    private string BuildWidthGuideStyle(bool isLeft)
    {
        var halfWidth = (_readerTextWidth / 2d).ToString("0.##", CultureInfo.InvariantCulture);
        var sign = isLeft ? '-' : '+';
        return $"left:calc(50% {sign} {halfWidth}px);";
    }

    private string BuildClusterWrapStyle() =>
        $"max-width:{_readerTextWidth.ToString(CultureInfo.InvariantCulture)}px;--rd-font-size:{_readerFontSize.ToString(CultureInfo.InvariantCulture)}px;";

    private string BuildReaderCardCssClass(int index)
    {
        var stateClass = index == _activeReaderCardIndex
            ? ReaderCardActiveCssClass
            : index == _readerTransitionSourceCardIndex
                ? ReaderCardPreviousCssClass
                : index == _preparedReaderCardIndex
                    ? ReaderCardNextCssClass
                    : index < _activeReaderCardIndex
                        ? ReaderCardPreviousCssClass
                        : ReaderCardNextCssClass;
        var transitionClass = _readerCardsWithoutMotionTransition.Contains(index)
            ? ReaderCardNoTransitionCssClass
            : null;

        return BuildClassList(ReaderCardCssClass, stateClass, transitionClass);
    }

    private string BuildReaderGroupCssClass(int cardIndex, int chunkIndex)
    {
        if (cardIndex != _activeReaderCardIndex || _activeReaderWordIndex < 0)
        {
            return ReaderGroupCssClass;
        }

        var groupStartIndex = GetChunkWordStartIndex(cardIndex, chunkIndex);
        var groupWordCount = GetChunkWordCount(cardIndex, chunkIndex);
        var isActiveGroup = _activeReaderWordIndex >= groupStartIndex &&
            _activeReaderWordIndex < groupStartIndex + groupWordCount;

        return BuildClassList(ReaderGroupCssClass, isActiveGroup ? ReaderGroupActiveCssClass : null);
    }

    private string BuildReaderWordCssClass(int cardIndex, int chunkIndex, int wordIndex)
    {
        var group = (ReaderGroupViewModel)_cards[cardIndex].Chunks[chunkIndex];
        var word = group.Words[wordIndex];
        var wordOrdinal = GetChunkWordStartIndex(cardIndex, chunkIndex) + wordIndex;
        var stateClass = cardIndex == _activeReaderCardIndex
            ? wordOrdinal < _activeReaderWordIndex
                ? ReaderWordReadCssClass
                : wordOrdinal == _activeReaderWordIndex
                    ? ReaderWordActiveCssClass
                    : null
            : null;

        return BuildClassList(ReaderWordCssClass, word.CssClass, stateClass);
    }

    private string BuildEdgeSegmentStyle(ReaderCardViewModel card, int index)
    {
        var opacity = index == _activeReaderCardIndex ? "1" : "0.4";
        return $"width:{card.WidthPercentString};opacity:{opacity};background:{card.EdgeColor};";
    }

    private string BuildReaderProgressStyle() => $"width:{_readerProgressFillWidth};";

    private string BuildElapsedLabel() => _elapsedLabel;

    private static string BuildBooleanDataAttribute(bool value) => value ? "true" : "false";

    private double BuildProgressPercent()
    {
        var totalWords = _cards.Sum(GetCardWordCount);
        if (totalWords == 0)
        {
            return 0;
        }

        var wordsBeforeCard = _cards
            .Take(_activeReaderCardIndex)
            .Sum(GetCardWordCount);
        var wordsInCurrentCard = Math.Max(0, _activeReaderWordIndex);
        return (wordsBeforeCard + wordsInCurrentCard) * 100d / totalWords;
    }

    private int GetElapsedMilliseconds()
    {
        var elapsed = _cards
            .Take(_activeReaderCardIndex)
            .Sum(card => card.DurationMilliseconds);

        if (_activeReaderCardIndex >= _cards.Count || _activeReaderWordIndex <= 0)
        {
            return elapsed;
        }

        var activeCard = _cards[_activeReaderCardIndex];
        var remainingWords = _activeReaderWordIndex;

        foreach (var chunk in activeCard.Chunks)
        {
            if (chunk is not ReaderGroupViewModel group)
            {
                continue;
            }

            foreach (var word in group.Words)
            {
                if (remainingWords <= 0)
                {
                    return elapsed;
                }

                elapsed += word.DurationMs + word.PauseAfterMs;
                remainingWords--;
            }
        }

        return elapsed;
    }

    private static int GetCardWordCount(ReaderCardViewModel card) =>
        card.Chunks.Sum(chunk => chunk is ReaderGroupViewModel group ? group.Words.Count : 0);

    private int GetChunkWordStartIndex(int cardIndex, int chunkIndex)
    {
        _ = _cards[cardIndex];
        var total = 0;

        for (var index = 0; index < chunkIndex; index++)
        {
            total += GetChunkWordCount(cardIndex, index);
        }

        return total;
    }

    private int GetChunkWordCount(int cardIndex, int chunkIndex) =>
        _cards[cardIndex].Chunks[chunkIndex] is ReaderGroupViewModel group
            ? group.Words.Count
            : 0;

    private static string FormatDurationLabel(int totalMilliseconds)
    {
        var totalSeconds = Math.Max(0, (int)Math.Round(totalMilliseconds / 1000d, MidpointRounding.AwayFromZero));
        return $"{totalSeconds / 60}:{totalSeconds % 60:00}";
    }

    private static string BuildClassList(params string?[] classNames) =>
        string.Join(' ', classNames.Where(className => !string.IsNullOrWhiteSpace(className)));
}
