using TUnit.Core.Interfaces;

namespace PrompterOne.Testing;

/// <summary>
/// Provides environment-aware parallel test limits using standard CI markers.
/// </summary>
public abstract class EnvironmentAwareParallelLimitBase : IParallelLimit
{
    protected virtual int CiLimit { get; } = 4;
    protected virtual int LocalLimit { get; } = 15;

    public int Limit => ResolveLimit();

    protected int ResolveLimit() =>
        TestEnvironment.IsCiEnvironment
            ? CiLimit
            : LocalLimit;
}
