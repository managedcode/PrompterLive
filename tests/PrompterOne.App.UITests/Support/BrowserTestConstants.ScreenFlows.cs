namespace PrompterOne.App.UITests;

internal static partial class BrowserTestConstants
{
    public static class Selectors
    {
        public const string CardCoverMeta = ".dcover-meta";
        public const string ClassAttribute = "class";
    }

    public static class EditorFlow
    {
        public const string OpeningBlock = "Opening Block";
        public const string PurposeBlock = "Purpose Block";
        public const string BenefitsBlock = "Benefits Block";
        public const string LearnSpeedAfterIncrease = "260";
        public const int BenefitsSegmentIndex = 2;
        public const int BenefitsBlockIndex = 1;
    }

    public static class SettingsFlow
    {
        public const string CrossTabThemeScenario = "settings-cross-tab-theme-sync";
        public const string CrossTabThemeSyncedStep = "01-light-theme-synced";
        public const int SharedContextPageCount = 2;
        public const string HtmlThemeAttribute = "data-theme";
        public const string LightTheme = "light";
        public const string CloudStorageScenario = "settings-cloud-storage";
        public const string CloudStorageConfiguredStep = "01-cloud-storage-configured";
        public const string CloudStorageReloadedStep = "02-cloud-storage-reloaded";
        public const string DropboxLabel = "Managed Dropbox";
        public const string DropboxValidationMessage = "Dropbox requires an access token or a refresh token with app key.";
        public const string OpenAiProviderId = "openai";
        public const string MicLevelPercentText = "82%";
        public const string MicLevelValue = "82";
        public const double NavItemLayoutTolerancePx = 0.5;
        public const string MicLevelInputScript =
            "element => { element.value = '82'; element.dispatchEvent(new Event('input', { bubbles: true })); }";
    }

    public static class TeleprompterFlow
    {
        public const string BackgroundColorProperty = "backgroundColor";
        public const string ColorProperty = "color";
        public const double ControlsMinimumOpacity = 0.9;
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
        public const string ProgressEmptyStylePattern = @"width:\s*0%";
        public const string ProgressFilledStylePattern = @"width:\s*100%";
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
        public const string ProductLaunchGreenWord = "transformative";
        public const string ProductLaunchHighlightWord = "solution";
        public const string WidthAfterChange = "900";
        public const string ProductLaunchPurpleWord = "focus";
        public const string ProductLaunchSlowWord = "elephant";
        public const string ProductLaunchTeleprompterWord = "teleprompter";
        public const string ProductLaunchTeleprompterPronunciation = "TELE-promp-ter";
        public const string ProductLaunchTeleprompterWpm = "180";
        public const string ProductLaunchUrgentWord = "time";
        public const string ProductLaunchVisionPronunciation = "ˈviʒən";
        public const string ProductLaunchVisionWord = "vision";
        public const string ProductLaunchWarmWord = "Let";
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
        public const string EnabledClass = "on";
        public const string FalseText = "false";
        public const string StyleAttribute = "style";
        public const string TrueText = "true";
        public const string WidthInputScript =
            "element => { element.value = '900'; element.dispatchEvent(new Event('input', { bubbles: true })); }";
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
        public const string SeedStoredSceneScript =
            """
            ({ cameraDeviceId }) => {
                localStorage.setItem('prompterone.settings.prompterone.reader', JSON.stringify({
                    CountdownSeconds: 3,
                    FontScale: 1,
                    TextWidth: 1,
                    ScrollSpeed: 1,
                    MirrorText: false,
                    ShowFocusLine: true,
                    ShowProgress: true,
                    ShowCameraScene: true
                }));

                localStorage.setItem('prompterone.settings.prompterone.scene', JSON.stringify({
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

                localStorage.setItem('prompterone.settings.prompterone.studio', JSON.stringify({
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
        public const int BaseWpm = 250;
        public const int SlowWpm = 100;
        public const int FastWpm = 400;

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
