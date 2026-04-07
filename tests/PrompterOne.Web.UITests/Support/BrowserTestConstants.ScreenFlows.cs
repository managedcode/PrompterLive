using PrompterOne.Shared.Services;
using PrompterOne.Shared.Storage;

namespace PrompterOne.Web.UITests;

internal static partial class BrowserTestConstants
{
    public static class EditorFlow
    {
        public const string OpeningBlock = "Opening Block";
        public const string PurposeBlock = "Purpose Block";
        public const string BenefitsBlock = "Benefits Block";
        public const string LayoutScenario = "editor-layout-width";
        public const string LayoutExpandedStep = "01-expanded-width";
        public const string LayoutCollapsedStep = "02-collapsed-metadata";
        public const string LineNumbersScenario = "editor-line-numbers";
        public const string LineNumbersStep = "01-gutter-visible";
        public const string SplitFeedbackScenario = "editor-split-feedback";
        public const string SplitFeedbackStep = "01-split-result-card";
        public const int SplitFeedbackVisibleTimeoutMs = 30_000;
        public const string LocalHistoryScenario = "editor-local-history";
        public const string LocalHistorySavedStep = "01-history-populated";
        public const string LocalHistoryRestoredStep = "02-history-restored";
        public const string LocalHistoryAutosaveScenario = "editor-local-history-autosave-toggle";
        public const string LocalHistoryAutosaveDisabledStep = "01-autosave-disabled";
        public const string LocalHistoryAutosaveEnabledStep = "02-autosave-enabled";
        public const string DatePickerScenario = "editor-date-picker-theme";
        public const string DatePickerDarkStep = "01-dark-theme-date-picker";
        public const string DatePickerLightStep = "02-light-theme-date-picker";
        public const string LearnSpeedAfterIncrease = "260";
        public const int BenefitsSegmentIndex = 2;
        public const int BenefitsBlockIndex = 1;
        public const string LightThemeScenario = "editor-light-theme-emotion-menu";
        public const string LightThemeStep = "01-readable-menu-and-tooltip";
        public const string MetadataRailCollapsedChevronDirection = "left";
        public const string MetadataRailExpandedChevronDirection = "right";
        public const int MetadataRailToggleSettleDelayMs = 250;
        public const string ToolbarTooltipScenario = "editor-toolbar-tooltips";
        public const string ToolbarTooltipDelayStep = "01-delayed-toolbar-tooltip";
        public const string ToolbarTooltipDropdownStep = "02-dropdown-tooltip-gap";
        public const string ToolbarTooltipViewportStep = "03-toolbar-tooltip-in-viewport";
        public const string EmotionTooltipText = "TPS emotions";
        public const string MotivationalEmotionTooltipText = "Motivational emotion";
        public const string ToolbarSemanticScenario = "editor-toolbar-semantic-icons";
        public const string ToolbarSemanticStep = "02-refined-floating-toolbar";
        public const string ToolbarSurfaceScenario = "editor-toolbar-surface-rhythm";
        public const string ToolbarSurfaceStep = "01-voice-menu-surface";
        public const string ToolbarDropdownAlignmentScenario = "editor-toolbar-dropdown-alignment";
        public const string ToolbarDropdownAlignmentTopStep = "01-top-voice-menu-left-cluster";
        public const string ToolbarDropdownAlignmentFloatingStep = "02-floating-voice-menu-left-cluster";
        public const double MinimumDateFieldWidthPx = 150;
        public const int TooltipEarlyCheckDelayMs = 180;
        public const int TooltipSettleDelayMs = 720;
        public const int FloatingSemanticDotCount = 2;
        public const double MinimumSemanticColorDistance = 18;
        public const double MinimumSemanticGroupColorDistance = 60;
        public const double MinimumDropdownSurfaceWidthPx = 300;
        public const double MaximumDropdownRowHeightDeltaPx = 4;
        public const double MaximumDropdownCompactRowHeightPx = 38;
        public const double MinimumDropdownSurfaceBorderAlpha = 0.28;
        public const double MaximumDropdownInlineMetaGapPx = 24;
        public const string VoiceClearLabel = "RESET Remove cues unwrap";
        public const double MinimumTooltipSurfaceBorderAlpha = 0.28;
        public const double MaximumReadableTextChannel = 160;
        public const double MinimumLightMenuSurfaceChannel = 220;
        public const double MinimumLightTooltipSurfaceChannel = 215;
        public const double MaximumEarlyTooltipOpacity = 0.05;
        public const double MinimumVisibleTooltipOpacity = 0.9;
        public const double MaximumTooltipMenuOverlapPx = 0.5;
        public const double ToolbarOverflowTolerancePx = 2;
    }

    public static class TooltipAuditFlow
    {
        public const int ClearHoverX = 2;
        public const int ClearHoverY = 2;
        public const string LibraryFolderScenario = "tooltip-surface-library-folder";
        public const string LibraryCardMenuScenario = "tooltip-surface-library-card-menu";
        public const string LibraryFolderStep = "01-folder-create";
        public const string LibraryCardMenuStep = "02-card-menu";
        public const string LearnScenario = "tooltip-surface-learn";
        public const string LearnPlayStep = "01-play-toggle";
        public const string TeleprompterScenario = "tooltip-surface-teleprompter";
        public const string TeleprompterPlayStep = "01-play-toggle";
        public const string SettingsScenario = "tooltip-surface-settings";
        public const string SettingsAccentStep = "01-accent-swatch";
        public const string PlacementLeft = "left";
        public const string PlacementTop = "top";
        public const string TextTransformNone = "none";
        public const string CreateFolderTooltipText = "Create folder";
        public const string MoreScriptActionsTooltipText = "More script actions";
        public const string PlayPlaybackTooltipText = "Start playback";
        public const string GoldAccentTooltipText = "Gold accent";
        public const int SharedTooltipSettleDelayMs = 560;
        public const int SharedTooltipDismissTimeoutMs = 1_500;
        public const double MinimumBorderAlpha = 0.28;
        public const double MinimumVisibleOpacity = 0.7;
        public const double MaximumOverlapPx = 0.5;
    }

    public static class SettingsFlow
    {
        public const string CrossTabThemeScenario = "settings-cross-tab-theme-sync";
        public const string CrossTabThemeSyncedStep = "01-light-theme-synced";
        public const string DarkTheme = "dark";
        public const int SharedContextPageCount = 2;
        public const string HtmlThemeAttribute = "data-theme";
        public const string LightTheme = "light";
        public const string CloudStorageScenario = "settings-cloud-storage";
        public const string CloudStorageConfiguredStep = "01-cloud-storage-configured";
        public const string CloudStorageReloadedStep = "02-cloud-storage-reloaded";
        public const string LightThemeScenario = "settings-cloud-light-theme";
        public const string LightThemeStep = "01-readable-colors";
        public const string DropboxLabel = "Managed Dropbox";
        public const string DropboxValidationMessage = "Dropbox requires an access token or a refresh token with app key.";
        public const string OpenAiProviderId = "openai";
        public const string MicLevelPercentText = "82%";
        public const string MicLevelValue = "82";
        public const string ShortcutsScenario = "settings-shortcuts-inventory";
        public const string ShortcutsStep = "01-hotkey-catalog";
        public const double NavItemLayoutTolerancePx = 0.5;
        public const double MinimumLightSurfaceChannel = 220;
        public const double MinimumLightFieldChannel = 230;
        public const double MaximumReadableTextChannel = 140;
        public const double MaximumReadableSecondaryTextChannel = 160;
        public const string MicLevelInputScript =
            "element => { element.value = '82'; element.dispatchEvent(new Event('input', { bubbles: true })); }";
    }

    public static class LibraryFlow
    {
        public const string BackgroundColorProperty = "backgroundColor";
        public const string ColorProperty = "color";
        public const string MetricsScenario = "library-real-metrics";
        public const string MetricsStep = "01-quantum-card-metrics";
        public const string LightThemeScenario = "library-light-theme";
        public const string LightThemeStep = "01-readable-library-screen";
        public const double MinimumTouchMenuOpacity = 0.9;
        public const double MinimumLightSidebarSurfaceChannel = 225;
        public const double MinimumLightSearchSurfaceChannel = 230;
        public const double MinimumLightCardSurfaceChannel = 210;
        public const double MinimumLightCreateTileSurfaceChannel = 210;
        public const double MaximumReadableTextChannel = 160;
        public const double MaximumReadableSecondaryTextChannel = 175;
    }

    public static class AppShellFlow
    {
        public const string PlaybackLaunchScenario = "app-shell-playback-launch";
        public const string LearnLaunchStep = "01-library-card-learn";
        public const string LearnBackStep = "02-header-back-from-learn";
        public const string TeleprompterLaunchStep = "03-library-card-read";
        public const string TeleprompterBackStep = "04-header-back-from-teleprompter";
        public const string SettingsOriginScenario = "app-shell-settings-origin";
        public const string SettingsLaunchStep = "01-go-live-open-settings";
        public const string SettingsBackStep = "02-header-back-from-settings";
        public const string SettingsTitle = "Settings";
        public const string LiveWidgetScenario = "app-shell-live-widget";
        public const string LiveWidgetViewportName = "iphone-medium-portrait";
        public const int LiveWidgetTimerPollAttempts = 5;
        public const int LiveWidgetTimerPollDelayMs = 450;
        public const string OnboardingScenario = "app-shell-onboarding";
        public const string OnboardingLibraryStep = "01-library";
        public const string OnboardingTpsStep = "02-tps";
        public const string OnboardingEditorStep = "03-editor";
        public const string OnboardingLearnStep = "04-learn";
        public const string OnboardingTeleprompterStep = "05-teleprompter";
        public const string OnboardingGoLiveStep = "06-go-live";
        public const string OnboardingEnglishWelcomeTitle = "How PrompterOne works";
        public const string OnboardingTpsTitle = "Understand what TPS is";
        public const string OnboardingEditorTitle = "Shape the script in Editor";
        public const string OnboardingLearnTitle = "Rehearse with RSVP";
        public const string OnboardingTeleprompterTitle = "Read on the teleprompter";
        public const string OnboardingGoLiveTitle = "Run the show in Go Live";
        public const string OnboardingUkrainianWelcomeTitle = "Як працює PrompterOne";
        public const string OnboardingUkrainianDismiss = "Не цікаво";
    }

    public static class TeleprompterFlow
    {
        public const string BackgroundColorProperty = "backgroundColor";
        public const string SecurityIncidentChromeScenarioName = "teleprompter-security-incident-chrome";
        public const string SecurityIncidentChromePageStep = "01-muted-page";
        public const string SecurityIncidentChromeProgressStep = "02-muted-progress-shell";
        public const string SecurityIncidentChromeControlsStep = "03-muted-controls";
        public const string CenterAlignmentScenarioName = "teleprompter-text-alignment";
        public const string CenterAlignmentStep = "01-four-alignment-modes";
        public const string AlignmentTooltipScenarioName = "teleprompter-alignment-tooltips";
        public const string AlignmentTooltipStep = "01-delayed-rail-tooltip";
        public const string ColorProperty = "color";
        public const string BackgroundImageProperty = "backgroundImage";
        public const double ControlsMinimumOpacity = 0.9;
        public const string ReaderTextAlignmentAttribute = "data-reader-text-alignment";
        public const string AlignmentLeftValue = "left";
        public const string AlignmentCenterValue = "center";
        public const string AlignmentRightValue = "right";
        public const string AlignmentJustifyValue = "justify";
        public const string TextWrapPrettyValue = "pretty";
        public const double MaximumActiveClusterInlinePaddingPx = 8.5;
        public const double MinimumOpticalInsetPx = 18;
        public const double MaximumClusterWrapPaddingPx = 16.5;
        public const double MaximumOpticalInsetPx = 28.5;
        public const double MaximumFullWidthButtonGapPx = 6;
        public const double MaximumDefaultLeftAverageCenterOffsetPx = 170;
        public const double MaximumDefaultLeftLineCenterOffsetPx = 225;
        public const string MirrorHorizontalTooltipText = "Mirror the reader horizontally";
        public const string MirrorVerticalTooltipText = "Mirror the reader vertically";
        public const string OrientationTooltipText = "Rotate the reader between landscape and portrait";
        public const string FullscreenTooltipText = "Toggle browser fullscreen";
        public const string AlignmentLeftTooltipText = "Align text to the left edge";
        public const string AlignmentCenterTooltipText = "Center text on the reading lane";
        public const string AlignmentRightTooltipText = "Align text to the right edge";
        public const string AlignmentJustifyTooltipText = "Stretch text across the full readable width";
        public const string FocalSliderTooltipText = "Move the focal reading guide";
        public const string WidthSliderTooltipText = "Adjust the reader text width";
        public const int TooltipEarlyCheckDelayMs = 1000;
        public const int TooltipSettleDelayMs = 3400;
        public const int TooltipDismissTimeoutMs = 1_500;
        public const double MaximumEarlyTooltipOpacity = 0.05;
        public const double MinimumVisibleTooltipOpacity = 0.9;
        public const double MaximumTooltipControlOverlapPx = 0.5;
        public const string MirrorScenarioName = "teleprompter-mirror-controls";
        public const string MirrorStep = "01-mirror-controls";
        public const string OrientationPortraitValue = "portrait";
        public const string OrientationLandscapeValue = "landscape";
        public const string FullscreenScenarioName = "teleprompter-fullscreen";
        public const string FullscreenStep = "01-fullscreen-active";
        public const string FullscreenStateScript = "() => Boolean(document.fullscreenElement)";
        public const string FullscreenInactiveStateScript = "() => !document.fullscreenElement";
        public const string ProgressScenarioName = "teleprompter-segmented-progress";
        public const string ProgressStep = "01-block-progress";
        public const string ProgressFitScenarioName = "teleprompter-progress-shell-fit";
        public const string ProgressFitStep = "01-track-contained";
        public const string ReadingChromeScenarioName = "teleprompter-reading-chrome";
        public const string ReadingChromeStep = "01-muted-active-playback";
        public const float ReadingChromeSettleDelayMs = 400;
        public const double MinimumShellButtonBackgroundAlphaReduction = 0.015;
        public const string ProgressEmptyStylePattern = @"width:\s*0%";
        public const string ProgressFilledStylePattern = @"width:\s*100%";
        public const double MaxProgressShellOverflowPx = 0.5;
        public const double MinimumChromeBackgroundAlphaReduction = 0.08;
        public const double MinimumEdgeInfoOpacityReduction = 0.08;
        public const int SecurityIncidentViewportWidth = 1797;
        public const int SecurityIncidentViewportHeight = 882;
        public const double MaximumMutedProgressLabelChannel = 228;
        public const double MaximumMutedControlIconChannel = 220;
        public const double MaximumMutedProgressFillChannel = 208;
        public const double MaximumMutedProgressFillAlpha = 0.6;
        public const double MaximumMutedPlayButtonBackgroundAlpha = 0.1;
        public const int MinimumBalancedTextLineCount = 2;
        public const string ReaderOrientationAttribute = "data-reader-orientation";
        public const string MirrorHorizontalTransform = "scaleX(-1)";
        public const string MirrorVerticalTransform = "scaleY(-1)";
        public const string OpeningBlock = "Opening Block";
        public const string OpeningLine = "Good morning everyone";
        public const string CollapsedOpeningLine = "Goodmorningeveryone";
        public const double EdgeInfoMinimumOpacity = 0.5;
        public const string FastWord = "Full";
        public const string FontScaleAfterIncrease = "40";
        public const string NeutralWord = "Good";
        public const string ProductLaunchProfessionalWord = "transformative";
        public const string ProductLaunchHighlightWord = "solution";
        public const string WidthAfterChange = "82%";
        public const string ProductLaunchRhetoricalWord = "focus";
        public const string ProductLaunchSlowWord = "elephant";
        public const string ProductLaunchTeleprompterWord = "teleprompter";
        public const string ProductLaunchTeleprompterPronunciation = "TELE-promp-ter";
        public const string ProductLaunchTeleprompterWpm = "180";
        public const string ProductLaunchUrgentWord = "time";
        public const string ProductLaunchVisionPronunciation = "VI-zhun";
        public const string ProductLaunchVisionWord = "vision";
        public const string ProductLaunchSoftWord = "Let";
        public const string SpeedOffsetsFastWord = "flight.";
        public const string SpeedOffsetsFastWpm = "154";
        public const string SpeedOffsetsNormalWord = "center";
        public const string SpeedOffsetsNormalWpm = "140";
        public const string SpeedOffsetsResumedSlowWord = "gentle";
        public const string SpeedOffsetsSlowWord = "steady";
        public const string SpeedOffsetsSlowWpm = "126";
        public const double SlidersMinimumOpacity = 0.6;
        public const string TransparentBackgroundColor = "rgba(0, 0, 0, 0)";
        public const string CameraRoleAttribute = "data-camera-role";
        public const string CameraDeviceIdAttribute = "data-camera-device-id";
        public const string CameraAutostartAttribute = "data-camera-autostart";
        public const string PrimaryCameraRole = "primary";
        public const string FalseText = "false";
        public const string StyleAttribute = "style";
        public const string TrueText = "true";
        public const string WidthInputScript =
            "element => { element.value = '82'; element.dispatchEvent(new Event('input', { bubbles: true })); }";
        public const string HasVideoTrackScript =
            "element => !!element.srcObject && element.srcObject.getVideoTracks().length > 0";
        public const string ResolveCameraDeviceScript =
            """
            async () => {
                try {
                    const stream = await navigator.mediaDevices.getUserMedia({ video: true, audio: false });
                    stream.getTracks().forEach(track => track.stop());
                } catch {
                }

                const devices = await navigator.mediaDevices.enumerateDevices();
                return devices.find(device => device.kind === 'videoinput')?.deviceId ?? 'default';
            }
            """;
        private static string ReaderStorageKey => string.Concat(BrowserStorageKeys.SettingsPrefix, BrowserAppSettingsKeys.ReaderSettings);
        private static string SceneStorageKey => string.Concat(BrowserStorageKeys.SettingsPrefix, BrowserAppSettingsKeys.SceneSettings);
        private static string StudioStorageKey => string.Concat(BrowserStorageKeys.SettingsPrefix, StudioSettingsStore.StorageKey);

        public static string SeedStoredSceneScript => $$"""
            ({ cameraDeviceId }) => {
                localStorage.setItem('{{ReaderStorageKey}}', JSON.stringify({
                    CountdownSeconds: 3,
                    FontScale: 1,
                    TextWidth: 1,
                    ScrollSpeed: 1,
                    MirrorText: false,
                    ShowFocusLine: true,
                    ShowProgress: true,
                    ShowCameraScene: true
                }));

                localStorage.setItem('{{SceneStorageKey}}', JSON.stringify({
                    Cameras: [
                        {
                            SourceId: 'scene-cam-a',
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
                            SourceId: 'scene-cam-b',
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
                                IncludeInOutput: true,
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

                localStorage.setItem('{{StudioStorageKey}}', JSON.stringify({
                    Camera: {
                        DefaultCameraId: cameraDeviceId,
                        Resolution: 0,
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
                            IsEnabled: false
                        },
                        TransportConnections: [],
                        DistributionTargets: [],
                        SourceSelections: []
                    }
                }));
            }
            """;
    }

    public static class ReaderTiming
    {
        public const int CapturePollIntervalMs = 20;
        public const int BoundaryTransitionSampleIndex = 4;
        public const int LearnTimingToleranceMs = 200;
        public const int LearnStartupTimingToleranceMs = 1800;
        public const int LearnSpeedStep = 10;
        public const int LearnSlowWpm = 200;
        public const int LearnFastWpm = 300;
        public const int LearnSlowWpmAdjustmentClicks = 5;
        public const int LearnFastWpmAdjustmentClicks = 5;
        public const int MinimumSpeedProbePlaybackDeltaMs = 140;
        public const int MinimumBoundaryPlaybackDeltaMs = 400;
        public const int MinimumBoundaryTransitionDeltaMs = 100;
        public const int SampleCaptureTimeoutMs = 12000;
        public const int TeleprompterTimingToleranceMs = 180;
        public const int WpmBoundaryWordCount = 8;
        public const int WordCount = 5;
        public const string FirstWord = "alpha";
        public const string SecondWord = "bravo";
        public const string ThirdWord = "charlie";
        public const string FourthWord = "delta";
        public const string FifthWord = "echo";
        public const string BoundarySixthWord = "foxtrot";
        public const string BoundarySeventhWord = "golf";
        public const string BoundaryEighthWord = "hotel";
        public const int BaseWpm = 220;
        public const int SlowWpm = 88;
        public const int FastWpm = 352;

        public static IReadOnlyList<string> ExpectedWords =>
        [
            FirstWord,
            SecondWord,
            ThirdWord,
            FourthWord,
            FifthWord
        ];

        public static IReadOnlyList<string> ExpectedBoundaryWords =>
        [
            FirstWord,
            SecondWord,
            ThirdWord,
            FourthWord,
            FifthWord,
            BoundarySixthWord,
            BoundarySeventhWord,
            BoundaryEighthWord
        ];
    }
}
