namespace PrompterOne.Shared.Contracts;

public static partial class UiTestIds
{
    public static class Onboarding
    {
        public const string Surface = "onboarding-surface";
        public const string Eyebrow = "onboarding-eyebrow";
        public const string Title = "onboarding-title";
        public const string Body = "onboarding-body";
        public const string Progress = "onboarding-progress";
        public const string Back = "onboarding-back";
        public const string Next = "onboarding-next";
        public const string Dismiss = "onboarding-dismiss";

        public static string Step(string stepId) => $"onboarding-step-{stepId}";
    }
}
