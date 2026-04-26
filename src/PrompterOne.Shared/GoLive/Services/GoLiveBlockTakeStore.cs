using PrompterOne.Core.Abstractions;
using PrompterOne.Shared.Services;

namespace PrompterOne.Shared.Services;

public sealed record GoLiveBlockTakeRecord(
    string Id,
    string BlockKey,
    string BlockTitle,
    int TakeNumber,
    string FileName,
    string MimeType,
    string SaveMode,
    long SizeBytes,
    DateTimeOffset RecordedAt);

public sealed class GoLiveBlockTakeStore(IUserSettingsStore settingsStore)
{
    public async Task<IReadOnlyList<GoLiveBlockTakeRecord>> LoadAsync(
        string? scriptId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scriptId))
        {
            return [];
        }

        return await settingsStore.LoadAsync<IReadOnlyList<GoLiveBlockTakeRecord>>(
                BuildKey(scriptId),
                cancellationToken)
            ?? [];
    }

    public Task SaveAsync(
        string? scriptId,
        IReadOnlyList<GoLiveBlockTakeRecord> takes,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scriptId))
        {
            return Task.CompletedTask;
        }

        return takes.Count == 0
            ? settingsStore.RemoveAsync(BuildKey(scriptId), cancellationToken)
            : settingsStore.SaveAsync(BuildKey(scriptId), takes, cancellationToken);
    }

    private static string BuildKey(string scriptId) =>
        string.Concat(BrowserStorageKeys.GoLiveBlockTakeKeyPrefix, scriptId.Trim());
}
