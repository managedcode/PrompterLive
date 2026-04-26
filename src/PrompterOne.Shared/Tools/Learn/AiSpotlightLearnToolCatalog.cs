using PrompterOne.Core.AI.Models;
using PrompterOne.Shared.Localization;

namespace PrompterOne.Shared.Tools;

internal static class AiSpotlightLearnToolCatalog
{
    public static void AddTo(List<AiSpotlightTool> tools)
    {
        AddReadOnly(tools, AiSpotlightToolNames.LearnStateRead, UiTextKey.HeaderLearn, AiSpotlightToolText.LearnStateRead);
        AddMutation(tools, AiSpotlightToolNames.LearnPlay, UiTextKey.TooltipPlayPlayback, AiSpotlightToolText.LearnPlay);
        AddMutation(tools, AiSpotlightToolNames.LearnPause, UiTextKey.TooltipPausePlayback, AiSpotlightToolText.LearnPause);
        AddMutation(tools, AiSpotlightToolNames.LearnStepBackward, UiTextKey.TooltipBackOneWord, AiSpotlightToolText.LearnStepBackward);
        AddMutation(tools, AiSpotlightToolNames.LearnStepForward, UiTextKey.TooltipForwardOneWord, AiSpotlightToolText.LearnStepForward);
        AddMutation(tools, AiSpotlightToolNames.LearnSpeedSet, UiTextKey.LearnWpm, AiSpotlightToolText.LearnSpeedSet, AiSpotlightToolParameterSets.RsvpSpeed);
        AddMutation(tools, AiSpotlightToolNames.LearnLoopToggle, UiTextKey.TooltipLoopPlaybackOn, AiSpotlightToolText.LearnLoopToggle, idempotent: false);
        AddReadOnly(tools, AiSpotlightToolNames.LearnNotesList, UiTextKey.LearnNotesTitle, AiSpotlightToolText.LearnNotesList);
        AddMutation(tools, AiSpotlightToolNames.LearnNoteAdd, UiTextKey.LearnNotesSave, AiSpotlightToolText.LearnNoteAdd, AiSpotlightToolParameterSets.PrepNote);
    }

    private static void AddReadOnly(List<AiSpotlightTool> tools, string name, UiTextKey label, string prompt) =>
        tools.Add(AiSpotlightToolFactory.AgentTool(
            name,
            label,
            UiTextKey.OnboardingLearnBody,
            prompt,
            AiSpotlightToolScopes.Learn));

    private static void AddMutation(
        List<AiSpotlightTool> tools,
        string name,
        UiTextKey label,
        string prompt,
        IReadOnlyList<ScriptAgentAppToolParameter>? parameters = null,
        bool idempotent = true) =>
        tools.Add(AiSpotlightToolFactory.AgentTool(
            name,
            label,
            UiTextKey.OnboardingLearnBody,
            prompt,
            AiSpotlightToolScopes.Learn,
            parameters,
            readOnly: false,
            idempotent: idempotent));
}
