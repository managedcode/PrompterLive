using System.Text.Json;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.App.UITests;

public sealed class GoLiveOutputFailureRollbackTests(StandaloneAppFixture fixture) : IClassFixture<StandaloneAppFixture>
{
    private const int ExpectedCallCount = 1;
    private const int LiveKitPlatformKindValue = 0;
    private const int SourceAndPublishRoleValue = BrowserTestConstants.GoLive.SourceAndPublishRoleValue;

    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
    public async Task GoLivePage_StartStream_VdoNinjaPublishFailure_RollsBackProgramSession()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await GoLiveFlowTests.SeedGoLiveSceneForReuseAsync(page);
            await GoLiveFlowTests.SeedGoLiveOperationalSettingsAsync(page);
            await page.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();
            await page.EvaluateAsync(BrowserTestConstants.GoLive.InstallVdoNinjaHarnessScript);
            await page.EvaluateAsync(BuildVdoNinjaPublishFailurePatchScript());

            await page.GetByTestId(UiTestIds.GoLive.StartStream).ClickAsync();
            await page.WaitForFunctionAsync(
                BrowserTestConstants.GoLive.VdoNinjaHarnessReadyScript,
                null,
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await WaitForRuntimeSessionClearedAsync(page);

            var harnessState = await page.EvaluateAsync<JsonElement>(BrowserTestConstants.GoLive.GetVdoNinjaHarnessScript);

            Assert.Equal(ExpectedCallCount, harnessState.GetProperty("connectCalls").GetArrayLength());
            Assert.Equal(ExpectedCallCount, harnessState.GetProperty("joinRoomCalls").GetArrayLength());
            Assert.Equal(ExpectedCallCount, harnessState.GetProperty("publishCalls").GetArrayLength());
            Assert.Equal(ExpectedCallCount, harnessState.GetProperty("leaveRoomCount").GetInt32());
            Assert.Equal(ExpectedCallCount, harnessState.GetProperty("disconnectCount").GetInt32());
            Assert.Equal(0, harnessState.GetProperty("stopPublishingCount").GetInt32());
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task GoLivePage_StartStream_LiveKitConnectFailure_RollsBackProgramSession()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await GoLiveFlowTests.SeedGoLiveSceneForReuseAsync(page);
            await SeedLiveKitOnlyOperationalSettingsAsync(page);
            await page.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();
            await page.EvaluateAsync(BrowserTestConstants.GoLive.InstallLiveKitHarnessScript);
            await page.EvaluateAsync(BuildLiveKitConnectFailurePatchScript());

            await page.GetByTestId(UiTestIds.GoLive.StartStream).ClickAsync();
            await WaitForRuntimeSessionClearedAsync(page);

            var harnessState = await page.EvaluateAsync<JsonElement>(BrowserTestConstants.GoLive.GetLiveKitHarnessScript);

            Assert.Equal(ExpectedCallCount, harnessState.GetProperty("connectCalls").GetArrayLength());
            Assert.Equal(ExpectedCallCount, harnessState.GetProperty("disconnectCount").GetInt32());
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static string BuildVdoNinjaPublishFailurePatchScript() => """
        () => {
            const harness = window.__prompterOneVdoNinjaHarness;
            const sdk = window.VDONinjaSDK || window.VDONinja;
            if (!harness || !sdk?.prototype) {
                throw new Error('VDO.Ninja harness is not available.');
            }

            sdk.prototype.publish = async function(stream, options) {
                const trackKinds = Array.from(stream?.getTracks?.() ?? []).map(track => track?.kind ?? null);
                harness.publishCalls.push({
                    label: options?.label ?? null,
                    room: options?.room ?? null,
                    streamId: options?.streamID ?? null,
                    trackKinds
                });
                throw new Error('Forced VDO.Ninja publish failure from browser test.');
            };
        }
        """;

    private static string BuildLiveKitConnectFailurePatchScript() => """
        () => {
            const harness = window.__prompterOneLiveKitHarness;
            const room = window.LivekitClient?.Room;
            if (!harness || !room?.prototype) {
                throw new Error('LiveKit harness is not available.');
            }

            room.prototype.connect = async function(url, token) {
                harness.connectCalls.push({ url, token });
                throw new Error('Forced LiveKit connect failure from browser test.');
            };
        }
        """;

    private static Task SeedLiveKitOnlyOperationalSettingsAsync(Microsoft.Playwright.IPage page) =>
        page.EvaluateAsync(
            BuildLiveKitOnlyOperationalSettingsScript(),
            new object[]
            {
                BrowserTestConstants.GoLive.StoredStudioSettingsKey,
                BrowserTestConstants.GoLive.LiveKitServer,
                BrowserTestConstants.GoLive.LiveKitRoom,
                BrowserTestConstants.GoLive.LiveKitToken,
                BrowserTestConstants.GoLive.FirstSourceId
            });

    private static async Task WaitForRuntimeSessionClearedAsync(Microsoft.Playwright.IPage page)
    {
        var deadline = DateTimeOffset.UtcNow.AddMilliseconds(BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs);
        JsonElement? lastRuntimeState = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            lastRuntimeState = await page.EvaluateAsync<JsonElement?>(
                BrowserTestConstants.GoLive.GetRuntimeStateScript,
                BrowserTestConstants.GoLive.RuntimeSessionId);

            if (!lastRuntimeState.HasValue)
            {
                return;
            }

            await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.DiagnosticPollDelayMs);
        }

        Assert.Fail($"Go Live runtime session did not clear. LastState={SerializeJsonState(lastRuntimeState)}");
    }

    private static string SerializeJsonState(JsonElement? state) =>
        state.HasValue ? state.Value.GetRawText() : "null";

    private static string BuildLiveKitOnlyOperationalSettingsScript() => $$"""
        ([storageKey, liveKitServer, liveKitRoom, liveKitToken, primarySourceId]) => {
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
                            Id: '{{BrowserTestConstants.GoLive.LiveKitTransportId}}',
                            Name: 'LiveKit',
                            PlatformKind: {{LiveKitPlatformKindValue}},
                            Roles: {{SourceAndPublishRoleValue}},
                            IsEnabled: true,
                            ServerUrl: liveKitServer,
                            BaseUrl: '',
                            RoomName: liveKitRoom,
                            Token: liveKitToken,
                            PublishUrl: '',
                            ViewUrl: ''
                        }
                    ],
                    DistributionTargets: [],
                    SourceSelections: [
                        { TargetId: '{{BrowserTestConstants.GoLive.LiveKitTransportId}}', SourceIds: [primarySourceId] }
                    ]
                }
            }));
        }
        """;
}
