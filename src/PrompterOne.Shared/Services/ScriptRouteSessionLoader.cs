using PrompterOne.Core.Abstractions;

namespace PrompterOne.Shared.Services;

internal static class ScriptRouteSessionLoader
{
    public static async Task<bool> EnsureRequestedSessionAsync(
        string? scriptId,
        IScriptRepository scriptRepository,
        IScriptSessionService sessionService)
    {
        if (string.IsNullOrWhiteSpace(scriptId))
        {
            return false;
        }

        var document = await scriptRepository.GetAsync(scriptId);
        if (document is null)
        {
            await sessionService.NewAsync();
            return false;
        }

        if (!string.Equals(sessionService.State.ScriptId, document.Id, StringComparison.Ordinal))
        {
            await sessionService.OpenAsync(document);
        }

        return true;
    }
}
