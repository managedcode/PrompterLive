using PrompterOne.Core.Services.Rsvp;

namespace PrompterOne.Core.Tests;

public sealed class RsvpEmotionAnalyzerTests
{
    [Test]
    public void AnalyzeWord_ReturnsExpectedSemanticEmotion()
    {
        var analyzer = new RsvpEmotionAnalyzer();

        Assert.Equal("happy", analyzer.AnalyzeWord("amazing"));
        Assert.Equal("professional", analyzer.AnalyzeWord("performance"));
        Assert.Equal("peaceful", analyzer.AnalyzeWord("serenity"));
    }

    [Test]
    public void UpdateEmotionForWord_TransitionsAndResetsToDefault()
    {
        var analyzer = new RsvpEmotionAnalyzer();

        var changedToExcited = analyzer.UpdateEmotionForWord("awesome");
        Assert.True(changedToExcited);
        Assert.Equal("Excited", analyzer.CurrentEmotion.Name);

        var changedBackToDefault = analyzer.UpdateEmotionForWord("ordinary");

        Assert.True(changedBackToDefault);
        Assert.Equal("Neutral", analyzer.CurrentEmotion.Name);
    }

    [Test]
    public void SetEmotion_IgnoresUnknownKeys()
    {
        var analyzer = new RsvpEmotionAnalyzer();

        analyzer.SetEmotion("calm");
        analyzer.SetEmotion("missing-emotion");

        Assert.Equal("Calm", analyzer.CurrentEmotion.Name);
        Assert.Equal("#4ECDC4", analyzer.CurrentEmotion.ColorHex);
    }
}
