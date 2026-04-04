using System.Text.Json;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.App.UITests;

public sealed class GoLiveMediaCleanupLifecycleTests(StandaloneAppFixture fixture) : IClassFixture<StandaloneAppFixture>
{
    private const string BrowserMediaInteropNamespace = "BrowserMediaInterop";
    private const string CleanupHarnessGlobal = "__prompterOneGoLiveCleanupHarness";

    private const string InstallCleanupSpiesScript = $$"""
        () => {
            const media = window["{{BrowserMediaInteropNamespace}}"];
            if (!media) {
                throw new Error("Browser media runtime is not available.");
            }

            const harness = window["{{CleanupHarnessGlobal}}"] ?? {
                audioTrackStopCalls: 0,
                releaseSharedCameraTrackCalls: 0
            };

            const originalReleaseSharedCameraTrack = media.releaseSharedCameraTrack.bind(media);
            media.releaseSharedCameraTrack = async captureKey => {
                harness.releaseSharedCameraTrackCalls += 1;
                return await originalReleaseSharedCameraTrack(captureKey);
            };

            const originalCreateLocalAudioTrack = media.createLocalAudioTrack.bind(media);
            media.createLocalAudioTrack = async deviceId => {
                const track = await originalCreateLocalAudioTrack(deviceId);
                const mediaStreamTrack = track?.mediaStreamTrack ?? null;
                let stopped = false;

                return {
                    kind: track?.kind ?? "audio",
                    mediaStreamTrack,
                    stop: () => {
                        if (stopped) {
                            return;
                        }

                        stopped = true;
                        harness.audioTrackStopCalls += 1;
                        return track.stop?.();
                    }
                };
            };

            window["{{CleanupHarnessGlobal}}"] = harness;
        }
        """;

    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
    public async Task GoLivePage_StopAndRestart_ReleaseSharedCameraCapturesAndStopMicTracks()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();

            await page.EvaluateAsync(InstallCleanupSpiesScript);
            await page.EvaluateAsync(BrowserTestConstants.Media.ClearRequestLogScript);
            var cleanupResult = await page.EvaluateAsync<JsonElement>(
                """
                async () => {
                    const support = window.PrompterOneGoLiveOutputSupport;
                    const composer = window.PrompterOneGoLiveMediaComposer;

                    if (!support?.normalizeRequest || !composer?.ensureProgramSession || !composer?.cleanupProgramSession) {
                        throw new Error("Go Live media runtimes are not available.");
                    }

                    const request = support.normalizeRequest({
                        audioInputs: [
                            {
                                delayMs: 0,
                                deviceId: "browser-mic-primary",
                                gain: 1,
                                isMuted: false,
                                isPrimary: true,
                                label: "Browser Microphone",
                                routeTarget: 2
                            }
                        ],
                        primarySourceId: "scene-cam-a",
                        programVideo: {
                            frameRate: 30,
                            height: 720,
                            width: 1280
                        },
                        recording: {
                            audioCodecLabel: "Opus",
                            containerLabel: "WEBM",
                            videoCodecLabel: "VP9"
                        },
                        transportConnections: [],
                        videoSources: [
                            {
                                deviceId: "browser-cam-primary",
                                isPrimary: true,
                                label: "Browser Camera A",
                                sourceId: "scene-cam-a",
                                transform: {
                                    height: 0.28,
                                    includeInOutput: true,
                                    mirrorHorizontal: false,
                                    mirrorVertical: false,
                                    opacity: 1,
                                    rotation: 0,
                                    visible: true,
                                    width: 0.28,
                                    x: 0.5,
                                    y: 0.5,
                                    zIndex: 1
                                }
                            },
                            {
                                deviceId: "browser-cam-secondary",
                                isPrimary: false,
                                label: "Browser Camera B",
                                sourceId: "scene-cam-b",
                                transform: {
                                    height: 0.22,
                                    includeInOutput: true,
                                    mirrorHorizontal: false,
                                    mirrorVertical: false,
                                    opacity: 0.92,
                                    rotation: 0,
                                    visible: true,
                                    width: 0.22,
                                    x: 0.18,
                                    y: 0.18,
                                    zIndex: 2
                                }
                            }
                        ]
                    });

                    const session = {};
                    await composer.ensureProgramSession(session, request);

                    const firstRequests = window["__prompterOneMediaHarness"].getRequestLog();
                    const firstVideoRequestCount = firstRequests.filter(request => request.hasVideo).length;
                    const firstAudioRequestCount = firstRequests.filter(request => request.hasAudio).length;

                    await composer.cleanupProgramSession(session);

                    const cleanupState = window["__prompterOneGoLiveCleanupHarness"];

                    await composer.ensureProgramSession(session, request);

                    const restartRequests = window["__prompterOneMediaHarness"].getRequestLog();
                    const restartVideoRequestCount = restartRequests.filter(request => request.hasVideo).length;
                    const restartAudioRequestCount = restartRequests.filter(request => request.hasAudio).length;

                    return {
                        cleanupState,
                        firstAudioRequestCount,
                        firstVideoRequestCount,
                        restartAudioRequestCount,
                        restartVideoRequestCount
                    };
                }
                """);

            var cleanupState = cleanupResult.GetProperty("cleanupState");
            var firstVideoRequestCount = cleanupResult.GetProperty("firstVideoRequestCount").GetInt32();
            var firstAudioRequestCount = cleanupResult.GetProperty("firstAudioRequestCount").GetInt32();
            var restartVideoRequestCount = cleanupResult.GetProperty("restartVideoRequestCount").GetInt32();
            var restartAudioRequestCount = cleanupResult.GetProperty("restartAudioRequestCount").GetInt32();

            Assert.True(firstVideoRequestCount > 0);
            Assert.True(firstAudioRequestCount > 0);
            Assert.True(cleanupState.GetProperty("releaseSharedCameraTrackCalls").GetInt32() >= firstVideoRequestCount);
            Assert.True(cleanupState.GetProperty("audioTrackStopCalls").GetInt32() >= firstAudioRequestCount);
            Assert.True(restartVideoRequestCount > firstVideoRequestCount);
            Assert.True(restartAudioRequestCount > firstAudioRequestCount);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
