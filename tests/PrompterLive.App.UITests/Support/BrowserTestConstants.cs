using System.Text.RegularExpressions;
using PrompterLive.Shared.Contracts;
using PrompterLive.Shared.Services;

namespace PrompterLive.App.UITests;

internal static partial class BrowserTestConstants
{
    public static class Css
    {
        public const string ActiveClass = "active";
    }

    public static class Html
    {
        public const string ClassAttribute = "class";
        public static readonly char[] ClassSeparator = [' '];
    }

    public static class Scripts
    {
        public const string DemoId = "rsvp-tech-demo";
        public const string LeadershipId = "ted-leadership";
        public const string QuantumId = "quantum-computing";
        public const string SecurityIncidentId = "security-incident";
        public const string ProductLaunchTitle = "Product Launch";
        public const string LeadershipTitle = "TED: Leadership";
        public const string QuantumTitle = "Quantum Computing";
        public const string SecurityIncidentTitle = "Security Incident";
    }

    public static class Learn
    {
        public const string EndOfScriptText = "End of script.";
        public const string MidFlowWord = "this";
        public const string NextPhraseFragment = "our monitoring systems detected unauthorized";
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
        public const int ContextWordCount = 5;
    }

    public static class Folders
    {
        public const string PresentationsId = "presentations";
        public const string TedTalksId = "ted";
        public const string RoadshowsId = "roadshows";
        public const string RoadshowsName = "Roadshows";
        public const string TedTalksName = "TED Talks";
    }

    public static class Library
    {
        public const string HoverBoxShadowNone = "none";
        public const string SearchQuery = "Quantum";
        public const string ModeLabel = "Actor";
    }

    public static class Editor
    {
        public const string FirstProbeCharacter = "d";
        public const string NewDraftProbeText = "dc";
        public const string SecondProbeCharacter = "c";
        public const string BodyHeading = "## [Intro|140WPM|warm]";
        public const string DisplayDuration = "12:34";
        public const string LegacyActiveBlockLabel = "ACTIVE BLOCK";
        public const string LegacyActiveSegmentLabel = "ACTIVE SEGMENT";
        public const string TransparentInputColor = "rgba(0, 0, 0, 0)";
        public const string VisibleOverlayOpacity = "1";
        public const string Welcome = "welcome";
        public const string TransformativeMoment = "transformative moment";
        public const string OurCompany = "our company";
        public const string HighlightFragment = "[highlight]welcome[/highlight]";
        public const string EmphasisFragment = "[emphasis]welcome[/emphasis]";
        public const string GreenFragment = "[green]welcome[/green]";
        public const string ProfessionalFragment = "[professional]transformative moment[/professional]";
        public const string SlowFragment = "[slow]transformative moment[/slow]";
        public const string SlowCompanyFragment = "[slow]our company[/slow]";
        public const string PauseFragment = "[pause:2s]";
        public const string CustomWpmToken = "[180WPM]";
        public const string DurationField = "display_duration:";
        public const string PronunciationToken = "[pronunciation:guide]";
        public const string SegmentRewrite = "## [Launch Angle|305WPM|focused|1:00-2:00]";
        public const string BlockRewrite = "### [Signal Block|305WPM|professional]";
        public const string TypedTitle = "Typed Intro";
        public const string TypedBlock = "Typed Block";
        public const string TypedHighlight = "[highlight]Every word[/highlight]";
        public const string TypedMultilineSelectionStart = "Typed Intro";
        public const string TypedMultilineSelectionEnd = "professional";
        public const string TypedSelectionTarget = "script";
        public const string SimplifiedMoment = "clear moment";
        public const int ClickCaretThreshold = 64;
        public const int ClickNearStartOffsetX = 140;
        public const int ClickNearStartOffsetY = 70;
        public const double FloatingBarMinHeightPx = 40;
        public const double FloatingBarMinGapAboveSelectionPx = 4;
        public const string TypedScript = """
            ## [Typed Intro|175WPM|focused|0:05-0:20]
            ### [Typed Block|165WPM|professional]
            This is a typed TPS script. / [highlight]Every word[/highlight] stays in sync. //
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
        public const string FirstSourceId = "scene-cam-a";
        public const string LiveKitRoom = "launch-room";
        public const string LiveKitServer = "wss://livekit.example.com";
        public const string LiveKitToken = "lk-test-token";
        public const string SceneStorageKey = "prompterlive.settings.prompterlive.scene";
        public const string SecondSourceId = "scene-cam-b";
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
            ([sceneStorageKey, firstSourceId, secondSourceId, cameraDeviceId]) => {
                window.localStorage.setItem(sceneStorageKey, JSON.stringify({
                    Cameras: [
                        {
                            SourceId: firstSourceId,
                            DeviceId: cameraDeviceId,
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
                            DeviceId: cameraDeviceId,
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
        public const string PersistedTargetsScript = """
            ([storageKey, liveKitServer, liveKitRoom, youtubeUrl, liveKitTargetId, youtubeTargetId]) => {
                const raw = window.localStorage.getItem(storageKey);
                if (!raw) {
                    return false;
                }

                const parsed = JSON.parse(raw);
                const streaming = parsed?.Streaming;
                const selections = streaming?.DestinationSourceSelections ?? [];
                const liveKitSources = selections.find(selection => selection.TargetId === liveKitTargetId)?.SourceIds ?? [];
                const youtubeSources = selections.find(selection => selection.TargetId === youtubeTargetId)?.SourceIds ?? [];
                return Boolean(
                    streaming?.LiveKitEnabled === true &&
                    streaming?.YoutubeEnabled === true &&
                    streaming?.LiveKitServerUrl === liveKitServer &&
                    streaming?.LiveKitRoomName === liveKitRoom &&
                    streaming?.YoutubeRtmpUrl === youtubeUrl &&
                    liveKitSources.length >= 1 &&
                    youtubeSources.length === 0);
            }
            """;
        public const string PreviewReadyScript = "(element) => Boolean(element && element.srcObject && element.readyState >= 2)";
        public const string StoredStudioSettingsKey = "prompterlive.settings." + StudioSettingsStore.StorageKey;
        public const string TwitchUrl = "rtmp://live.twitch.tv/app";
        public const string TwitchKey = "twitch_stream_key";
        public const string YoutubeUrl = "rtmps://a.rtmp.youtube.com/live2";
        public const string YoutubeKey = "youtube_stream_key";
    }

    public static class Diagnostics
    {
        public const string BootstrapDetail = "Forced bootstrap diagnostics from browser test.";
        public const string ConnectivityOfflineTitle = "Connection lost";
        public const string ConnectivityOnlineTitle = "Connection restored";
        public const string ForcedFailureDetail = "Forced diagnostics failure from browser test.";
        public const string CreateFolderFailure = "Unable to create this folder.";
        public const string FolderStorageKey = "prompterlive.folders.v1";
        public const string ShowBootstrapErrorScript = "detail => window.PrompterLive.shell.showBootstrapError(detail)";
        public const string ShowConnectivityOfflineScript = "() => window.PrompterLive.shell.showConnectivityOffline()";
        public const string ShowConnectivityOnlineScript = "() => window.PrompterLive.shell.showConnectivityOnline()";
    }

    public static class Localization
    {
        public const string CultureStorageKey = "prompterlive.settings.culture";
        public const string FrenchCultureName = "fr";
        public const string FrenchCreateFolderTitle = "Créer un dossier";
        public const string FrenchFoldersLabel = "Dossiers";
        public const string FrenchSortByLabel = "Trier par";
        public const string SetLocalStorageScript = "([key, value]) => window.localStorage.setItem(key, value)";
    }

    public static class Timing
    {
        public const int FastVisibleTimeoutMs = 5_000;
        public const int DefaultVisibleTimeoutMs = 10_000;
        public const int ExtendedVisibleTimeoutMs = 15_000;
        public const int NewDraftPersistGraceDelayMs = 700;
        public const int NewDraftPersistSettleDelayMs = 1_800;
        public const int FloatingToolbarSettleDelayMs = 500;
        public const int LearnPlaybackDelayMs = 900;
        public const int ReaderPlaybackDelayMs = 2_500;
        public const int ReaderPlaybackStartTimeoutMs = 5_000;
        public const int ReaderTransitionSettleDelayMs = 1_100;
        public const int ReaderPostTransitionAdvanceDelayMs = 1_600;
        public const int ReaderAutomaticTransitionTimeoutMs = 14_000;
        public const int ReaderCameraInitDelayMs = 750;
        public const int PersistDelayMs = 800;
    }

    public static class Regexes
    {
        public static Regex ActiveClass { get; } = new(@"\bactive\b", RegexOptions.Compiled);
        public static Regex ToggleOnClass { get; } = new(@"\bon\b", RegexOptions.Compiled);
        public static Regex NonZeroWidth { get; } = new(@"width:\s*0%", RegexOptions.Compiled);
        public static Regex ReaderTimeNotZero { get; } = new(@"^0:00 /", RegexOptions.Compiled);
        public static Regex ReaderSecondBlockIndicator { get; } = new(@"^2 / \d+$", RegexOptions.Compiled);
        public static Regex CameraAutoStart { get; } = new(@"true|false", RegexOptions.Compiled);
        public static Regex EndsWithPause { get; } = new(@"\[pause:2s\]\s*$", RegexOptions.Compiled);
    }

    public static class Keyboard
    {
        public const string SelectAll = "Meta+A";
        public const string Backspace = "Backspace";
        public const string Undo = "Meta+Z";
        public const string Redo = "Meta+Shift+Z";
    }

    public static class Routes
    {
        public static string Editor => AppRoutes.Editor;
        public static string Library => AppRoutes.Library;
        public static string Settings => AppRoutes.Settings;
        public static string EditorDemo => AppRoutes.EditorWithId(Scripts.DemoId);
        public static string EditorQuantum => AppRoutes.EditorWithId(Scripts.QuantumId);
        public static string LearnDemo => AppRoutes.LearnWithId(Scripts.DemoId);
        public static string LearnLeadership => AppRoutes.LearnWithId(Scripts.LeadershipId);
        public static string LearnQuantum => AppRoutes.LearnWithId(Scripts.QuantumId);
        public static string LearnSecurityIncident => AppRoutes.LearnWithId(Scripts.SecurityIncidentId);
        public static string GoLiveDemo => AppRoutes.GoLiveWithId(Scripts.DemoId);
        public static string TeleprompterDemo => AppRoutes.TeleprompterWithId(Scripts.DemoId);
        public static string TeleprompterLeadership => AppRoutes.TeleprompterWithId(Scripts.LeadershipId);
        public static string TeleprompterSecurityIncident => AppRoutes.TeleprompterWithId(Scripts.SecurityIncidentId);
        public static string TeleprompterQuantum => AppRoutes.TeleprompterWithId(Scripts.QuantumId);

        public static string Pattern(string route) => string.Concat("**", route);
    }

    public static class Elements
    {
        public const string CameraOverlaySelector = ".rd-camera-overlay";
        public const string TeleprompterShellSelector = ".rd";
        public static string DemoCard => UiTestIds.Library.Card(Scripts.DemoId);
        public static string SecurityIncidentCard => UiTestIds.Library.Card(Scripts.SecurityIncidentId);
        public static string LeadershipCard => UiTestIds.Library.Card(Scripts.LeadershipId);
        public static string PresentationsChip => UiTestIds.Library.FolderChip(Folders.PresentationsId);
        public static string QuantumCard => UiTestIds.Library.Card(Scripts.QuantumId);
        public static string RoadshowsFolder => UiTestIds.Library.Folder(Folders.RoadshowsId);
        public static string TedTalksChip => UiTestIds.Library.FolderChip(Folders.TedTalksId);
        public static string TedTalksFolder => UiTestIds.Library.Folder(Folders.TedTalksId);
    }
}
