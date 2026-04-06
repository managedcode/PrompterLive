using System.Globalization;
using System.Threading;

namespace PrompterOne.Web.Tests;

internal sealed class CultureScope : IDisposable
{
    private static readonly object ScopeGate = new();
    private readonly CultureInfo _originalCulture;
    private readonly CultureInfo _originalUiCulture;
    private readonly CultureInfo? _originalDefaultCulture;
    private readonly CultureInfo? _originalDefaultUiCulture;

    public CultureScope()
    {
        Monitor.Enter(ScopeGate);
        _originalCulture = CultureInfo.CurrentCulture;
        _originalUiCulture = CultureInfo.CurrentUICulture;
        _originalDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
        _originalDefaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;
    }

    public CultureScope(string cultureName)
        : this()
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        CultureInfo.CurrentUICulture = _originalUiCulture;
        CultureInfo.DefaultThreadCurrentCulture = _originalDefaultCulture;
        CultureInfo.DefaultThreadCurrentUICulture = _originalDefaultUiCulture;
        Monitor.Exit(ScopeGate);
    }
}
