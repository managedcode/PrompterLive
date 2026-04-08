using PrompterOne.Testing;

[assembly: ParallelLimiter<PrompterOne.Web.UITests.Studio.MaxParallelTestsForPipeline>]

namespace PrompterOne.Web.UITests.Studio;

public sealed class MaxParallelTestsForPipeline : EnvironmentAwareParallelLimitBase
{
    protected override int CiLimit { get; } = 2;
    protected override int LocalLimit { get; } = 15;
}
