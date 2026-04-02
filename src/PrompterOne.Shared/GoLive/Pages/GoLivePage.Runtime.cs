using PrompterOne.Core.Models.Media;
using PrompterOne.Shared.Services;

namespace PrompterOne.Shared.Pages;

public partial class GoLivePage
{
    private GoLiveOutputRuntimeRequest BuildRuntimeRequest(SceneCameraSource? camera) =>
        GoLiveOutputRequestFactory.Build(
            camera,
            MediaSceneService.State,
            _studioSettings.Streaming,
            _recordingPreferences,
            _sessionTitle);
}
