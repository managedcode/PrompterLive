using TUnit.Core.Interfaces;

namespace PrompterOne.Testing;

/// <summary>
/// Provides environment-aware parallel test limits using standard CI markers.
/// </summary>
public abstract class EnvironmentAwareParallelLimitBase : IParallelLimit
{
    protected virtual int CiLimit { get; } = 8;
    protected virtual int LocalLimit { get; } = 4;

    public int Limit => ResolveLimit();

    private int ResolveLimit() =>
        TestEnvironment.IsCiEnvironment
            ? CiLimit
            : LocalLimit;
}
