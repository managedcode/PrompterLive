namespace PrompterOne.Shared.Tools;

internal static class AiSpotlightToolParameterNames
{
    public const string Cue = "cue";
    public const string DelayMs = "delayMs";
    public const string DeviceId = "deviceId";
    public const string End = "end";
    public const string GainPercent = "gainPercent";
    public const string Instruction = "instruction";
    public const string Key = "key";
    public const string Kind = "kind";
    public const string Label = "label";
    public const string Layout = "layout";
    public const string MatchCase = "matchCase";
    public const string NodeId = "nodeId";
    public const string Offset = "offset";
    public const string Percent = "percent";
    public const string Platform = "platform";
    public const string Query = "query";
    public const string Reason = "reason";
    public const string ReplacementText = "replacementText";
    public const string ScriptId = "scriptId";
    public const string Section = "section";
    public const string SourceId = "sourceId";
    public const string SourceRange = "sourceRange";
    public const string Start = "start";
    public const string StreamKey = "streamKey";
    public const string StreamUrl = "streamUrl";
    public const string TargetId = "targetId";
    public const string Text = "text";
    public const string TransportId = "transportId";
    public const string Url = "url";
    public const string Value = "value";
    public const string WorkflowName = "workflowName";
    public const string WordsPerMinute = "wpm";
}

internal static class AiSpotlightToolParameterDescriptions
{
    public const string AnalysisInstruction = "What should be analyzed.";
    public const string AnnotationKind = "Annotation entity kind.";
    public const string AnnotationLabel = "Annotation label.";
    public const string AssistantInstruction = "Natural-language user instruction for the assistant.";
    public const string DelayMs = "Audio delay in milliseconds.";
    public const string EditReason = "User-facing reason for the edit.";
    public const string EndOffset = "Exclusive UTF-16 end offset.";
    public const string GainPercent = "Input gain percentage.";
    public const string GoLiveSourceId = "Go Live source identifier.";
    public const string GraphKind = "Optional graph entity kind.";
    public const string GraphNodeId = "Graph node identifier.";
    public const string GraphQuery = "Graph search query.";
    public const string InsertionOffset = "UTF-16 insertion offset.";
    public const string MatchCase = "Whether matching is case-sensitive.";
    public const string MediaDeviceId = "Browser media device identifier.";
    public const string MediaSourceId = "Camera, microphone, stream, or recording source identifier.";
    public const string MicrophoneDeviceId = "Browser microphone device identifier.";
    public const string Percent = "Target percentage value.";
    public const string PrepNoteText = "Prep note text for the current Learn section.";
    public const string ProgramLayout = "Program layout preset.";
    public const string ReplacementText = "Replacement script text.";
    public const string RsvpWordsPerMinute = "Target RSVP words per minute.";
    public const string ScriptId = "Script document identifier.";
    public const string ScriptSearchQuery = "Script title or content query.";
    public const string ScriptText = "Script text to insert.";
    public const string SearchQuery = "Search text or regular expression.";
    public const string SettingsKey = "Settings field key.";
    public const string SettingsSection = "Settings section name.";
    public const string SettingsValue = "Settings value.";
    public const string SourceRange = "Source range connected to the annotation.";
    public const string StartOffset = "Inclusive UTF-16 start offset.";
    public const string StreamKey = "Secret stream key.";
    public const string StreamTargetId = "Stream target identifier.";
    public const string StreamUrl = "RTMP, RTMPS, or relay endpoint.";
    public const string StreamingPlatform = "Streaming platform name.";
    public const string TpsCue = "TPS cue or tag name.";
    public const string TpsCueValue = "TPS cue value.";
    public const string TransportId = "Transport connection identifier.";
    public const string TransportPlatform = "Transport platform name.";
    public const string TransportUrl = "Transport URL or room URL.";
    public const string WorkflowInstruction = "Natural-language user instruction for the workflow.";
    public const string WorkflowName = "Known PrompterOne agent workflow name.";
}
