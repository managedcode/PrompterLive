using PrompterOne.Core.Abstractions;
using PrompterOne.Shared.Services;

namespace PrompterOne.Shared.Services.Editor;

public sealed class EditorBlockAttachmentStore(IUserSettingsStore settingsStore)
{
    public async Task<IReadOnlyList<EditorBlockAttachment>> LoadAsync(
        string? scriptId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scriptId))
        {
            return [];
        }

        return await settingsStore.LoadAsync<IReadOnlyList<EditorBlockAttachment>>(
                BuildKey(scriptId),
                cancellationToken)
            ?? [];
    }

    public Task SaveAsync(
        string? scriptId,
        IReadOnlyList<EditorBlockAttachment> attachments,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scriptId))
        {
            return Task.CompletedTask;
        }

        return attachments.Count == 0
            ? settingsStore.RemoveAsync(BuildKey(scriptId), cancellationToken)
            : settingsStore.SaveAsync(BuildKey(scriptId), attachments, cancellationToken);
    }

    private static string BuildKey(string scriptId) =>
        string.Concat(BrowserStorageKeys.EditorBlockAttachmentKeyPrefix, scriptId.Trim());
}
