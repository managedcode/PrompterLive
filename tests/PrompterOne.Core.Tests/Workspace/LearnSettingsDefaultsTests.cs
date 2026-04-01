using PrompterOne.Core.Models.Workspace;

namespace PrompterOne.Core.Tests;

public sealed class LearnSettingsDefaultsTests
{
    [Fact]
    public void LearnSettings_DefaultConstructor_UsesWorkspaceDefaults()
    {
        var settings = new LearnSettings();

        Assert.Equal(LearnSettingsDefaults.HasCustomizedWordsPerMinute, settings.HasCustomizedWordsPerMinute);
        Assert.Equal(LearnSettingsDefaults.WordsPerMinute, settings.WordsPerMinute);
        Assert.Equal(LearnSettingsDefaults.ContextWords, settings.ContextWords);
        Assert.Equal(LearnSettingsDefaults.IgnoreScriptSpeeds, settings.IgnoreScriptSpeeds);
        Assert.Equal(LearnSettingsDefaults.AutoPlay, settings.AutoPlay);
        Assert.Equal(LearnSettingsDefaults.LoopPlayback, settings.LoopPlayback);
        Assert.Equal(LearnSettingsDefaults.ShowPhrasePreview, settings.ShowPhrasePreview);
    }
}
