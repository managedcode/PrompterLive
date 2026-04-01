namespace PrompterOne.Core.Models.Workspace;

public sealed record LearnSettings(
    int WordsPerMinute = 300,
    int ContextWords = 2,
    bool IgnoreScriptSpeeds = false,
    bool AutoPlay = false,
    bool LoopPlayback = false,
    bool ShowPhrasePreview = true);
