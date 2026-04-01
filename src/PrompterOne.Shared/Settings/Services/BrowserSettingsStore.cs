using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using PrompterOne.Core.Abstractions;

namespace PrompterOne.Shared.Services;

public sealed class BrowserSettingsStore(IJSRuntime jsRuntime, ILogger<BrowserSettingsStore>? logger = null) : IUserSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IJSRuntime _jsRuntime = jsRuntime;
    private readonly ILogger<BrowserSettingsStore> _logger = logger ?? NullLogger<BrowserSettingsStore>.Instance;

    public async Task<T?> LoadAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Loading browser setting {Key}.", key);
            var json = await _jsRuntime.InvokeAsync<string?>(
                BrowserStorageMethodNames.LoadSettingJson,
                cancellationToken,
                ResolveStorageKey(key));

            return string.IsNullOrWhiteSpace(json)
                ? default
                : JsonSerializer.Deserialize<T>(json, JsonOptions);
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
            var json = JsonSerializer.Serialize(value, JsonOptions);
            await _jsRuntime.InvokeVoidAsync(
                BrowserStorageMethodNames.SaveSettingJson,
                cancellationToken,
                ResolveStorageKey(key),
                json);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to save browser setting {Key}.", key);
            throw;
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Removing browser setting {Key}.", key);
            await _jsRuntime.InvokeVoidAsync(
                BrowserStorageMethodNames.RemoveStorageValue,
                cancellationToken,
                ResolveStorageKey(key));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to remove browser setting {Key}.", key);
            throw;
        }
    }

    private static string ResolveStorageKey(string key) =>
        string.Concat(BrowserStorageKeys.SettingsPrefix, key);
}
