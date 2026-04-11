namespace PrompterOne.Shared.Services;

internal sealed class CallbackDisposable(Action callback) : IDisposable
{
    private bool _isDisposed;

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        callback();
    }
}
