using System.Globalization;
using PrompterOne.Shared.Contracts;

namespace PrompterOne.Shared.Pages;

public partial class TeleprompterPage
{
    private const double ReaderAlignmentChangeThresholdPixels = 0.5d;
    private const string ReaderTextNoTransitionStyle = "transition:none;";

    private readonly Dictionary<int, string> _readerCardTextStyles = [];
    private readonly HashSet<int> _readerCardsPendingTransitionRestore = [];
    private readonly HashSet<int> _readerCardsWithoutTransition = [];
    private bool _pendingReaderAlignment;
    private bool _pendingReaderAlignmentInstant;

    private async Task AlignActiveReaderTextAsync()
    {
        if (!_pendingReaderAlignment || _cards.Count == 0 || _activeReaderCardIndex >= _cards.Count)
        {
            return;
        }

        var instant = _pendingReaderAlignmentInstant;
        _pendingReaderAlignment = false;
        _pendingReaderAlignmentInstant = false;
        await AlignReaderCardTextAsync(
            _activeReaderCardIndex,
            Math.Max(_activeReaderWordIndex, 0),
            neutralizeCard: false,
            rerender: true,
            instantTransition: instant);
    }

    private string BuildReaderCardTextStyle(int cardIndex)
    {
        var hasStyle = _readerCardTextStyles.TryGetValue(cardIndex, out var style);
        var disableTransition = _readerCardsWithoutTransition.Contains(cardIndex);

        if (!hasStyle && !disableTransition)
        {
            return string.Empty;
        }

        if (!hasStyle)
        {
            return ReaderTextNoTransitionStyle;
        }

        var resolvedStyle = style ?? string.Empty;
        return disableTransition
            ? resolvedStyle + ReaderTextNoTransitionStyle
            : resolvedStyle;
    }

    private void RequestReaderAlignment(bool instant = false)
    {
        _pendingReaderAlignment = true;
        _pendingReaderAlignmentInstant |= instant;
    }

    private void ResetReaderAlignmentState()
    {
        _readerCardTextStyles.Clear();
        _readerCardsPendingTransitionRestore.Clear();
        _readerCardsWithoutTransition.Clear();
        _pendingReaderAlignment = true;
        _pendingReaderAlignmentInstant = true;
    }

    private Task PrepareReaderCardAlignmentAsync(int cardIndex, int wordOrdinal) =>
        AlignReaderCardTextAsync(cardIndex, wordOrdinal, neutralizeCard: true, rerender: false, instantTransition: false);

    private Task AlignReaderWordBeforeActivationAsync(int cardIndex, int wordOrdinal) =>
        AlignReaderCardTextAsync(cardIndex, wordOrdinal, neutralizeCard: false, rerender: false, instantTransition: false);

    private async Task RestorePendingReaderTextTransitionsAsync()
    {
        if (_readerCardsPendingTransitionRestore.Count == 0)
        {
            return;
        }

        var cardsToRestore = _readerCardsPendingTransitionRestore.ToArray();
        _readerCardsPendingTransitionRestore.Clear();

        var changed = false;
        foreach (var cardIndex in cardsToRestore)
        {
            changed |= _readerCardsWithoutTransition.Remove(cardIndex);
        }

        if (changed)
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task AlignReaderCardTextAsync(
        int cardIndex,
        int wordOrdinal,
        bool neutralizeCard,
        bool rerender,
        bool instantTransition)
    {
        if (!TryGetAlignmentWordId(cardIndex, wordOrdinal, out var wordId))
        {
            return;
        }

        var offsetPixels = await ReaderInterop.MeasureClusterOffsetAsync(
            UiDomIds.Teleprompter.Stage,
            UiDomIds.Teleprompter.CardText(cardIndex),
            wordId,
            _readerFocalPointPercent,
            neutralizeCard);

        if (!offsetPixels.HasValue)
        {
            return;
        }

        var nextStyle = BuildReaderCardTextStyleValue(offsetPixels.Value);
        var currentStyle = _readerCardTextStyles.TryGetValue(cardIndex, out var existingStyle)
            ? existingStyle
            : string.Empty;
        var currentTransitionMode = _readerCardsWithoutTransition.Contains(cardIndex);
        if (string.Equals(currentStyle, nextStyle, StringComparison.Ordinal) &&
            currentTransitionMode == instantTransition)
        {
            return;
        }

        _readerCardTextStyles[cardIndex] = nextStyle;
        if (instantTransition)
        {
            _readerCardsWithoutTransition.Add(cardIndex);
            _readerCardsPendingTransitionRestore.Add(cardIndex);
        }
        else
        {
            _readerCardsWithoutTransition.Remove(cardIndex);
            _readerCardsPendingTransitionRestore.Remove(cardIndex);
        }

        if (rerender)
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    private bool TryGetAlignmentWordId(int cardIndex, int wordOrdinal, out string wordId)
    {
        wordId = string.Empty;
        if (!TryGetAlignmentWordPosition(cardIndex, wordOrdinal, out var position))
        {
            return false;
        }

        wordId = UiDomIds.Teleprompter.CardWord(position.CardIndex, position.ChunkIndex, position.WordIndex);
        return true;
    }

    private bool TryGetAlignmentWordPosition(int cardIndex, int wordOrdinal, out (int CardIndex, int ChunkIndex, int WordIndex) position)
    {
        position = default;
        if (cardIndex < 0 || cardIndex >= _cards.Count)
        {
            return false;
        }

        var remainingWords = Math.Max(wordOrdinal, 0);
        var chunks = _cards[cardIndex].Chunks;

        for (var chunkIndex = 0; chunkIndex < chunks.Count; chunkIndex++)
        {
            if (chunks[chunkIndex] is not ReaderGroupViewModel group)
            {
                continue;
            }

            for (var wordIndex = 0; wordIndex < group.Words.Count; wordIndex++)
            {
                if (remainingWords == 0)
                {
                    position = (cardIndex, chunkIndex, wordIndex);
                    return true;
                }

                remainingWords--;
            }
        }

        return false;
    }

    private static string BuildReaderCardTextStyleValue(double offsetPixels)
    {
        var roundedOffset = Math.Round(offsetPixels, 2, MidpointRounding.AwayFromZero);
        return Math.Abs(roundedOffset) <= ReaderAlignmentChangeThresholdPixels
            ? string.Empty
            : $"transform:translateY({roundedOffset.ToString("0.##", CultureInfo.InvariantCulture)}px);";
    }
}
