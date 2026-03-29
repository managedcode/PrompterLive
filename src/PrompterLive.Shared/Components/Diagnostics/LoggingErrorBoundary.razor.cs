using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using PrompterLive.Shared.Services.Diagnostics;

namespace PrompterLive.Shared.Components.Diagnostics;

public class LoggingErrorBoundaryBase : ErrorBoundary
{
    private const string BoundaryOperation = "Unhandled UI render";

    [Inject] private UiDiagnosticsService Diagnostics { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private ILogger<LoggingErrorBoundaryBase> Logger { get; set; } = null!;

    protected UiDiagnosticEntry? CurrentDiagnostic => Diagnostics.Current;

    protected override Task OnErrorAsync(Exception exception)
    {
        Logger.LogCritical(exception, "Unhandled UI exception reached the global error boundary.");
        Diagnostics.ReportFatal(BoundaryOperation, exception);
        return Task.CompletedTask;
    }

    protected void HandleRecover()
    {
        Diagnostics.Clear();
        Recover();
    }

    protected void HandleReturnHome()
    {
        Diagnostics.Clear();
        Recover();
        Navigation.NavigateTo("/library", replace: true);
    }
}
