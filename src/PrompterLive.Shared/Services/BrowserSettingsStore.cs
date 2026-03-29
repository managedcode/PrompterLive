using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

namespace PrompterLive.Shared.Services;

public sealed class BrowserSettingsStore(IJSRuntime jsRuntime, ILogger<BrowserSettingsStore>? logger = null)
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;
    private readonly ILogger<BrowserSettingsStore> _logger = logger ?? NullLogger<BrowserSettingsStore>.Instance;

    public async Task<T?> LoadAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Loading browser setting {Key}.", key);
            return await _jsRuntime.InvokeAsync<T?>("PrompterLive.settings.load", cancellationToken, key);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to load browser setting {Key}.", key);
            throw;
        }
    }

    public async Task SaveAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Saving browser setting {Key}.", key);
            await _jsRuntime.InvokeVoidAsync("PrompterLive.settings.save", cancellationToken, key, value);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to save browser setting {Key}.", key);
            throw;
        }
    }
}
