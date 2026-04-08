using PrompterOne.Testing;

[assembly: ParallelLimiter<PrompterOne.Core.Tests.MaxParallelTestsForPipeline>]

namespace PrompterOne.Core.Tests;

/// <summary>
/// Limits parallel test execution to reduce resource contention in CI.
/// </summary>
public sealed class MaxParallelTestsForPipeline : EnvironmentAwareParallelLimitBase
{
    protected override int LocalLimit { get; } = 10;
}
