using PrompterOne.Testing;

[assembly: ParallelLimiter<PrompterOne.Web.UITests.Reader.MaxParallelTestsForPipeline>]

namespace PrompterOne.Web.UITests.Reader;

public sealed class MaxParallelTestsForPipeline : EnvironmentAwareParallelLimitBase
{
    protected override int CiLimit { get; } = 2;
    protected override int LocalLimit { get; } = 15;
}
