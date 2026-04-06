using ManagedCode.Tps;
using PrompterOne.Core.Services.Rsvp;

namespace PrompterOne.Core.Tests;

public sealed class RsvpTextProcessorTimelineTests
{
    private const int PlainTextReadingDefaultSpeed = 250;
    private readonly RsvpTextProcessor _processor = new();

    [Fact]
    public void ParseScript_TpsSampleBuildsPhraseGroupsForLearnTimeline()
    {
        var sample = CoreTestSeedData.CreateDocuments()
            .Single(document => string.Equals(document.Id, CoreTestSeedData.Scripts.SecurityIncidentId, StringComparison.Ordinal));

        var processed = _processor.ParseScript(sample.Text);

        Assert.NotEmpty(processed.AllWords);
        Assert.NotEmpty(processed.Segments);
        Assert.NotEmpty(processed.PhraseGroups);

        var firstPhrase = processed.PhraseGroups[0];
        var secondPhrase = processed.PhraseGroups[1];
        var thirdPhrase = processed.PhraseGroups[2];

        Assert.Equal(RsvpTextProcessorTimelineTestSource.FirstPhraseWord, firstPhrase.Words[0]);
        Assert.Equal(RsvpTextProcessorTimelineTestSource.SecondPhraseWord, secondPhrase.Words[0]);
        Assert.Equal(RsvpTextProcessorTimelineTestSource.ThirdPhraseWord, thirdPhrase.Words[0]);
    }

    [Fact]
    public void ParseScript_PlainTextUsesSdkImplicitSegmentTitleAndReadingDefaultSpeed()
    {
        const string source = "Hello world.\nThis is still plain text.";

        var processed = _processor.ParseScript(source);

        var segment = Assert.Single(processed.Segments);
        Assert.Equal(TpsSpec.DefaultImplicitSegmentName, segment.Title);
        Assert.Equal(PlainTextReadingDefaultSpeed, segment.Speed);
        Assert.Equal("Hello", processed.AllWords[0]);
    }

    [Fact]
    public void ParseScript_TpsSourceDoesNotLeakFrontMatterHeadersOrInlineTagMarkupIntoReadableWords()
    {
        var sample = CoreTestSeedData.CreateDocuments()
            .Single(document => string.Equals(document.Id, CoreTestSeedData.Scripts.DemoId, StringComparison.Ordinal));

        var processed = _processor.ParseScript(sample.Text);

        Assert.NotEmpty(processed.AllWords);
        Assert.Equal("Good", processed.AllWords[0]);
        Assert.Contains("welcome", processed.AllWords, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            processed.AllWords,
            word => word.Contains('[', StringComparison.Ordinal) ||
                    word.Contains(']', StringComparison.Ordinal) ||
                    word.Contains("---", StringComparison.Ordinal) ||
                    word.Contains("title:", StringComparison.OrdinalIgnoreCase) ||
                    word.Contains("140WPM", StringComparison.OrdinalIgnoreCase) ||
                    word.Contains("Speaker:", StringComparison.OrdinalIgnoreCase));
    }
}

internal static class RsvpTextProcessorTimelineTestSource
{
    public const string FirstPhraseWord = "We";
    public const string SecondPhraseWord = "At";
    public const string ThirdPhraseWord = "our";
}
