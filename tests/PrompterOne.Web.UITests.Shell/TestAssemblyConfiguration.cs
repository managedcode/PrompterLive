using PrompterOne.Testing;

[assembly: ParallelLimiter<PrompterOne.Web.UITests.Shell.MaxParallelTestsForPipeline>]

namespace PrompterOne.Web.UITests.Shell;

public sealed class MaxParallelTestsForPipeline : EnvironmentAwareParallelLimitBase
{
    protected override int LocalLimit { get; } = 15;
}
