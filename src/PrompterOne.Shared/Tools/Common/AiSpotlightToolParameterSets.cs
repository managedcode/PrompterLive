using PrompterOne.Core.AI.Models;

namespace PrompterOne.Shared.Tools;

internal static class AiSpotlightToolParameterSets
{
    public static readonly IReadOnlyList<ScriptAgentAppToolParameter> Instruction =
        AiSpotlightToolParameters.Of(String(
            AiSpotlightToolParameterNames.Instruction,
            AiSpotlightToolParameterDescriptions.AssistantInstruction));

    public static readonly IReadOnlyList<ScriptAgentAppToolParameter> Workflow =
        AiSpotlightToolParameters.Of(
            String(AiSpotlightToolParameterNames.WorkflowName, AiSpotlightToolParameterDescriptions.WorkflowName),
            String(AiSpotlightToolParameterNames.Instruction, AiSpotlightToolParameterDescriptions.WorkflowInstruction));

    public static readonly IReadOnlyList<ScriptAgentAppToolParameter> Range =
        AiSpotlightToolParameters.Of(
            Integer(AiSpotlightToolParameterNames.Start, AiSpotlightToolParameterDescriptions.StartOffset),
            Integer(AiSpotlightToolParameterNames.End, AiSpotlightToolParameterDescriptions.EndOffset));

    public static readonly IReadOnlyList<ScriptAgentAppToolParameter> Replacement =
        AiSpotlightToolParameters.Of(
            Integer(AiSpotlightToolParameterNames.Start, AiSpotlightToolParameterDescriptions.StartOffset),
            Integer(AiSpotlightToolParameterNames.End, AiSpotlightToolParameterDescriptions.EndOffset),
            String(AiSpotlightToolParameterNames.ReplacementText, AiSpotlightToolParameterDescriptions.ReplacementText),
            String(AiSpotlightToolParameterNames.Reason, AiSpotlightToolParameterDescriptions.EditReason, required: false));

    public static readonly IReadOnlyList<ScriptAgentAppToolParameter> Insertion =
        AiSpotlightToolParameters.Of(
            Integer(AiSpotlightToolParameterNames.Offset, AiSpotlightToolParameterDescriptions.InsertionOffset),
            String(AiSpotlightToolParameterNames.Text, AiSpotlightToolParameterDescriptions.ScriptText),
            String(AiSpotlightToolParameterNames.Reason, AiSpotlightToolParameterDescriptions.EditReason, required: false));

    public static readonly IReadOnlyList<ScriptAgentAppToolParameter> Search =
        AiSpotlightToolParameters.Of(
            String(AiSpotlightToolParameterNames.Query, AiSpotlightToolParameterDescriptions.SearchQuery),
            Boolean(AiSpotlightToolParameterNames.MatchCase, AiSpotlightToolParameterDescriptions.MatchCase, required: false));

    public static readonly IReadOnlyList<ScriptAgentAppToolParameter> FindReplace =
        AiSpotlightToolParameters.Of(
            String(AiSpotlightToolParameterNames.Query, AiSpotlightToolParameterDescriptions.SearchQuery),
            String(AiSpotlightToolParameterNames.ReplacementText, AiSpotlightToolParameterDescriptions.ReplacementText),
            Boolean(AiSpotlightToolParameterNames.MatchCase, AiSpotlightToolParameterDescriptions.MatchCase, required: false));

    public static readonly IReadOnlyList<ScriptAgentAppToolParameter> TpsCue =
        AiSpotlightToolParameters.Of(
            String(AiSpotlightToolParameterNames.Cue, AiSpotlightToolParameterDescriptions.TpsCue),
            String(AiSpotlightToolParameterNames.Value, AiSpotlightToolParameterDescriptions.TpsCueValue, required: false));

    public static readonly IReadOnlyList<ScriptAgentAppToolParameter> GraphQuery =
        AiSpotlightToolParameters.Of(
            String(AiSpotlightToolParameterNames.Query, AiSpotlightToolParameterDescriptions.GraphQuery),
            String(AiSpotlightToolParameterNames.Kind, AiSpotlightToolParameterDescriptions.GraphKind, required: false));

    public static readonly IReadOnlyList<ScriptAgentAppToolParameter> GraphNode =
        AiSpotlightToolParameters.Of(String(
            AiSpotlightToolParameterNames.NodeId,
            AiSpotlightToolParameterDescriptions.GraphNodeId));

    public static readonly IReadOnlyList<ScriptAgentAppToolParameter> GraphAnnotation =
        AiSpotlightToolParameters.Of(
            String(AiSpotlightToolParameterNames.Label, AiSpotlightToolParameterDescriptions.AnnotationLabel),
            String(AiSpotlightToolParameterNames.Kind, AiSpotlightToolParameterDescriptions.AnnotationKind),
            String(AiSpotlightToolParameterNames.SourceRange, AiSpotlightToolParameterDescriptions.SourceRange, required: false));

    public static readonly IReadOnlyList<ScriptAgentAppToolParameter> LibrarySearch =
        AiSpotlightToolParameters.Of(String(
            AiSpotlightToolParameterNames.Query,
            AiSpotlightToolParameterDescriptions.ScriptSearchQuery));

    public static readonly IReadOnlyList<ScriptAgentAppToolParameter> Script =
        AiSpotlightToolParameters.Of(String(
            AiSpotlightToolParameterNames.ScriptId,
            AiSpotlightToolParameterDescriptions.ScriptId));

    public static readonly IReadOnlyList<ScriptAgentAppToolParameter> MediaDevice =
        AiSpotlightToolParameters.Of(String(
            AiSpotlightToolParameterNames.DeviceId,
            AiSpotlightToolParameterDescriptions.MediaDeviceId));

    public static readonly IReadOnlyList<ScriptAgentAppToolParameter> MediaAnalysis =
        AiSpotlightToolParameters.Of(
            String(AiSpotlightToolParameterNames.SourceId, AiSpotlightToolParameterDescriptions.MediaSourceId),
            String(AiSpotlightToolParameterNames.Instruction, AiSpotlightToolParameterDescriptions.AnalysisInstruction));

    public static readonly IReadOnlyList<ScriptAgentAppToolParameter> MicrophoneGain =
        AiSpotlightToolParameters.Of(
            String(AiSpotlightToolParameterNames.DeviceId, AiSpotlightToolParameterDescriptions.MicrophoneDeviceId),
            Integer(AiSpotlightToolParameterNames.GainPercent, AiSpotlightToolParameterDescriptions.GainPercent));

    public static readonly IReadOnlyList<ScriptAgentAppToolParameter> MicrophoneDelay =
        AiSpotlightToolParameters.Of(
            String(AiSpotlightToolParameterNames.DeviceId, AiSpotlightToolParameterDescriptions.MicrophoneDeviceId),
            Integer(AiSpotlightToolParameterNames.DelayMs, AiSpotlightToolParameterDescriptions.DelayMs));

    public static readonly IReadOnlyList<ScriptAgentAppToolParameter> SettingValue =
        AiSpotlightToolParameters.Of(
            String(AiSpotlightToolParameterNames.Section, AiSpotlightToolParameterDescriptions.SettingsSection),
            String(AiSpotlightToolParameterNames.Key, AiSpotlightToolParameterDescriptions.SettingsKey),
            String(AiSpotlightToolParameterNames.Value, AiSpotlightToolParameterDescriptions.SettingsValue));

    public static readonly IReadOnlyList<ScriptAgentAppToolParameter> DeviceSelection = MediaDevice;

    public static readonly IReadOnlyList<ScriptAgentAppToolParameter> RsvpSpeed =
        AiSpotlightToolParameters.Of(Integer(
            AiSpotlightToolParameterNames.WordsPerMinute,
            AiSpotlightToolParameterDescriptions.RsvpWordsPerMinute));

    public static readonly IReadOnlyList<ScriptAgentAppToolParameter> PrepNote =
        AiSpotlightToolParameters.Of(String(
            AiSpotlightToolParameterNames.Text,
            AiSpotlightToolParameterDescriptions.PrepNoteText));

    public static readonly IReadOnlyList<ScriptAgentAppToolParameter> Percent =
        AiSpotlightToolParameters.Of(Integer(
            AiSpotlightToolParameterNames.Percent,
            AiSpotlightToolParameterDescriptions.Percent));

    public static readonly IReadOnlyList<ScriptAgentAppToolParameter> Transport =
        AiSpotlightToolParameters.Of(
            String(AiSpotlightToolParameterNames.TransportId, AiSpotlightToolParameterDescriptions.TransportId),
            String(AiSpotlightToolParameterNames.Platform, AiSpotlightToolParameterDescriptions.TransportPlatform),
            String(AiSpotlightToolParameterNames.Url, AiSpotlightToolParameterDescriptions.TransportUrl, required: false));

    public static readonly IReadOnlyList<ScriptAgentAppToolParameter> StreamTarget =
        AiSpotlightToolParameters.Of(
            String(AiSpotlightToolParameterNames.TargetId, AiSpotlightToolParameterDescriptions.StreamTargetId),
            String(AiSpotlightToolParameterNames.Platform, AiSpotlightToolParameterDescriptions.StreamingPlatform),
            String(AiSpotlightToolParameterNames.StreamUrl, AiSpotlightToolParameterDescriptions.StreamUrl, required: false),
            String(AiSpotlightToolParameterNames.StreamKey, AiSpotlightToolParameterDescriptions.StreamKey, required: false));

    public static readonly IReadOnlyList<ScriptAgentAppToolParameter> Source =
        AiSpotlightToolParameters.Of(String(
            AiSpotlightToolParameterNames.SourceId,
            AiSpotlightToolParameterDescriptions.GoLiveSourceId));

    public static readonly IReadOnlyList<ScriptAgentAppToolParameter> Layout =
        AiSpotlightToolParameters.Of(String(
            AiSpotlightToolParameterNames.Layout,
            AiSpotlightToolParameterDescriptions.ProgramLayout));

    private static ScriptAgentAppToolParameter Boolean(string name, string description, bool required = true) =>
        AiSpotlightToolParameters.Boolean(name, description, required);

    private static ScriptAgentAppToolParameter Integer(string name, string description, bool required = true) =>
        AiSpotlightToolParameters.Integer(name, description, required);

    private static ScriptAgentAppToolParameter String(string name, string description, bool required = true) =>
        AiSpotlightToolParameters.String(name, description, required);
}
