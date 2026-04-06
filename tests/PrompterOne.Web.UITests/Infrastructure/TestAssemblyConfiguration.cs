[assembly: ParallelLimiter<PrompterOne.Web.UITests.UiTestParallelLimit>]

namespace PrompterOne.Web.UITests;

public sealed record UiTestParallelLimit : IParallelLimit
{
    public int Limit => UiTestParallelization.DefaultWorkerLimit;
}

internal static class UiTestParallelization
{
    public const int DefaultWorkerLimit = 4;
    public const string EditorAuthoringConstraintKey = nameof(EditorAuthoringConstraintKey);
    public const string EditorPerformanceConstraintKey = nameof(EditorPerformanceConstraintKey);
}
