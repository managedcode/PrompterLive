using PrompterOne.Testing;

[assembly: ParallelLimiter<PrompterOne.Web.Tests.MaxParallelTestsForPipeline>]

namespace PrompterOne.Web.Tests;

/// <summary>
/// Limits parallel test execution to reduce resource contention in CI.
/// </summary>
public sealed class MaxParallelTestsForPipeline : EnvironmentAwareParallelLimitBase
{
    protected override int LocalLimit { get; } = 15;
}
