namespace PrompterOne.Shared.Tools;

internal static class AiSpotlightToolNames
{
    public const string AskContext = "ask_context";
    public const string ContextGet = "context_get";
    public const string ToolsList = "tools_list";
    public const string AgentsList = "agents_list";
    public const string AgentWorkflowsList = "agent_workflows_list";
    public const string AgentContextInspect = "agent_context_inspect";
    public const string AgentOutputsCompare = "agent_outputs_compare";
    public const string AgentWorkflowsRunningList = "agent_workflows_running_list";
    public const string AgentWorkflowHistoryRead = "agent_workflow_history_read";
    public const string AgentWorkflowCancel = "agent_workflow_cancel";
    public const string AgentSpawnScript = "agent_spawn_script";
    public const string WorkflowRunWriter = "workflow_run_writer";
    public const string WorkflowRunReviewer = "workflow_run_reviewer";
    public const string WorkflowRunGroupReview = "workflow_run_group_review";

    public const string NavLibrary = "nav_library";
    public const string NavEditor = "nav_editor";
    public const string NavLearn = "nav_learn";
    public const string NavPrep = "nav_prep";
    public const string NavTeleprompter = "nav_teleprompter";
    public const string NavGoLive = "nav_go_live";
    public const string NavSettings = "nav_settings";
    public const string ScriptNew = "script_new";

    public const string SettingsCloud = "settings_cloud";
    public const string SettingsFiles = "settings_files";
    public const string SettingsCameras = "settings_cameras";
    public const string SettingsMicrophones = "settings_microphones";
    public const string SettingsStreaming = "settings_streaming";
    public const string SettingsRecording = "settings_recording";
    public const string SettingsAi = "settings_ai";
    public const string SettingsAppearance = "settings_appearance";
    public const string SettingsLanguage = "settings_language";
    public const string SettingsShortcuts = "settings_shortcuts";
    public const string SettingsFeedback = "settings_feedback";
    public const string SettingsAbout = "settings_about";
    public const string SettingsSectionsList = "settings_sections_list";
    public const string SettingRead = "setting_read";
    public const string SettingsMediaDefaultsRead = "settings_media_defaults_read";
    public const string SettingsRecordingProfileRead = "settings_recording_profile_read";
    public const string SettingUpdate = "setting_update";
    public const string SettingReset = "setting_reset";
    public const string SettingsDefaultCameraSet = "settings_default_camera_set";
    public const string SettingsDefaultMicrophoneSet = "settings_default_microphone_set";
    public const string SettingsMicrophoneEnable = "settings_microphone_enable";
    public const string SettingsMicrophoneDisable = "settings_microphone_disable";
    public const string SettingsRecordingProfileUpdate = "settings_recording_profile_update";
    public const string SettingsAiSecretUpdate = "settings_ai_secret_update";
    public const string SettingsCloudSecretUpdate = "settings_cloud_secret_update";

    public const string ScriptImport = "script_import";
    public const string ScriptExport = "script_export";
    public const string ScriptDuplicate = "script_duplicate";
    public const string ScriptDelete = "script_delete";
    public const string EditorSelectionRead = "editor_selection_read";
    public const string ScriptRead = "script_read";
    public const string ScriptMetadataRead = "script_metadata_read";
    public const string EditorCursorContextRead = "editor_cursor_context_read";
    public const string EditorRangeRead = "editor_range_read";
    public const string EditorFind = "editor_find";
    public const string ScriptOutlineRead = "script_outline_read";
    public const string ScriptHistoryOpen = "script_history_open";
    public const string ScriptSave = "script_save";
    public const string ScriptSplit = "script_split";
    public const string EditorInsert = "editor_insert";
    public const string EditorReplace = "editor_replace";
    public const string EditorFindReplace = "editor_find_replace";
    public const string TpsCueInsert = "tps_cue_insert";
    public const string TpsSelectionWrap = "tps_selection_wrap";
    public const string EditorUndo = "editor_undo";
    public const string EditorRedo = "editor_redo";
    public const string EditorRangeDelete = "editor_range_delete";
    public const string ScriptRevisionRestore = "script_revision_restore";
    public const string EditorSelectionRewrite = "editor_selection_rewrite";
    public const string EditorSelectionToTps = "editor_selection_to_tps";

    public const string GraphInspect = "graph_inspect";
    public const string GraphOpen = "graph_open";
    public const string GraphSummaryRead = "graph_summary_read";
    public const string GraphSearch = "graph_search";
    public const string GraphFilter = "graph_filter";
    public const string GraphNodeRead = "graph_node_read";
    public const string GraphNeighborsRead = "graph_neighbors_read";
    public const string GraphNodeHighlight = "graph_node_highlight";
    public const string GraphSectionExplain = "graph_section_explain";
    public const string GraphExport = "graph_export";
    public const string GraphDiff = "graph_diff";
    public const string GraphBuild = "graph_build";
    public const string GraphRebuild = "graph_rebuild";
    public const string GraphAnnotationAdd = "graph_annotation_add";
    public const string GraphAnnotationUpdate = "graph_annotation_update";

    public const string LibraryScriptsList = "library_scripts_list";
    public const string LibraryScriptsSearch = "library_scripts_search";
    public const string LibraryScriptMetadataRead = "library_script_metadata_read";
    public const string LibraryFoldersList = "library_folders_list";
    public const string LibraryFolderCreate = "library_folder_create";
    public const string LibraryScriptRename = "library_script_rename";
    public const string LibraryScriptMove = "library_script_move";
    public const string LibraryScriptDelete = "library_script_delete";

    public const string MediaDevicesList = "media_devices_list";
    public const string MediaPermissionsQuery = "media_permissions_query";
    public const string MediaCapabilitiesRead = "media_capabilities_read";
    public const string MediaCameraPreviewStateRead = "media_camera_preview_state_read";
    public const string MediaAudioOutputsList = "media_audio_outputs_list";
    public const string MediaMicrophoneLevelsRead = "media_microphone_levels_read";
    public const string MediaCameraPreviewAnalyze = "media_camera_preview_analyze";
    public const string MediaMicrophoneAudioAnalyze = "media_microphone_audio_analyze";
    public const string MediaRecordingAnalyze = "media_recording_analyze";
    public const string MediaDevicesRefresh = "media_devices_refresh";
    public const string MediaCameraAccessRequest = "media_camera_access_request";
    public const string MediaMicrophoneAccessRequest = "media_microphone_access_request";
    public const string MediaCameraPreviewStart = "media_camera_preview_start";
    public const string MediaCameraPreviewStop = "media_camera_preview_stop";
    public const string MediaCameraMirrorToggle = "media_camera_mirror_toggle";
    public const string MediaMicrophoneMute = "media_microphone_mute";
    public const string MediaMicrophoneUnmute = "media_microphone_unmute";
    public const string MediaMicrophoneGainUpdate = "media_microphone_gain_update";
    public const string MediaMicrophoneDelayUpdate = "media_microphone_delay_update";
    public const string MediaAudioOutputSelect = "media_audio_output_select";
    public const string MediaScreenCaptureRequest = "media_screen_capture_request";

    public const string LearnStateRead = "learn_state_read";
    public const string LearnPlay = "learn_play";
    public const string LearnPause = "learn_pause";
    public const string LearnStepBackward = "learn_step_backward";
    public const string LearnStepForward = "learn_step_forward";
    public const string LearnSpeedSet = "learn_speed_set";
    public const string LearnLoopToggle = "learn_loop_toggle";
    public const string LearnNotesList = "learn_notes_list";
    public const string LearnNoteAdd = "learn_note_add";

    public const string TeleprompterStateRead = "teleprompter_state_read";
    public const string TeleprompterPlay = "teleprompter_play";
    public const string TeleprompterPause = "teleprompter_pause";
    public const string TeleprompterPreviousBlock = "teleprompter_previous_block";
    public const string TeleprompterNextBlock = "teleprompter_next_block";
    public const string TeleprompterTextSizeSet = "teleprompter_text_size_set";
    public const string TeleprompterReadWidthSet = "teleprompter_read_width_set";
    public const string TeleprompterCameraToggle = "teleprompter_camera_toggle";
    public const string TeleprompterMirrorHorizontalToggle = "teleprompter_mirror_horizontal_toggle";
    public const string TeleprompterMirrorVerticalToggle = "teleprompter_mirror_vertical_toggle";
    public const string TeleprompterFullscreenToggle = "teleprompter_fullscreen_toggle";

    public const string StreamGoLiveStateRead = "stream_go_live_state_read";
    public const string StreamTransportsList = "stream_transports_list";
    public const string StreamTargetsList = "stream_targets_list";
    public const string StreamTransportHealthRead = "stream_transport_health_read";
    public const string StreamRecordingArtifactsList = "stream_recording_artifacts_list";
    public const string StreamScenesList = "stream_scenes_list";
    public const string StreamTargetValidate = "stream_target_validate";
    public const string StreamHealthAnalyze = "stream_health_analyze";
    public const string StreamProgramVideoAnalyze = "stream_program_video_analyze";
    public const string StreamProgramAudioAnalyze = "stream_program_audio_analyze";
    public const string StreamLiveKitTransportConfigure = "stream_livekit_transport_configure";
    public const string StreamVdoNinjaTransportConfigure = "stream_vdo_ninja_transport_configure";
    public const string StreamYouTubeKeyConfigure = "stream_youtube_key_configure";
    public const string StreamTwitchKeyConfigure = "stream_twitch_key_configure";
    public const string StreamCustomRtmpConfigure = "stream_custom_rtmp_configure";
    public const string StreamTargetBind = "stream_target_bind";
    public const string StreamTargetRemove = "stream_target_remove";
    public const string StreamDestinationArm = "stream_destination_arm";
    public const string StreamDestinationUnarm = "stream_destination_unarm";
    public const string StreamGoLiveSourceSelect = "stream_go_live_source_select";
    public const string StreamGoLiveLayoutSet = "stream_go_live_layout_set";
    public const string StreamGoLiveAudioMixUpdate = "stream_go_live_audio_mix_update";
    public const string StreamGoLiveSceneAdd = "stream_go_live_scene_add";
    public const string StreamGoLiveSceneReorder = "stream_go_live_scene_reorder";
    public const string StreamGoLiveSceneRemove = "stream_go_live_scene_remove";
    public const string StreamSourceTakeToAir = "stream_source_take_to_air";
    public const string StreamRecordingStart = "stream_recording_start";
    public const string StreamRecordingStop = "stream_recording_stop";
    public const string StreamStart = "stream_start";
    public const string StreamStop = "stream_stop";

    public const string HotkeyPrefix = "hotkey_";

    public static string Hotkey(string definitionId) =>
        string.Concat(HotkeyPrefix, ToolNamePart(definitionId));

    private static string ToolNamePart(string value) =>
        value.Replace('-', '_');
}
