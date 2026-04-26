using PrompterOne.Core.AI.Models;
using PrompterOne.Shared.Localization;

namespace PrompterOne.Shared.Tools;

internal static class AiSpotlightTeleprompterToolCatalog
{
    public static void AddTo(List<AiSpotlightTool> tools)
    {
        AddReadOnly(tools, AiSpotlightToolNames.TeleprompterStateRead, UiTextKey.HeaderRead, AiSpotlightToolText.TeleprompterStateRead);
        AddMutation(tools, AiSpotlightToolNames.TeleprompterPlay, UiTextKey.TooltipPlayPlayback, AiSpotlightToolText.TeleprompterPlay);
        AddMutation(tools, AiSpotlightToolNames.TeleprompterPause, UiTextKey.TooltipPausePlayback, AiSpotlightToolText.TeleprompterPause);
        AddMutation(tools, AiSpotlightToolNames.TeleprompterPreviousBlock, UiTextKey.TooltipPreviousBlock, AiSpotlightToolText.TeleprompterPreviousBlock);
        AddMutation(tools, AiSpotlightToolNames.TeleprompterNextBlock, UiTextKey.TooltipNextBlock, AiSpotlightToolText.TeleprompterNextBlock);
        AddMutation(tools, AiSpotlightToolNames.TeleprompterTextSizeSet, UiTextKey.TooltipAdjustReaderTextSize, AiSpotlightToolText.TeleprompterTextSizeSet, AiSpotlightToolParameterSets.Percent);
        AddMutation(tools, AiSpotlightToolNames.TeleprompterReadWidthSet, UiTextKey.TooltipAdjustReaderTextWidth, AiSpotlightToolText.TeleprompterReadWidthSet, AiSpotlightToolParameterSets.Percent);
        AddMutation(tools, AiSpotlightToolNames.TeleprompterCameraToggle, UiTextKey.TooltipToggleCameraPreview, AiSpotlightToolText.TeleprompterCameraToggle, idempotent: false);
        AddMutation(tools, AiSpotlightToolNames.TeleprompterRecordingStart, UiTextKey.TeleprompterStartRecordingTooltip, AiSpotlightToolText.TeleprompterRecordingStart);
        AddMutation(tools, AiSpotlightToolNames.TeleprompterRecordingStop, UiTextKey.TeleprompterStopRecordingTooltip, AiSpotlightToolText.TeleprompterRecordingStop);
        AddMutation(tools, AiSpotlightToolNames.TeleprompterMirrorHorizontalToggle, UiTextKey.TooltipMirrorReaderHorizontally, AiSpotlightToolText.TeleprompterMirrorHorizontalToggle, idempotent: false);
        AddMutation(tools, AiSpotlightToolNames.TeleprompterMirrorVerticalToggle, UiTextKey.TooltipMirrorReaderVertically, AiSpotlightToolText.TeleprompterMirrorVerticalToggle, idempotent: false);
        AddMutation(tools, AiSpotlightToolNames.TeleprompterFullscreenToggle, UiTextKey.TooltipToggleBrowserFullscreen, AiSpotlightToolText.TeleprompterFullscreenToggle, idempotent: false);
    }

    private static void AddReadOnly(List<AiSpotlightTool> tools, string name, UiTextKey label, string prompt) =>
        tools.Add(AiSpotlightToolFactory.AgentTool(
            name,
            label,
            UiTextKey.OnboardingTeleprompterBody,
            prompt,
            AiSpotlightToolScopes.Teleprompter));

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
            UiTextKey.OnboardingTeleprompterBody,
            prompt,
            AiSpotlightToolScopes.Teleprompter,
            parameters,
            readOnly: false,
            idempotent: idempotent));
}
