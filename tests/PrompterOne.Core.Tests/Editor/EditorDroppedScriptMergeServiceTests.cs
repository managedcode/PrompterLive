using PrompterOne.Core.Models.Editor;
using PrompterOne.Core.Services.Editor;

namespace PrompterOne.Core.Tests;

public sealed class EditorDroppedScriptMergeServiceTests
{
    private const string ExistingDraft =
        """
        ## [Existing|140WPM|Professional]
        ### [Opening|140WPM|Warm]
        Keep the current draft.
        """;
    private const string FirstDroppedBody =
        """
        ## [Dropped One|150WPM|Focused]
        ### [Closing|150WPM|Professional]
        First dropped file.
        """;
    private const string SecondDroppedBody =
        """
        ## [Dropped Two|160WPM|Focused]
        ### [Closing|160WPM|Professional]
        Second dropped file.
        """;

    private readonly EditorDroppedScriptMergeService _service = new();

    [Test]
    public void Merge_WhenExistingDraftIsEmpty_ReplacesTextAndMovesSelectionToEnd()
    {
        var result = _service.Merge(
            string.Empty,
            [FirstDroppedBody]);

        Assert.True(result.ReplacedExistingText);
        Assert.Equal(FirstDroppedBody, result.Text);
        Assert.Equal(
            new EditorSelectionRange(FirstDroppedBody.Length, FirstDroppedBody.Length),
            result.Selection);
    }

    [Test]
    public void Merge_WhenExistingDraftHasContent_AppendsDroppedBodyWithSingleBlankGap()
    {
        var result = _service.Merge(
            $"{ExistingDraft}\n\n",
            [FirstDroppedBody]);
        var expectedText = string.Concat(ExistingDraft, "\n\n", FirstDroppedBody);

        Assert.False(result.ReplacedExistingText);
        Assert.Equal(expectedText, result.Text);
        Assert.Equal(
            new EditorSelectionRange(expectedText.Length, expectedText.Length),
            result.Selection);
    }

    [Test]
    public void Merge_WhenMultipleFilesAreDropped_PreservesDropOrder()
    {
        var result = _service.Merge(
            string.Empty,
            [SecondDroppedBody, FirstDroppedBody]);
        var expectedText = string.Concat(SecondDroppedBody, "\n\n", FirstDroppedBody);

        Assert.Equal(expectedText, result.Text);
    }
}
