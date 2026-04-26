using System.Text.RegularExpressions;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Storage;
using PrompterOne.Testing;

namespace PrompterOne.Web.UITests;

internal static partial class BrowserTestConstants
{
    public static class Viewport
    {
        public const int DefaultHeight = 768;
        public const int DefaultWidth = 1366;
    }

    public static class Html
    {
        public const string AriaPressedAttribute = "aria-pressed";
        public const string AriaPressedFalseValue = "false";
        public const string AriaPressedTrueValue = "true";
        public const string DataIconKindAttribute = "data-icon-kind";
        public const string DataTestAttribute = "data-test";
        public const string PlaceholderAttribute = "placeholder";
        public const string ValueAttribute = "value";
    }

    public static class State
    {
        public const string ActiveAttribute = "data-active";
        public const string EnabledAttribute = "data-enabled";
        public const string ExpandedAttribute = "data-expanded";
        public const string VisibleAttribute = "data-visible";
        public const string ActiveValue = "active";
        public const string InactiveValue = "inactive";
        public const string EnabledValue = "true";
        public const string DisabledValue = "false";
        public const string OpenValue = "open";
        public const string ClosedValue = "closed";
        public const string VisibleValue = "true";
        public const string HiddenValue = "false";
    }

    public static class Scripts
    {
        public const string DemoId = "test-product-launch-script";
        public const string HugeDraftId = "test-huge-editor-draft-script";
        public const string LargeDraftId = "test-large-editor-draft-script";
        public const string LeadershipId = "test-ted-leadership-script";
        public const string LearnWpmBoundaryId = "test-learn-wpm-boundary-script";
        public const string QuantumId = "test-quantum-computing-script";
        public const string ReaderTimingId = "test-reader-timing-script";
        public const string SecurityIncidentId = "test-security-incident-script";
        public const string SpeedOffsetsId = "test-tps-speed-offsets-script";
        public const string TpsCueMatrixId = "test-tps-cue-matrix-script";
        public const string MissingId = "test-missing-script-id";
        public const string HugeDraftTitle = "Huge Editor Performance Probe";
        public const string LargeDraftTitle = "Large Editor Performance Probe";
        public const string ProductLaunchTitle = "Product Launch";
        public const string LeadershipTitle = "TED: Leadership";
        public const string LearnWpmBoundaryTitle = "Learn WPM Boundary Probe";
        public const string IntroSubtitle = "Intro";
        public const string QuantumTitle = "Quantum Computing";
        public const string ReaderTimingTitle = "Reader Timing Probe";
        public const string SecurityIncidentTitle = "Security Incident";
        public const string SpeedOffsetsTitle = "TPS Speed Offsets";
        public const string TpsCueMatrixTitle = "TPS Cue Matrix";
        public const string UntitledTitle = "Untitled Script";
    }

    public static class Learn
    {
        public const string ProgressLabelPattern = @"^\s*(?<current>\d+)\s*/\s*(?<total>\d+)\b";
        public const string DemoProbeWord = "believe";
        public const int DemoProbeStepLimit = 12;
        public const string DemoContextLayoutProbeWord = "solution";
        public const int DemoContextLayoutProbeStepLimit = 80;
        public const string DemoFocusStackProbeWord = "introducing";
        public const int DemoFocusStackProbeStepLimit = 80;
        public const string DemoLeftContextFirstWord = "introducing";
        public const string DemoLeftContextSecondWord = "a";
        public const string DemoRightContextFirstWord = "that";
        public const string DemoRightContextSecondWord = "will";
        public const int DemoViewportHeight = 899;
        public const int DemoViewportWidth = 1598;
        public const string EndOfScriptText = "End of script.";
        public const string FasterPlaybackSpeedText = "300";
        public const string LeadershipCurrentSentencePreviewText =
            "It begins with the moment you decide that someone else's progress matters as much as your own";
        public const string LeadershipCleanSentencePreviewText =
            "In uncertain times teams do not need louder instructions";
        public const string LeadershipCleanSentenceProbeWord = "uncertain";
        public const int LeadershipCleanSentenceProbeStepLimit = 40;
        public const string LeadershipLeftContextWord = "In";
        public const string LeadershipPreviewProbeWord = "that";
        public const int LeadershipPreviewProbeStepLimit = 16;
        public const string LeadershipRightContextFirstWord = "times";
        public const string LeadershipRightContextSecondWord = "teams";
        public const int LeadershipViewportHeight = 768;
        public const int LeadershipViewportWidth = 1366;
        public const int MaxLeadershipVisibleContextWordGapPx = 120;
        public const string LongWordProbeWord = "transformative";
        public const int LongWordProbeStepLimit = 20;
        public const int MaxDemoVisibleContextWordGapPx = 72;
        public const double MaxFocusWordSlackPx = 4;
        public const double MaxFocusWordOverflowPx = 0.5;
        public const double MaxRailClipPx = 0.5;
        public const string MidFlowWord = "this";
        public const int MinimumPlaybackAdvanceDeltaWords = 1;
        public const string NextPhraseFragment = "our monitoring systems detected unauthorized";
        public const int PlaybackSpeedIncreaseClicks = 5;
        public const string QuantumProbeWord = "intuition";
        public const int QuantumProbeStepLimit = 12;
        public const int QuantumViewportHeight = 882;
        public const int QuantumViewportWidth = 1797;
        public const int MaxQuantumVisibleContextWordGapPx = 72;
        public const string SecurityIncidentProbeWord = "unauthorized";
        public const int SecurityIncidentProbeStepLimit = 20;
        public const int SecurityIncidentViewportHeight = 882;
        public const int SecurityIncidentViewportWidth = 1797;
        public const int MaxVisibleContextWordGapPx = 100;
        public const string OverlapProbeWord = "instructions";
        public const int OverlapProbeStepLimit = 40;
        public const int OverlapViewportHeight = 766;
        public const int OverlapViewportWidth = 1082;
        public const int MidFlowStepLarge = 5;
        public const int MidFlowStepSmall = 2;
        public const int ContextWordCount = 2;
        public const string PauseBoundaryProbeWord = "will";
        public const int PauseBoundaryProbeStepLimit = 20;
        public const string PauseBoundaryLeftContextFirstWord = "I";
        public const string PauseBoundaryLeftContextSecondWord = "believe";
        public const string PauseBoundaryRightContextFirstWord = "be";
        public const string PauseBoundaryRightContextSecondWord = "a";
        public const string StopAtEndScenarioName = "learn-stop-at-end";
        public const string StopAtEndStep = "01-stop-at-end";
        public const string LoopWrapScenarioName = "learn-loop-wrap";
        public const string LoopWrapStep = "01-loop-wrap";
        public const int StepForwardLargeWordCount = 5;
        public const string ShortcutScenarioName = "learn-keyboard-shortcuts";
        public const string ShortcutStep = "01-loop-and-speed";
        public const string ShortcutNavigationScenarioName = "learn-keyboard-navigation-shortcuts";
        public const string ShortcutNavigationStep = "01-word-navigation";
        public const string LoopPressedValue = "true";
        public const string PrepNoteScenarioName = "learn-prep-notes";
        public const string PrepNoteStep = "01-section-note";
        public const string PrepNoteText = "Hold eye contact before the launch metric.";
        public const string SpeedAfterDecreaseText = "250";
        public const string SpeedAfterIncreaseText = "260";
    }

    public static class Teleprompter
    {
        public const string ActiveStateValue = "true";
        public const int AlignmentPollDelayMs = 50;
        public const string InactiveStateValue = "false";
        public const int AlignmentTolerancePx = 6;
        public const string AdjustedFocalPointPercent = "45";
        public static readonly Regex AdjustedFocalGuideStyle = new("top:\\s*45%", RegexOptions.Compiled);
        public const int AlignmentTimeoutMs = 1000;
        public const string ActiveWordSelector =
            $"[{Html.DataTestAttribute}='{UiTestIds.Teleprompter.ActiveWord}']";
        public const int TransitionProbeIntervalMs = 50;
        public const int TransitionProbeSampleCount = 28;
        public const double TransitionReversalTolerancePx = 6;
        public const double TransitionMinimumTravelPx = 80;
        public const string PauseToggleIconSelector = $"[{Html.DataTestAttribute}='{UiTestIds.Teleprompter.PauseIcon}']";
        public const string ShortcutScenarioName = "teleprompter-keyboard-shortcuts";
        public const string ShortcutStep = "01-mirror-and-justify";
        public const string ShortcutInputFocusScenarioName = "teleprompter-keyboard-input-focus";
        public const string ShortcutInputFocusStep = "01-slider-focus-contained";
    }

    public static class RapidInput
    {
        public const string CriticalConsolePrefix = "crit:";
        public const string ConsoleErrorType = "error";
        public const string ConsoleWarningType = "warning";
        public const string ObjectDisposedExceptionFragment = "ObjectDisposedException";
        public const string UnhandledUiExceptionFragment = "Unhandled UI exception";
        public const string DisposedCancellationTokenFragment = "CancellationTokenSource_Disposed";
        public const int EditorHistoryBurstCount = 6;
        public const int LearnLoopToggleBurstCount = 2;
        public const int LearnPlayToggleBurstCount = 4;
        public const int LearnStepBurstCount = 8;
        public const int PostBurstSettleDelayMs = 900;
        public const int TeleprompterBlockBurstCount = 8;
        public const int TeleprompterWordBurstCount = 10;
    }

    public static class Folders
    {
        public const string AllScriptsName = "All Scripts";
        public const string PresentationsId = "test-presentations";
        public const string PresentationsName = "Presentations";
        public const string TedTalksId = "test-ted-talks";
        public const string RoadshowsId = "roadshows";
        public const string RoadshowSeasonId = "roadshow-season";
        public const string RoadshowSeasonName = "Roadshow Season";
        public const string RoadshowsName = "Roadshows";
        public const string TedTalksName = "TED Talks";
    }

    public static class Library
    {
        public const string HoverBoxShadowNone = "none";
        public const string ContentSearchQuery = "intuition";
        public const string FileNameSearchQuery = "test-quantum-computing.tps";
        public const string SearchQuery = "Quantum";
        public const string ModeLabel = "Actor";
        public const string ShowModeValue = "shows";
        public const string ShowsLabel = "Shows";
        public const string NewShowLabel = "New show";
        public const string CreateShowTitle = "Create Show";
        public const string ShowPlaceholder = "Morning show";
        public const string DemoToneLabel = "Warm";
        public const string PracticeActionLabel = "Practice";
        public const string PrepActionLabel = "Prep";
        public const string TeleprompterActionLabel = "Teleprompter";
    }

    public static class Editor
    {
        public const string CalendarIconUnexpectedColor = "rgb(0, 0, 0)";
        public const string FindNoMatches = "No matches";
        public const string FindSingleMatchSummary = "1 / 1";
        public const string FindQuery = "intuition";
        public const string FindMissingQuery = "nonexistent";
        public const string FirstProbeCharacter = "d";
        public const string NewDraftProbeText = "dc";
        public const string SecondProbeCharacter = "c";
        public const string BodyHeading = "## [Intro|140WPM|warm]";
        public const string OpeningBlockHeading = "### [Opening Block|140WPM]";
        public const string RenderedOpeningProbe = "Good morning everyone";
        public const string RenderedOpeningRewrite = "Good morning everyone, welcome from cards mode.";
        public const string DisplayDuration = "12:34";
        public const string HeaderContinuationText = " B";
        public const string LegacyActiveBlockLabel = "ACTIVE BLOCK";
        public const string LegacyActiveSegmentLabel = "ACTIVE SEGMENT";
        public const string QuantumOverviewBlockHeader = "### [Overview Block|280WPM|neutral]";
        public const string QuantumOverviewBlockLineText = QuantumOverviewBlockHeader + HeaderContinuationText;
        public const string TransparentInputColor = "rgba(0, 0, 0, 0)";
        public const string VisibleOverlayOpacity = "1";
        public const string Welcome = "welcome";
        public const string TransformativeMoment = "transformative moment";
        public const string OurCompany = "our company";
        public const string HighlightFragment = "[highlight]welcome[/highlight]";
        public const string EmphasisFragment = "[emphasis]welcome[/emphasis]";
        public const string LoudFragment = "[loud]welcome[/loud]";
        public const string ProfessionalFragment = "[professional]transformative moment[/professional]";
        public const string ProfessionalCompanyFragment = "[professional]our company[/professional]";
        public const string WhisperCompanyFragment = "[whisper]our company[/whisper]";
        public const string SarcasmMomentFragment = "[sarcasm]transformative moment[/sarcasm]";
        public const string SlowFragment = "[slow]transformative moment[/slow]";
        public const string SlowCompanyFragment = "[slow]our company[/slow]";
        public const string PauseFragment = "[pause:2s]";
        public const string TimedPauseFragment = "[pause:500ms]";
        public const string CustomWpmToken = "[180WPM]";
        public const string CustomWpmCompanyFragment = "[180WPM]our company[/180WPM]";
        public const string PronunciationCompanyFragment = "[pronunciation:guide]our company[/pronunciation]";
        public const string EditPointMediumFragment = "[edit_point:medium]";
        public const string StructureSegmentToken = "## [Segment Name|Speaker:Host|140WPM|neutral|0:00-0:30]";
        public const string StructureBlockToken = "### [Block Name|Speaker:Host|140WPM|focused]";
        public const string DurationField = "duration:";
        public const string LineFeed = "\n";
        public const string PronunciationToken = "[pronunciation:guide]";
        public const string SegmentRewrite = "## [Launch Angle|205WPM|focused|1:00-2:00]";
        public const string BlockRewrite = "### [Signal Block|205WPM|professional]";
        public const string RetitledScript = "Renamed Product Launch";
        public const string TitleFieldPrefix = "title:";
        public const string TypedTitle = "Typed Intro";
        public const string TypedBlock = "Typed Block";
        public const string TypedHighlight = "[highlight]Every word[/highlight]";
        public const string TypedMultilineSelectionStart = "typed TPS script.";
        public const string TypedMultilineSelectionEnd = "floating toolbar";
        public const string TypedMultilineSelectionProbeLine = "This is a typed TPS script.";
        public const string TypedSelectionTarget = "script";
        public const string ReverseSelectionTarget = "script";
        public const int ReverseSelectionCharacterCount = 5;
        public const string ReverseSelectionExpectedText = "cript";
        public const string ReverseMultilineSelectionTarget = "anchored.";
        public const int ReverseMultilineSelectionCharacterCount = 72;
        public const string DemoFloatingSelectionTarget = "local-first teleprompter workflow";
        public const string ToolbarPinnedSelectionTarget = "welcome";
        public const int ToolbarPinnedSelectionCharacterCount = 7;
        public const double FloatingBarPinnedMaxDriftPx = 4;
        public const double MenuDismissClickVerticalFactor = 0.5;
        public const int MenuDismissRightInsetPx = 96;
        public const int LocalHistoryAutosaveObservationDelayMs = 1900;
        public const int LocalHistoryCurrentRevisionIndex = 0;
        public const int LocalHistoryPreviousRevisionIndex = 1;
        public const int LocalHistoryOriginalRevisionIndex = 2;
        public const string LocalHistoryFirstLine = "First browser-local revision for restore.";
        public const string LocalHistorySecondLine = "Second browser-local revision for restore.";
        public const string LocalHistoryUnsavedLine = "Autosave disabled should keep this line local.";
        public const string LocalHistoryResavedLine = "Autosave re-enabled should persist this line.";
        public const string SimplifiedMoment = "clear moment";
        public const string AiProviderConfiguredApiKey = "sk-test-openai";
        public const string AiProviderConfiguredModel = "gpt-4o";
        public const string AiScrollJumpLineTemplate = "Line {0:D3} carries the transformative moment for AI simplify.";
        public const int AiScrollJumpLineCount = 140;
        public const int AiScrollJumpMinimumScrollTopPx = 1;
        public const int AiScrollJumpTargetLineIndex = 90;
        public const int AiScrollJumpSettleDelayMs = 500;
        public const double AiScrollJumpMaximumAllowedDeltaPx = 64;
        public const string GraphReadyAttributeName = "data-graph-ready";
        public const string GraphReadyAttributeValue = "true";
        public const string GraphStateAttributeName = "data-graph-state";
        public const string GraphErrorAttributeName = "data-graph-error";
        public const string GraphSemanticStatusAttributeName = "data-status";
        public const string GraphSemanticStatusModelUnavailableValue = "ModelUnavailable";
        public const string GraphSemanticStatusTokenizerSimilarityValue = "TokenizerSimilarity";
        public const string GraphTooltipsAttributeName = "data-graph-tooltips";
        public const string GraphTooltipsAttributeValue = "true";
        public const string GraphNavigationAttributeName = "data-graph-navigation";
        public const string GraphNavigationAttributeValue = "true";
        public const string GraphSelectedNodeAttributeName = "data-graph-selected-node";
        public const string GraphLayoutAttributeName = "data-graph-layout";
        public const string GraphNodeStyleAttributeName = "data-graph-node-style";
        public const string GraphAutoLayoutRunsAttributeName = "data-graph-auto-layout-runs";
        public const string GraphGrabAttributeName = "data-graph-grab";
        public const string GraphGrabAttributeValue = "true";
        public const string GraphPanningAttributeName = "data-graph-panning";
        public const string GraphPanningAttributeValue = "true";
        public const string GraphSplitModeAttributeName = "data-mode";
        public const string GraphSplitModeSplitValue = "split";
        public const string GraphSplitModeGraphOnlyValue = "graph-only";
        public const string GraphSplitResizingAttributeName = "data-resizing";
        public const string GraphSplitResizingValue = "true";
        public const int GraphSplitResizeDeltaPx = 90;
        public const double GraphSplitResizeMinimumDeltaPx = 24;
        public const string GraphGrabCursorValue = "grab";
        public const string GraphLayoutCompactValue = "compact";
        public const string GraphLayoutCircularValue = "circular";
        public const string GraphLayoutGridValue = "grid";
        public const string GraphLayoutMdsValue = "mds";
        public const int GraphNodeOverlapExpectedCount = 0;
        public const int GraphRenderedNodePositionMinimumCount = 2;
        public const double GraphMinimumReadableEdgeOpacity = .64;
        public const double GraphTooltipAnchorTolerancePx = 2;
        public const string GraphRawTpsMarkupRegex = @"\[[^\]\r\n]+\]";
        public const string GraphLaneNodePrefix = "prompterone:graph:lane:";
        public const string GraphNodeTypeEllipseValue = "ellipse";
        public const string GraphNodeTypeRectValue = "rect";
        public const string GraphNodeKindSimilarityChunkValue = "SimilarityChunk";
        public const string GraphEdgeLabelTokenSimilarityValue = "token similarity";
        public const string GraphNodeStyleCardsValue = "cards";
        public const string GraphNodeStyleCompactValue = "compact";
        public const string GraphNodeStyleDotsValue = "dots";
        public const string GraphAutoLayoutFirstRunValue = "1";
        public const string GraphAutoLayoutSecondRunValue = "2";
        public const string GraphLayoutStoryValue = "story";
        public const string TypingResponsivenessProbeText = "local typing must stay instant";
        public const int ClickCaretThreshold = 64;
        public const int ClickNearStartOffsetX = 140;
        public const int ClickNearStartOffsetY = 70;
        public const double FloatingBarMinHeightPx = 40;
        public const double FloatingBarMinGapAboveSelectionPx = 4;
        public const double GutterMinimumWidthPx = 36;
        public const double GutterMaximumWidthPx = 88;
        public const double MinimumLineNumberTextGapPx = 6;
        public const double MaximumLineNumberTextGapPx = 18;
        public const double MinimapMinimumWidthPx = 64;
        public const double MinimapStageEdgeTolerancePx = 18;
        public const double MinimumContentLeftWithLineNumbersPx = 36;
        public const double MaximumLayoutViewportRightGapPx = 2;
        public const double MaximumCollapsedMetadataRailWidthPx = 72;
        public const double MetadataRailDockGapPx = 10;
        public const double MetadataRailDockTolerancePx = 2;
        public const string GutterFirstLineNumberText = "1";
        public const string HiddenOverflowValue = "hidden";
        public const int ScrollProbeLineCount = 120;
        public const int MaxSourceScrollHostTopPx = 0;
        public const int CiMaxTypingLongTaskCount = 10;
        public const int LocalMaxTypingLongTaskCount = 2;
        public const double MaxVisibleRenderP95LatencyMs = 240;
        public const double MaxVisibleRenderSpikeLatencyMs = 300;
        public static int AllowedTypingLongTaskCount =>
            TestEnvironment.IsCiEnvironment
                ? CiMaxTypingLongTaskCount
                : LocalMaxTypingLongTaskCount;
        public const string TypedScript = """
            ## [Typed Intro|175WPM|focused|0:05-0:20]
            ### [Typed Block|165WPM|professional]
            This is a typed TPS script. / [highlight]Every word[/highlight] stays in sync. //
            """;
        public const string TypedMultilineScript = """
            ## [Typed Intro|175WPM|focused|0:05-0:20]
            ### [Typed Block|165WPM|professional]
            This is a typed TPS script.
            Professional speakers keep the floating toolbar anchored.
            """;
    }

    public static class Streaming
    {
        public const string CameraFrameRateFps24 = "Fps24";
        public const string ResolutionHd720 = "Hd720";
        public const string RtmpUrl = "rtmp://live.example.com/stream";
        public const string StreamKey = "sk-live-key";
        public const string BitrateKbps = "7200";
    }

    public static class GoLive
    {
        public const string AutoSeedScenario = "go-live-auto-seed";
        public const string AutoSeedStudioStep = "01-default-studio-shell";
        public const string ActiveStateValue = "active";
        public const string IdleDotColorChannel = "239, 68, 68";
        public const string LiveStateAttributeName = UiDataAttributes.Live.State;
        public const string ScreenTitle = "Go Live";
        public const string CameraSwitchScenario = "go-live-camera-switch";
        public const string CameraSwitchStep = "01-secondary-on-air";
        public const string CrossTabIndicatorActiveStep = "01-recording-active";
        public const string CrossTabIndicatorIdleStep = "02-recording-idle";
        public const string CrossTabIndicatorScenario = "go-live-cross-tab-indicator";
        public const string FirstSourceId = "scene-cam-a";
        public const string FrontCameraLabel = "Front camera";
        public const string BroadcastMicLabel = "Broadcast mic";
        public const string HostParticipantName = "Host";
        public const string IdleStateValue = "idle";
        public const string LegacyNetworkUploadMetric = "8.2 Mbps";
        public const string LiveKitHarnessGlobal = AppMediaRuntime.GoLive.LiveKitHarnessGlobalName;
        public const string LiveKitRoom = "launch-room";
        public const string LiveKitServer = "wss://livekit.example.com";
        public const string LiveKitTransportId = "livekit";
        public const string LiveKitToken = "lk-test-token";
        public const string LiveLevelAttributeName = UiDataAttributes.Live.Level;
        public const string MicChannelId = "mic";
        public const int MinimumActiveLevelPercent = 1;
        public const string Mp4ContainerLabel = "MP4";
        public const string Mp4MimeFragment = "mp4";
        public const string OutputWidthLabel = "1920";
        public const string PrimaryParticipantId = "host";
        public const string PrompterUtilitySourceId = "prompter-display";
        public const string ProgramChannelId = "program";
        public const string RemoteLiveKitGuestExternalId = "guest-one";
        public const string RemoteLiveKitGuestLabel = "Guest One";
        public const string RemoteLiveKitGuestSourceId = "livekit:guest-one";
        public const string RemoteVdoGuestExternalId = "guest-two";
        public const string RemoteVdoGuestLabel = "Guest Two";
        public const string RemoteVdoGuestSourceId = "vdoninja:guest-two";
        public const string RecordingStateValue = "recording";
        public const string RecordingChannelId = "recording";
        public const int SharedContextPageCount = 2;
        public const string RuntimeSessionId = "go-live-program";
        public static readonly string SceneStorageKey = string.Concat(BrowserStorageKeys.SettingsPrefix, BrowserAppSettingsKeys.SceneSettings);
        public const string SecondSourceId = "scene-cam-b";
        public const string SideCameraLabel = "Side camera";
        public const string StreamingStateValue = "streaming";
        public const string ByteSuffix = "B";
        public const int AudioRouteBothValue = 2;
        public const int PublishOnlyRoleValue = 2;
        public const int SourceAndPublishRoleValue = 3;
        public const string VdoNinjaHarnessGlobal = AppMediaRuntime.GoLive.VdoNinjaHarnessGlobalName;
        public const string VdoNinjaTransportId = "vdoninja";
        public const string VdoNinjaPublishStreamId = "prompterone-program";
        public const string VdoNinjaPublishUrl = "https://vdo.ninja/?room=launch-room&push=prompterone-program";
        public const string VdoNinjaRoom = "launch-room";
        public const string WebmContainerLabel = "WEBM";
        public const string WidgetReturnScreenshotPath = "output/playwright/go-live-widget-return.png";
        public const string ZeroLevelValue = "0";
        public const string ShortcutScenario = "go-live-keyboard-shortcuts";
        public const string ShortcutStep = "01-mode-layout-and-recording";
        public static string InstallVdoNinjaHarnessScript => $$"""
            () => {
                const harness = {
                    connectCalls: [],
                    constructorCalls: [],
                    disconnectCount: 0,
                    joinRoomCalls: [],
                    leaveRoomCount: 0,
                    publishCalls: [],
                    stopPublishingCount: 0
                };

                class FakeVdoNinjaSdk extends EventTarget {
                    constructor(options) {
                        super();
                        this.options = options ?? {};
                        harness.constructorCalls.push({
                            label: this.options.label ?? null,
                            room: this.options.room ?? null
                        });
                    }

                    async connect() {
                        harness.connectCalls.push({
                            label: this.options.label ?? null,
                            room: this.options.room ?? null
                        });
                        this.dispatchEvent(new CustomEvent('connected'));
                    }

                    async joinRoom(options) {
                        harness.joinRoomCalls.push({
                            room: options?.room ?? null
                        });
                        this.dispatchEvent(new CustomEvent('roomJoined', {
                            detail: { room: options?.room ?? null }
                        }));
                    }

                    async publish(stream, options) {
                        const trackKinds = Array.from(stream?.getTracks?.() ?? []).map(track => track?.kind ?? null);
                        harness.publishCalls.push({
                            label: options?.label ?? null,
                            room: options?.room ?? null,
                            streamId: options?.streamID ?? null,
                            trackKinds
                        });
                        this.dispatchEvent(new CustomEvent('peerListing', {
                            detail: [{ id: 'viewer-1' }]
                        }));
                        this.dispatchEvent(new CustomEvent('peerLatency', {
                            detail: { latency: 42 }
                        }));
                        this.dispatchEvent(new CustomEvent('publishing', {
                            detail: { streamID: options?.streamID ?? null }
                        }));
                        return { streamID: options?.streamID ?? null };
                    }

                    async stopPublishing() {
                        harness.stopPublishingCount += 1;
                        this.dispatchEvent(new CustomEvent('publishingStopped'));
                    }

                    async leaveRoom() {
                        harness.leaveRoomCount += 1;
                        this.dispatchEvent(new CustomEvent('roomLeft'));
                    }

                    disconnect() {
                        harness.disconnectCount += 1;
                        this.dispatchEvent(new CustomEvent('disconnected'));
                    }
                }

                window["{{VdoNinjaHarnessGlobal}}"] = harness;
                window["{{AppMediaRuntime.Vendor.VdoNinjaSdkGlobalName}}"] = FakeVdoNinjaSdk;
                window["{{AppMediaRuntime.Vendor.VdoNinjaLegacyGlobalName}}"] = FakeVdoNinjaSdk;
            }
            """;
        public static string GetVdoNinjaHarnessScript => $$"""() => window["{{VdoNinjaHarnessGlobal}}"]""";
        public static string VdoNinjaHarnessReadyScript =>
            $$"""() => Boolean(window["{{VdoNinjaHarnessGlobal}}"] && window["{{VdoNinjaHarnessGlobal}}"].connectCalls.length === 1 && window["{{VdoNinjaHarnessGlobal}}"].joinRoomCalls.length === 1 && window["{{VdoNinjaHarnessGlobal}}"].publishCalls.length === 1 && window["{{VdoNinjaHarnessGlobal}}"].publishCalls[0].trackKinds.includes('video') && window["{{VdoNinjaHarnessGlobal}}"].publishCalls[0].trackKinds.includes('audio'))""";
        public static string InstallLiveKitHarnessScript => $$"""
            () => {
                const harness = {
                    connectCalls: [],
                    publishCalls: [],
                    unpublishCalls: [],
                    disconnectCount: 0
                };

                class FakeRoom {
                    constructor() {
                        this.localParticipant = {
                            publishTrack: async (track, options) => {
                                harness.publishCalls.push({
                                    kind: track?.kind ?? null,
                                    source: options?.source ?? null,
                                    name: options?.name ?? null
                                });
                                return {};
                            },
                            unpublishTrack: async (track) => {
                                harness.unpublishCalls.push({
                                    kind: track?.kind ?? null
                                });
                                return {};
                            }
                        };
                    }

                    async connect(url, token) {
                        harness.connectCalls.push({ url, token });
                    }

                    disconnect() {
                        harness.disconnectCount += 1;
                    }
                }

                window["{{LiveKitHarnessGlobal}}"] = harness;
                window["{{AppMediaRuntime.Vendor.LiveKitClientGlobalName}}"] = {
                    Room: FakeRoom,
                    Track: {
                        Source: {
                            Camera: "camera",
                            Microphone: "microphone"
                        }
                    }
                };
            }
            """;
        public static string GetLiveKitHarnessScript => $$"""() => window["{{LiveKitHarnessGlobal}}"]""";
        public static string LiveKitHarnessReadyScript =>
            $$"""() => Boolean(window["{{LiveKitHarnessGlobal}}"] && window["{{LiveKitHarnessGlobal}}"].connectCalls.length >= 1 && window["{{LiveKitHarnessGlobal}}"].publishCalls.some(call => call.kind === 'video') && window["{{LiveKitHarnessGlobal}}"].publishCalls.some(call => call.kind === 'audio'))""";
        public static string LiveKitRollbackHarnessReadyScript =>
            $$"""() => Boolean(window["{{LiveKitHarnessGlobal}}"] && window["{{LiveKitHarnessGlobal}}"].connectCalls.length === 1 && window["{{LiveKitHarnessGlobal}}"].disconnectCount === 1)""";
        public static string GetRuntimeStateScript =>
            $$"""sessionId => window["{{AppMediaRuntime.GoLive.OutputNamespace}}"].getSessionState(sessionId)""";
        public static string RecordingRuntimeActiveScript =>
            $$"""sessionId => Boolean(window["{{AppMediaRuntime.GoLive.OutputNamespace}}"].getSessionState(sessionId)?.recording?.active)""";
        public static string RecordingRuntimeMetadataReadyScript =>
            $$"""sessionId => { const state = window["{{AppMediaRuntime.GoLive.OutputNamespace}}"].getSessionState(sessionId); return Boolean(state?.recording?.active && state?.recording?.fileName && state?.recording?.mimeType && (state?.recording?.sizeBytes ?? 0) > 0); }""";
        public static string RecordingRuntimeFileChangedScript =>
            $$"""([sessionId, previousFileName]) => { const state = window["{{AppMediaRuntime.GoLive.OutputNamespace}}"].getSessionState(sessionId); return Boolean(state?.recording?.active && state?.recording?.fileName && state.recording.fileName !== previousFileName && state?.recording?.mimeType && (state?.recording?.sizeBytes ?? 0) > 0); }""";
        public static string RecordingRuntimeInactiveScript =>
            $$"""sessionId => !Boolean(window["{{AppMediaRuntime.GoLive.OutputNamespace}}"].getSessionState(sessionId)?.recording?.active)""";
        public static string RecordingRuntimeUsesProgramSourceScript =>
            $$"""([sessionId, sourceId]) => window["{{AppMediaRuntime.GoLive.OutputNamespace}}"].getSessionState(sessionId)?.program?.primarySourceId === sourceId""";
        public static string RecordingRuntimeAudioLevelsReadyScript { get; } =
            $$"""([sessionId, minimumLevel]) => { const state = window["{{AppMediaRuntime.GoLive.OutputNamespace}}"].getSessionState(sessionId); return (state?.audio?.programLevelPercent ?? 0) >= minimumLevel && (state?.audio?.recordingLevelPercent ?? 0) >= minimumLevel; }""";
        public const string ResolveCameraDeviceScript = """
            async () => {
                const mediaDevices = navigator.mediaDevices;
                if (!mediaDevices || !mediaDevices.enumerateDevices) {
                    return 'default';
                }

                const devices = await mediaDevices.enumerateDevices();
                return devices.find(device => device.kind === 'videoinput')?.deviceId ?? 'default';
            }
            """;
        public const string SeedSceneScript = """
            ([sceneStorageKey, firstSourceId, secondSourceId, primaryCameraId, secondaryCameraId]) => {
                window.localStorage.setItem(sceneStorageKey, JSON.stringify({
                    Cameras: [
                        {
                            SourceId: firstSourceId,
                            DeviceId: primaryCameraId,
                            Label: 'Front camera',
                            Transform: {
                                X: 0.82,
                                Y: 0.82,
                                Width: 0.28,
                                Height: 0.28,
                                Rotation: 0,
                                MirrorHorizontal: true,
                                MirrorVertical: false,
                                Visible: true,
                                IncludeInOutput: true,
                                ZIndex: 1,
                                Opacity: 1
                            }
                        },
                        {
                            SourceId: secondSourceId,
                            DeviceId: secondaryCameraId,
                            Label: 'Side camera',
                            Transform: {
                                X: 0.18,
                                Y: 0.18,
                                Width: 0.22,
                                Height: 0.22,
                                Rotation: 0,
                                MirrorHorizontal: false,
                                MirrorVertical: false,
                                Visible: true,
                                IncludeInOutput: false,
                                ZIndex: 2,
                                Opacity: 0.92
                            }
                        }
                    ],
                    PrimaryMicrophoneId: null,
                    PrimaryMicrophoneLabel: null,
                    AudioBus: {
                        Inputs: [],
                        MasterGain: 1,
                        MonitorEnabled: true
                    }
                }));
            }
            """;
        public const string SeedEmptySceneScript = """
            (sceneStorageKey) => {
                window.localStorage.setItem(sceneStorageKey, JSON.stringify({
                    Cameras: [],
                    PrimaryMicrophoneId: null,
                    PrimaryMicrophoneLabel: null,
                    AudioBus: {
                        Inputs: [],
                        MasterGain: 1,
                        MonitorEnabled: true
                    }
                }));
            }
            """;
        public static string SeedSceneWithPrimaryMicrophoneScript { get; } = $$"""
            ([sceneStorageKey, microphoneId, microphoneLabel]) => {
                const raw = window.localStorage.getItem(sceneStorageKey);
                if (!raw) {
                    return;
                }

                const scene = JSON.parse(raw);
                scene.PrimaryMicrophoneId = microphoneId;
                scene.PrimaryMicrophoneLabel = microphoneLabel;
                scene.AudioBus = {
                    Inputs: [
                        {
                            DeviceId: microphoneId,
                            Label: microphoneLabel,
                            DelayMs: 0,
                            Gain: 1,
                            IsMuted: false,
                            RouteTarget: {{AudioRouteBothValue}}
                        }
                    ],
                    MasterGain: 1,
                    MonitorEnabled: true
                };

                window.localStorage.setItem(sceneStorageKey, JSON.stringify(scene));
            }
            """;
        public const string NoCameraDevicesInitScript = """
            () => {
                const mediaDevices = navigator.mediaDevices;
                if (!mediaDevices) {
                    return;
                }

                mediaDevices.enumerateDevices = async () => [
                    {
                        deviceId: 'browser-mic-only',
                        kind: 'audioinput',
                        label: 'Browser microphone',
                        groupId: 'browser-mic-group',
                        toJSON() { return this; }
                    }
                ];
            }
            """;
        public static string SeedOperationalStudioSettingsScript { get; } = $$"""
            ([storageKey, vdoNinjaRoom, vdoNinjaPublishUrl, youtubeUrl, youtubeKey, primarySourceId]) => {
                window.localStorage.setItem(storageKey, JSON.stringify({
                    Camera: {
                        DefaultCameraId: null,
                        Resolution: 0,
                        FrameRate: 1,
                        MirrorCamera: true,
                        AutoStartOnRead: true
                    },
                    Microphone: {
                        DefaultMicrophoneId: null,
                        InputLevelPercent: 65,
                        NoiseSuppression: true,
                        EchoCancellation: true
                    },
                    Streaming: {
                        ProgramCapture: {
                            ResolutionPreset: 0,
                            BitrateKbps: 6000,
                            ShowTextOverlay: true,
                            IncludeCameraInOutput: true
                        },
                        Recording: {
                            IsEnabled: true
                        },
                        TransportConnections: [
                            {
                                Id: 'vdoninja',
                                Name: 'VDO.Ninja',
                                PlatformKind: 1,
                                Roles: {{PublishOnlyRoleValue}},
                                IsEnabled: true,
                                ServerUrl: '',
                                BaseUrl: 'https://vdo.ninja/',
                                RoomName: vdoNinjaRoom,
                                Token: '',
                                PublishUrl: vdoNinjaPublishUrl,
                                ViewUrl: ''
                            }
                        ],
                        DistributionTargets: [
                            {
                                Id: 'youtube-live',
                                Name: 'YouTube Live',
                                PlatformKind: 2,
                                IsEnabled: true,
                                RtmpUrl: youtubeUrl,
                                StreamKey: youtubeKey,
                                BoundTransportConnectionIds: ['vdoninja']
                            }
                        ],
                        SourceSelections: [
                            { TargetId: 'local-recording', SourceIds: [primarySourceId] },
                            { TargetId: 'vdoninja', SourceIds: [primarySourceId] }
                        ]
                    }
                }));
            }
            """;
        public static string SeedDualTransportStudioSettingsScript { get; } = $$"""
            ([storageKey, liveKitServer, liveKitRoom, liveKitToken, vdoNinjaRoom, vdoNinjaPublishUrl, primarySourceId]) => {
                window.localStorage.setItem(storageKey, JSON.stringify({
                    Camera: {
                        DefaultCameraId: null,
                        Resolution: 0,
                        FrameRate: 1,
                        MirrorCamera: true,
                        AutoStartOnRead: true
                    },
                    Microphone: {
                        DefaultMicrophoneId: null,
                        InputLevelPercent: 65,
                        NoiseSuppression: true,
                        EchoCancellation: true
                    },
                    Streaming: {
                        ProgramCapture: {
                            ResolutionPreset: 0,
                            BitrateKbps: 6000,
                            ShowTextOverlay: true,
                            IncludeCameraInOutput: true
                        },
                        Recording: {
                            IsEnabled: true
                        },
                        TransportConnections: [
                            {
                                Id: 'livekit',
                                Name: 'LiveKit',
                                PlatformKind: 0,
                                Roles: {{SourceAndPublishRoleValue}},
                                IsEnabled: true,
                                ServerUrl: liveKitServer,
                                BaseUrl: '',
                                RoomName: liveKitRoom,
                                Token: liveKitToken,
                                PublishUrl: '',
                                ViewUrl: ''
                            },
                            {
                                Id: 'vdoninja',
                                Name: 'VDO.Ninja',
                                PlatformKind: 1,
                                Roles: {{SourceAndPublishRoleValue}},
                                IsEnabled: true,
                                ServerUrl: '',
                                BaseUrl: 'https://vdo.ninja/',
                                RoomName: vdoNinjaRoom,
                                Token: '',
                                PublishUrl: vdoNinjaPublishUrl,
                                ViewUrl: ''
                            }
                        ],
                        DistributionTargets: [],
                        SourceSelections: [
                            { TargetId: 'livekit', SourceIds: [primarySourceId] },
                            { TargetId: 'vdoninja', SourceIds: [primarySourceId] }
                        ]
                    }
                }));
            }
            """;
        public const string PersistedToggleTargetsScript = """
            (storageKey) => {
                const raw = window.localStorage.getItem(storageKey);
                if (!raw) {
                    return false;
                }

                const parsed = JSON.parse(raw);
                const streaming = parsed?.Streaming;
                const transportConnections = Array.isArray(streaming?.TransportConnections)
                    ? streaming.TransportConnections
                    : [];
                const distributionTargets = Array.isArray(streaming?.DistributionTargets)
                    ? streaming.DistributionTargets
                    : [];

                const hasEnabledTransport = (id) =>
                    transportConnections.some(connection => connection?.Id === id && connection?.IsEnabled === true);
                const hasEnabledTarget = (id) =>
                    distributionTargets.some(target => target?.Id === id && target?.IsEnabled === true);

                return Boolean(
                    hasEnabledTransport('vdoninja') &&
                    hasEnabledTarget('youtube-live'));
            }
            """;
        public const string PreviewReadyScript = "(element) => Boolean(element && element.srcObject && element.readyState >= 2)";
        public static readonly string StoredStudioSettingsKey = string.Concat(BrowserStorageKeys.SettingsPrefix, StudioSettingsStore.StorageKey);
        public const string TwitchUrl = "rtmp://live.twitch.tv/app";
        public const string TwitchKey = "twitch_stream_key";
        public const string YoutubeUrl = "rtmps://a.rtmp.youtube.com/live2";
        public const string YoutubeKey = "youtube_stream_key";
    }

    public static class Diagnostics
    {
        public const string ConnectivityOfflineTitle = "Connection lost";
        public const string ConnectivityOnlineTitle = "Connection restored";
        public const string DispatchOfflineEventScript = "() => window.dispatchEvent(new Event('offline'))";
        public const string DispatchOnlineEventScript = "() => window.dispatchEvent(new Event('online'))";
        public const string NavigatorOnlineMatchesScript = "expectedOnline => navigator.onLine === expectedOnline";
        public const string FolderCreateFailureToggleGlobal = "__prompterFailFolderCreate";
        public const string ForcedFailureDetail = "Forced diagnostics failure from browser test.";
        public const string CreateFolderFailure = "Unable to create this folder.";
        public const string FolderStorageKey = BrowserStorageKeys.FolderLibrary;
    }

    public static class Localization
    {
        public const string CultureStorageKey = BrowserStorageKeys.CultureSetting;
        public const string EnglishSortByLabel = "Sort by";
        public const string FrenchCultureName = "fr";
        public const string FrenchCreateFolderTitle = "Créer un dossier";
        public const string FrenchLanguageLabel = "Français";
        public const string FrenchFoldersLabel = "Dossiers";
        public const string FrenchSortByLabel = "Trier par";
        public const string GermanCultureName = "de";
        public const string GermanSortByLabel = "Sortieren nach";
        public const string RussianCultureName = "ru";
        public const string SetLocalStorageScript = "([key, value]) => window.localStorage.setItem(key, value)";

        public static string BuildNavigatorLanguagesInitScript(params string[] languages)
        {
            var serializedLanguages = string.Join(
                ", ",
                languages.Select(language => $"'{language}'"));

            return $$"""
            (() => {
                const languages = [{{serializedLanguages}}];
                Object.defineProperty(window.navigator, 'languages', {
                    configurable: true,
                    get: () => languages
                });
                Object.defineProperty(window.navigator, 'language', {
                    configurable: true,
                    get: () => languages[0]
                });
            })();
            """;
        }
    }

    public static class Timing
    {
        public const int DefaultNavigationTimeoutMs = 20_000;
        public const int EditorMutationTimeoutMs = 20_000;
        public const int EditorReadyTimeoutMs = 25_000;
        public const int FastVisibleTimeoutMs = 5_000;
        public const int DefaultVisibleTimeoutMs = 10_000;
        public const int DiagnosticPollDelayMs = 50;
        public const int ExtendedVisibleTimeoutMs = 15_000;
        public const int InteractionRetryCount = 3;
        public const int InteractionRetryDelayMs = 125;
        public const int RuntimeWarmupVisibleTimeoutMs = 45_000;
        public const int NewDraftPersistGraceDelayMs = 700;
        public const int NewDraftPersistSettleDelayMs = 2_200;
        public const int FloatingToolbarSettleDelayMs = 500;
        public const int LearnPlaybackDelayMs = 900;
        public const int LearnPlaybackProbeWindowMs = 2_200;
        public const int ReaderPlaybackDelayMs = 2_500;
        public const int ReaderPlaybackReadyTimeoutMs = 8_000;
        public const int ReaderPlaybackStartTimeoutMs = 5_000;
        public const int ReaderPlaybackAdvanceTimeoutMs = 8_000;
        public const int ReaderTransitionSettleDelayMs = 1_100;
        public const int ReaderPostTransitionAdvanceDelayMs = 1_600;
        public const int ReaderAutomaticTransitionTimeoutMs = 14_000;
        public const int ReaderCameraInitDelayMs = 750;
        public const int PersistDelayMs = 1_800;
        public const int PersistReloadDelayMs = 10_000;
        public const int TypingProbeSettleDelayMs = 300;
    }

    public static class Regexes
    {
        public static Regex LearnProgressLabel { get; } = new(BrowserTestConstants.Learn.ProgressLabelPattern, RegexOptions.Compiled);
        public static Regex SettingsAboutVersion { get; } = new(@"^Version \d+\.\d+\.\d+ · Build \d+$", RegexOptions.Compiled);
        public static Regex NonZeroWidth { get; } = new(@"width:\s*0%", RegexOptions.Compiled);
        public static Regex ReaderFirstBlockIndicator { get; } = new(@"^1 / \d+$", RegexOptions.Compiled);
        public static Regex ReaderTimeNotZero { get; } = new(@"^0:00 /", RegexOptions.Compiled);
        public static Regex ReaderSecondBlockIndicator { get; } = new(@"^2 / \d+$", RegexOptions.Compiled);
        public static Regex CameraAutoStart { get; } = new(@"true|false", RegexOptions.Compiled);
        public static Regex EndsWithPause { get; } = new(@"\[pause:2s\]\s*$", RegexOptions.Compiled);
    }

    public static class Keyboard
    {
        public const string ArrowDown = "ArrowDown";
        public const string ArrowLeft = "ArrowLeft";
        public const string ArrowRight = "ArrowRight";
        public const string ArrowUp = "ArrowUp";
        public const string BracketLeft = "[";
        public const string BracketRight = "]";
        public const string PageDown = "PageDown";
        public const string PageUp = "PageUp";
        public const string Digit2 = "2";
        public const string Digit3 = "3";
        public const string Digit4 = "4";
        public const string Enter = "Enter";
        public const string F = "f";
        public const string H = "h";
        public const string L = "l";
        public const string O = "o";
        public const string R = "r";
        public const string Space = "Space";
        public const string V = "v";
        public const string SelectAll = "ControlOrMeta+A";
        public const string OpenAssistant = "ControlOrMeta+K";
        public const string Backspace = "Backspace";
        public const string Undo = "ControlOrMeta+Z";
        public const string Redo = "ControlOrMeta+Shift+Z";
        public const string Shift = "Shift";
    }

    public static class Routes
    {
        public static string Editor => AppRoutes.Editor;
        public static string GoLive => AppRoutes.GoLive;
        public static string Library => AppRoutes.Library;
        public static string Settings => AppRoutes.Settings;
        public static string EditorDemo => AppRoutes.EditorWithId(Scripts.DemoId);
        public static string EditorHugeDraft => AppRoutes.EditorWithId(Scripts.HugeDraftId);
        public static string EditorLargeDraft => AppRoutes.EditorWithId(Scripts.LargeDraftId);
        public static string EditorMissing => AppRoutes.EditorWithId(Scripts.MissingId);
        public static string EditorQuantum => AppRoutes.EditorWithId(Scripts.QuantumId);
        public static string LearnDemo => AppRoutes.LearnWithId(Scripts.DemoId);
        public static string LearnLeadership => AppRoutes.LearnWithId(Scripts.LeadershipId);
        public static string LearnMissing => AppRoutes.LearnWithId(Scripts.MissingId);
        public static string LearnQuantum => AppRoutes.LearnWithId(Scripts.QuantumId);
        public static string LearnWpmBoundary => AppRoutes.LearnWithId(Scripts.LearnWpmBoundaryId);
        public static string LearnReaderTiming => AppRoutes.LearnWithId(Scripts.ReaderTimingId);
        public static string LearnSecurityIncident => AppRoutes.LearnWithId(Scripts.SecurityIncidentId);
        public static string PrepDemo => AppRoutes.PrepWithId(Scripts.DemoId);
        public static string PrepQuantum => AppRoutes.PrepWithId(Scripts.QuantumId);
        public static string GoLiveDemo => AppRoutes.GoLiveWithId(Scripts.DemoId);
        public static string GoLiveLeadership => AppRoutes.GoLiveWithId(Scripts.LeadershipId);
        public static string GoLiveQuantum => AppRoutes.GoLiveWithId(Scripts.QuantumId);
        public static string LibraryWithOnboarding => AppRoutes.LibraryWithOnboarding();
        public static string TeleprompterDemo => AppRoutes.TeleprompterWithId(Scripts.DemoId);
        public static string TeleprompterLeadership => AppRoutes.TeleprompterWithId(Scripts.LeadershipId);
        public static string TeleprompterMissing => AppRoutes.TeleprompterWithId(Scripts.MissingId);
        public static string TeleprompterSecurityIncident => AppRoutes.TeleprompterWithId(Scripts.SecurityIncidentId);
        public static string TeleprompterQuantum => AppRoutes.TeleprompterWithId(Scripts.QuantumId);
        public static string TeleprompterReaderTiming => AppRoutes.TeleprompterWithId(Scripts.ReaderTimingId);
        public static string TeleprompterSpeedOffsets => AppRoutes.TeleprompterWithId(Scripts.SpeedOffsetsId);
        public static string TeleprompterTpsCueMatrix => AppRoutes.TeleprompterWithId(Scripts.TpsCueMatrixId);

        public static string Pattern(string route) => string.Concat("**", route);
    }

    public static class Telemetry
    {
        public const string ClarityProvider = "clarity";
        public const string ClarityScriptRequestPattern = "https://www.clarity.ms/tag/**";
        public const string ClarityScriptUrlPrefix = "https://www.clarity.ms/tag/";
        public const int ExpectedActionEventCount = 1;
        public const int ExpectedActionPageViewCount = 2;
        public const int ExpectedVendorLoadCount = 2;
        public const string EventsCollection = AppRuntimeTelemetry.Harness.EventsCollection;
        public const string GoogleAnalyticsScriptRequestPattern = "https://www.googletagmanager.com/gtag/js**";
        public const string GoogleAnalyticsScriptUrlPrefix = "https://www.googletagmanager.com/gtag/js?id=";
        public const string GoogleAnalyticsProvider = "google-analytics";
        public const string HarnessGlobal = AppRuntimeTelemetry.Harness.GlobalName;
        public const string InitializationsCollection = AppRuntimeTelemetry.Harness.InitializationsCollection;
        public const int MinimumInitializationCount = 1;
        public const int MinimumPageViewCount = 1;
        public const string PageViewsCollection = AppRuntimeTelemetry.Harness.PageViewsCollection;
        public const string RuntimeAllowVendorLoadsProperty = AppRuntimeTelemetry.Harness.RuntimeAllowVendorLoadsProperty;
        public const string RuntimeGlobal = AppRuntimeTelemetry.Harness.RuntimeGlobalName;
        public const string RuntimeHarnessEnabledProperty = AppRuntimeTelemetry.Harness.RuntimeHarnessEnabledProperty;
        public const int TelemetryWaitTimeoutMs = Timing.ExtendedVisibleTimeoutMs;
        public const string VendorLoadsCollection = AppRuntimeTelemetry.Harness.VendorLoadsCollection;
    }

    public static class Elements
    {
        public const string CameraOverlaySelector = $"[{Html.DataTestAttribute}='teleprompter-camera-overlay']";
        public const string TeleprompterShellSelector = $"[{Html.DataTestAttribute}='{UiTestIds.Teleprompter.Page}']";
        public static string DemoCard => UiTestIds.Library.Card(Scripts.DemoId);
        public static string SecurityIncidentCard => UiTestIds.Library.Card(Scripts.SecurityIncidentId);
        public static string LeadershipCard => UiTestIds.Library.Card(Scripts.LeadershipId);
        public static string PresentationsFolder => UiTestIds.Library.Folder(Folders.PresentationsId);
        public static string QuantumCard => UiTestIds.Library.Card(Scripts.QuantumId);
        public static string RoadshowSeasonFolder => UiTestIds.Library.Folder(Folders.RoadshowSeasonId);
        public static string RoadshowsFolder => UiTestIds.Library.Folder(Folders.RoadshowsId);
        public static string TedTalksFolder => UiTestIds.Library.Folder(Folders.TedTalksId);
    }
}
