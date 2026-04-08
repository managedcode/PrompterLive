using PrompterOne.Testing;

[assembly: ParallelLimiter<PrompterOne.Web.UITests.Editor.MaxParallelTestsForPipeline>]

namespace PrompterOne.Web.UITests.Editor;

public sealed class MaxParallelTestsForPipeline : EnvironmentAwareParallelLimitBase
{
    protected override int LocalLimit { get; } = 15;
}
