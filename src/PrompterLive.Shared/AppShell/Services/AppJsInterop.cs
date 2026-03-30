namespace PrompterLive.Shared.Services;

public static class AppJsInterop
{
    public const string AttachShellDiagnosticsLoggerMethod = "PrompterLive.shell.attachDiagnosticsLogger";
    public const string AttachNavigatorMethod = "PrompterLiveDesign.attachNavigator";
    public const string ChangeRsvpSpeedMethod = "PrompterLiveDesign.changeRsvpSpeed";
    public const string ChangeReaderFontSizeMethod = "PrompterLiveDesign.changeReaderFontSize";
    public const string ChangeReaderFocalPointMethod = "PrompterLiveDesign.changeReaderFocalPoint";
    public const string ChangeReaderTextWidthMethod = "PrompterLiveDesign.changeReaderTextWidth";
    public const string InitializeDesignMethod = "PrompterLiveDesign.initialize";
    public const string JumpReaderCardMethod = "PrompterLiveDesign.jumpReaderCard";
    public const string SetRsvpTimelineMethod = "PrompterLiveDesign.setRsvpTimeline";
    public const string StepRsvpWordMethod = "PrompterLiveDesign.stepRsvpWord";
    public const string StepReaderWordMethod = "PrompterLiveDesign.stepReaderWord";
    public const string ToggleRsvpPlaybackMethod = "PrompterLiveDesign.toggleRsvpPlayback";
    public const string ToggleReaderCameraMethod = "PrompterLiveDesign.toggleReaderCamera";
    public const string ToggleReaderPlaybackMethod = "PrompterLiveDesign.toggleReaderPlayback";
}
