namespace PrompterOne.Core.Models.Workspace;

public sealed record LearnSettings(
    bool HasCustomizedWordsPerMinute = LearnSettingsDefaults.HasCustomizedWordsPerMinute,
    int WordsPerMinute = LearnSettingsDefaults.WordsPerMinute,
    int ContextWords = LearnSettingsDefaults.ContextWords,
    bool IgnoreScriptSpeeds = LearnSettingsDefaults.IgnoreScriptSpeeds,
    bool AutoPlay = LearnSettingsDefaults.AutoPlay,
    bool LoopPlayback = LearnSettingsDefaults.LoopPlayback,
    bool ShowPhrasePreview = LearnSettingsDefaults.ShowPhrasePreview);
