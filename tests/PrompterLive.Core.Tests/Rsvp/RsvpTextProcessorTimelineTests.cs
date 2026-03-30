using PrompterLive.Core.Services.Rsvp;
using PrompterLive.Core.Samples;

namespace PrompterLive.Core.Tests;

public sealed class RsvpTextProcessorTimelineTests
{
    private readonly RsvpTextProcessor _processor = new();

    [Fact]
    public void ParseScript_TpsSampleBuildsPhraseGroupsForLearnTimeline()
    {
        var sample = SampleScriptCatalog.GetById(SampleScriptCatalog.SecuritySampleId);

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
}

internal static class RsvpTextProcessorTimelineTestSource
{
    public const string FirstPhraseWord = "We";
    public const string SecondPhraseWord = "At";
    public const string ThirdPhraseWord = "our";
}
