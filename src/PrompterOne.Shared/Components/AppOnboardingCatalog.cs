using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Localization;

namespace PrompterOne.Shared.Components;

internal static class AppOnboardingCatalog
{
    public static IReadOnlyList<AppOnboardingStepDescriptor> Build(string scriptId) =>
    [
        CreateStep(
            AppOnboardingStepId.Library,
            UiIconKind.Folder,
            UiTextKey.OnboardingStepLibrary,
            UiTextKey.OnboardingWelcomeEyebrow,
            UiTextKey.OnboardingWelcomeTitle,
            UiTextKey.OnboardingWelcomeBody,
            [
                UiTextKey.OnboardingWelcomeBulletSearch,
                UiTextKey.OnboardingWelcomeBulletModes,
                UiTextKey.OnboardingWelcomeBulletLocal
            ],
            UiTextKey.OnboardingOpenTps,
            AppRoutes.Library),
        CreateStep(
            AppOnboardingStepId.Tps,
            UiIconKind.CodeArrows,
            UiTextKey.OnboardingStepTps,
            UiTextKey.OnboardingTpsEyebrow,
            UiTextKey.OnboardingTpsTitle,
            UiTextKey.OnboardingTpsBody,
            [
                UiTextKey.OnboardingTpsBulletFormat,
                UiTextKey.OnboardingTpsBulletRuntime,
                UiTextKey.OnboardingTpsBulletGlossary
            ],
            UiTextKey.OnboardingOpenEditor,
            AppRoutes.EditorWithId(scriptId)),
        CreateStep(
            AppOnboardingStepId.Editor,
            UiIconKind.DocumentLines,
            UiTextKey.OnboardingStepEditor,
            UiTextKey.OnboardingEditorEyebrow,
            UiTextKey.OnboardingEditorTitle,
            UiTextKey.OnboardingEditorBody,
            [
                UiTextKey.OnboardingEditorBulletTps,
                UiTextKey.OnboardingEditorBulletMetadata,
                UiTextKey.OnboardingEditorBulletAuthoring
            ],
            UiTextKey.OnboardingOpenLearn,
            AppRoutes.EditorWithId(scriptId)),
        CreateStep(
            AppOnboardingStepId.Learn,
            UiIconKind.BookOpen,
            UiTextKey.OnboardingStepLearn,
            UiTextKey.OnboardingLearnEyebrow,
            UiTextKey.OnboardingLearnTitle,
            UiTextKey.OnboardingLearnBody,
            [
                UiTextKey.OnboardingLearnBulletRsvp,
                UiTextKey.OnboardingLearnBulletTiming,
                UiTextKey.OnboardingLearnBulletContext
            ],
            UiTextKey.OnboardingOpenTeleprompter,
            AppRoutes.LearnWithId(scriptId)),
        CreateStep(
            AppOnboardingStepId.Teleprompter,
            UiIconKind.Monitor,
            UiTextKey.OnboardingStepTeleprompter,
            UiTextKey.OnboardingTeleprompterEyebrow,
            UiTextKey.OnboardingTeleprompterTitle,
            UiTextKey.OnboardingTeleprompterBody,
            [
                UiTextKey.OnboardingTeleprompterBulletMirror,
                UiTextKey.OnboardingTeleprompterBulletFocus,
                UiTextKey.OnboardingTeleprompterBulletPlayback
            ],
            UiTextKey.OnboardingOpenGoLive,
            AppRoutes.TeleprompterWithId(scriptId)),
        CreateStep(
            AppOnboardingStepId.GoLive,
            UiIconKind.Broadcast,
            UiTextKey.OnboardingStepGoLive,
            UiTextKey.OnboardingGoLiveEyebrow,
            UiTextKey.OnboardingGoLiveTitle,
            UiTextKey.OnboardingGoLiveBody,
            [
                UiTextKey.OnboardingGoLiveBulletProgram,
                UiTextKey.OnboardingGoLiveBulletRecording,
                UiTextKey.OnboardingGoLiveBulletRouting
            ],
            UiTextKey.OnboardingFinish,
            AppRoutes.GoLiveWithId(scriptId))
    ];

    private static AppOnboardingStepDescriptor CreateStep(
        AppOnboardingStepId stepId,
        UiIconKind iconKind,
        UiTextKey stepLabelKey,
        UiTextKey eyebrowKey,
        UiTextKey titleKey,
        UiTextKey bodyKey,
        IReadOnlyList<UiTextKey> bulletKeys,
        UiTextKey actionKey,
        string route) =>
        new(
            StepId: stepId,
            IconKind: iconKind,
            StepLabelKey: stepLabelKey,
            EyebrowKey: eyebrowKey,
            TitleKey: titleKey,
            BodyKey: bodyKey,
            BulletKeys: bulletKeys,
            ActionKey: actionKey,
            Route: route);
}
