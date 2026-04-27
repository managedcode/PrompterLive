using Microsoft.JSInterop;

namespace PrompterOne.Shared.Services;

public sealed class AppShellFilePickerInterop(IJSRuntime jsRuntime) : IDisposable, IAsyncDisposable
{
    private const string FileSaveUnavailableMessage = "File save is not available.";

    private readonly IJSRuntime _jsRuntime = jsRuntime;
    private Task<IJSObjectReference?>? _moduleTask;

    public async Task OpenAsync(string inputId)
    {
        var module = await GetModuleAsync();
        if (module is null)
        {
            return;
        }

        await module.InvokeVoidAsync(
            AppShellFilePickerInteropMethodNames.OpenFilePicker,
            inputId);
    }

    public async Task<AppShellFileSaveMode> SaveTextAsync(
        string suggestedFileName,
        string text,
        string mimeType,
        string description,
        IReadOnlyList<string> extensions,
        bool preferSavePicker = true)
    {
        var module = await GetModuleAsync() ?? throw new InvalidOperationException(FileSaveUnavailableMessage);

        var normalizedExtensions = extensions
            .Where(extension => !string.IsNullOrWhiteSpace(extension))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var result = await module.InvokeAsync<string>(
            AppShellFilePickerInteropMethodNames.SaveTextFile,
            suggestedFileName ?? string.Empty,
            text ?? string.Empty,
            mimeType ?? string.Empty,
            description ?? string.Empty,
            normalizedExtensions,
            preferSavePicker);

        return result switch
        {
            AppShellFilePickerInteropMethodNames.FileSaveModeFileSystem => AppShellFileSaveMode.FileSystem,
            AppShellFilePickerInteropMethodNames.FileSaveModeDownload => AppShellFileSaveMode.Download,
            _ => AppShellFileSaveMode.Cancelled
        };
    }

    public async Task<AppShellFileSaveMode> SaveBytesAsync(
        string suggestedFileName,
        byte[] content,
        string mimeType,
        string description,
        IReadOnlyList<string> extensions,
        bool preferSavePicker = true)
    {
        var module = await GetModuleAsync() ?? throw new InvalidOperationException(FileSaveUnavailableMessage);

        var normalizedExtensions = extensions
            .Where(extension => !string.IsNullOrWhiteSpace(extension))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var result = await module.InvokeAsync<string>(
            AppShellFilePickerInteropMethodNames.SaveBinaryFile,
            suggestedFileName ?? string.Empty,
            content ?? [],
            mimeType ?? string.Empty,
            description ?? string.Empty,
            normalizedExtensions,
            preferSavePicker);

        return result switch
        {
            AppShellFilePickerInteropMethodNames.FileSaveModeFileSystem => AppShellFileSaveMode.FileSystem,
            AppShellFilePickerInteropMethodNames.FileSaveModeDownload => AppShellFileSaveMode.Download,
            _ => AppShellFileSaveMode.Cancelled
        };
    }

    public void Dispose()
    {
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask is null)
        {
            return;
        }

        var module = await _moduleTask;
        if (module is not null)
        {
            await module.DisposeAsync();
        }
    }

    private Task<IJSObjectReference?> GetModuleAsync() =>
        _moduleTask ??= ImportModuleAsync();

    private async Task<IJSObjectReference?> ImportModuleAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<IJSObjectReference>(
                AppShellFilePickerInteropMethodNames.JSImportMethodName,
                AppShellFilePickerInteropMethodNames.ModulePath);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (JSException)
        {
            return null;
        }
    }
}
