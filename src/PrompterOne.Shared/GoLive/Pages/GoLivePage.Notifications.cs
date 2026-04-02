namespace PrompterOne.Shared.Pages;

public partial class GoLivePage
{
    private static readonly TimeSpan SessionRefreshInterval = TimeSpan.FromMilliseconds(200);

    private CancellationTokenSource? _sessionRefreshCancellation;
    private PeriodicTimer? _sessionRefreshTimer;
    private bool _disposed;

    protected override void OnInitialized()
    {
        GoLiveSession.StateChanged += HandleGoLiveSessionChanged;
        GoLiveOutputRuntime.StateChanged += HandleGoLiveOutputRuntimeChanged;
        GoLiveRemoteSourceRuntime.StateChanged += HandleGoLiveRemoteSourceRuntimeChanged;
        UpdateSessionRefreshLoop();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _ = StopPrimaryMicrophoneMonitorAsync();
        _ = GoLiveRemoteSourceRuntime.StopAsync();
        DisposeCore();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await StopPrimaryMicrophoneMonitorAsync();
        await GoLiveRemoteSourceRuntime.StopAsync();
        DisposeCore();
        GC.SuppressFinalize(this);
    }

    private void HandleGoLiveSessionChanged()
    {
        UpdateSessionRefreshLoop();
        _ = InvokeAsync(StateHasChanged);
    }

    private void HandleGoLiveOutputRuntimeChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    private void HandleGoLiveRemoteSourceRuntimeChanged()
    {
        ApplyRemoteSourceState();
        _ = InvokeAsync(StateHasChanged);
    }

    private void UpdateSessionRefreshLoop()
    {
        if (GoLiveSession.State.HasActiveSession || HasSourceIntakeConnections)
        {
            EnsureSessionRefreshLoop();
            return;
        }

        StopSessionRefreshLoop();
    }

    private void EnsureSessionRefreshLoop()
    {
        if (_sessionRefreshTimer is not null)
        {
            return;
        }

        _sessionRefreshCancellation = new CancellationTokenSource();
        _sessionRefreshTimer = new PeriodicTimer(SessionRefreshInterval);
        _ = RunSessionRefreshLoopAsync(_sessionRefreshTimer, _sessionRefreshCancellation.Token);
    }

    private async Task RunSessionRefreshLoopAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (HasSourceIntakeConnections)
                {
                    await InvokeAsync(SyncRemoteSourcesAsync);
                }

                if (GoLiveSession.State.HasActiveSession)
                {
                    await InvokeAsync(GoLiveOutputRuntime.RefreshStateAsync);
                }

                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void StopSessionRefreshLoop()
    {
        _sessionRefreshCancellation?.Cancel();
        _sessionRefreshCancellation?.Dispose();
        _sessionRefreshCancellation = null;
        _sessionRefreshTimer?.Dispose();
        _sessionRefreshTimer = null;
    }

    private void DisposeCore()
    {
        GoLiveSession.StateChanged -= HandleGoLiveSessionChanged;
        GoLiveOutputRuntime.StateChanged -= HandleGoLiveOutputRuntimeChanged;
        GoLiveRemoteSourceRuntime.StateChanged -= HandleGoLiveRemoteSourceRuntimeChanged;
        StopSessionRefreshLoop();
        DisposePrimaryMicrophoneObserver();
        _interactionGate.Dispose();
    }
}
