namespace PrompterOne.Shared.Contracts;

public static partial class UiTestIds
{
    public static class Tooltip
    {
        public static string Anchor(string ownerTestId) => $"{ownerTestId}-tooltip-anchor";

        public static string Surface(string ownerTestId) => $"{ownerTestId}-tooltip";
    }
}
