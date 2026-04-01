namespace PrompterOne.Core.Abstractions;

public interface IUserSettingsStore
{
    Task<T?> LoadAsync<T>(string key, CancellationToken cancellationToken = default);

    Task SaveAsync<T>(string key, T value, CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
