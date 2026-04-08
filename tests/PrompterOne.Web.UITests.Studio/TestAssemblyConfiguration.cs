using PrompterOne.Testing;

[assembly: ParallelLimiter<PrompterOne.Web.UITests.Studio.MaxParallelTestsForPipeline>]

namespace PrompterOne.Web.UITests.Studio;

public sealed class MaxParallelTestsForPipeline : EnvironmentAwareParallelLimitBase
{
}
