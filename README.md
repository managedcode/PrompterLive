# PrompterLive

`PrompterLive` is a browser-first teleprompter and rehearsal tool for authors, speakers, streamers, editors, and production teams.

It is not a server product and not a desktop wrapper. The current runtime shape is a **standalone Blazor WebAssembly app** that boots directly in the browser, stores working state on the client, uses browser camera and microphone APIs, and can publish its WebAssembly build to GitHub Pages.

## Production URL

- GitHub Pages target: [https://managedcode.github.io/PrompterLive/](https://managedcode.github.io/PrompterLive/)
- Publish workflow: [`.github/workflows/deploy-github-pages.yml`](.github/workflows/deploy-github-pages.yml)

`PrompterLive` is published as a repository Pages site. After the publish workflow runs, the WebAssembly artifact is served as a static site with no separate backend.

## What It Is

`PrompterLive` combines several workflows into a single product:

- `Library` for script and folder management
- `Editor` for TPS authoring and speech-structure editing
- `Learn` for RSVP rehearsal mode
- `Teleprompter` for the classic reading surface
- `Go Live` for browser-side live output, preview, and routing
- `Settings` for camera, microphone, theme, and runtime preferences

Architecture map: [docs/Architecture.md](docs/Architecture.md)

## Key Properties

- standalone Blazor WebAssembly runtime with no server backend
- UI ported from [`new-design/`](new-design/) as the design source of truth
- TPS-focused editor for prompt-ready scripts
- RSVP/Learn mode with ORP-style word rendering
- browser-side media scene, device setup, and live preview
- Go Live runtime with LiveKit and VDO.Ninja outputs
- browser-local document and settings storage
- browser-realistic acceptance tests through Playwright

## RSVP Inspiration

`PrompterLive` does not visually copy `Squirt`, but the `Learn` mode is directly inspired by the RSVP logic and pacing approach behind Squirt-style readers.

In practice that means:

- ORP-style focus on a key letter inside each word
- word timing that reacts to word length and punctuation
- natural pauses after commas, periods, and stronger phrase boundaries
- a custom UI and layout adapted to `PrompterLive`, not a raw Squirt clone

Canonical inspiration: [cameron/squirt](https://github.com/cameron/squirt)

## Technical Model

- host: [`src/PrompterLive.App`](src/PrompterLive.App)
- routed UI, CSS, browser interop: [`src/PrompterLive.Shared`](src/PrompterLive.Shared)
- reusable domain logic: [`src/PrompterLive.Core`](src/PrompterLive.Core)
- automated tests: [`tests/`](tests/)
- visual reference: [`new-design/`](new-design/)

Further reading:

- architecture: [docs/Architecture.md](docs/Architecture.md)
- settings media feedback: [docs/Features/SettingsMediaFeedback.md](docs/Features/SettingsMediaFeedback.md)
- go live runtime: [docs/Features/GoLiveRuntime.md](docs/Features/GoLiveRuntime.md)
- vendored streaming SDK policy: [docs/Features/VendoredStreamingSdkReleases.md](docs/Features/VendoredStreamingSdkReleases.md)
- versioning and Pages publish: [docs/Features/AppVersioningAndGitHubPages.md](docs/Features/AppVersioningAndGitHubPages.md)

## App Version

The app version is shown in `Settings > About`.

Current scheme:

- local builds: `0.1.0`
- CI builds: `0.1.<github.run_number>`

Version source of truth:

- [`Directory.Build.props`](Directory.Build.props)
- [`src/PrompterLive.App/Program.cs`](src/PrompterLive.App/Program.cs)

Versioning and Pages workflow details: [docs/Features/AppVersioningAndGitHubPages.md](docs/Features/AppVersioningAndGitHubPages.md)

## Local Run

Requirements:

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

Run:

```bash
cd src/PrompterLive.App
dotnet run
```

Useful commands:

```bash
dotnet build /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx -warnaserror
dotnet test /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx
dotnet format /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx
dotnet test /Users/ksemenenko/Developer/PrompterLive/PrompterLive.slnx --collect:"XPlat Code Coverage"
```

## Deploy

Publish runs through the GitHub Actions workflow:

- [`.github/workflows/deploy-github-pages.yml`](.github/workflows/deploy-github-pages.yml)

What it does:

- builds `src/PrompterLive.App`
- injects the CI build number into the app version
- takes the published `wwwroot`
- rewrites `base href` for the repository Pages path
- adds `404.html` for client-side routing
- deploys the artifact to GitHub Pages

Official platform reference: [GitHub Pages](https://docs.github.com/en/pages)

## Stack

- [.NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [Blazor](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor)
- Razor Class Library
- [xUnit](https://xunit.net/)
- [bUnit](https://bunit.dev/)
- [Playwright](https://playwright.dev/)

## Browser Media And Streaming Stack

`PrompterLive` is a browser-first media app, so it relies on standard Web APIs and a thin interop layer:

- [WebRTC API](https://developer.mozilla.org/en-US/docs/Web/API/WebRTC_API)
- [`getUserMedia()`](https://developer.mozilla.org/en-US/docs/Web/API/MediaDevices/getUserMedia)
- [MediaRecorder](https://developer.mozilla.org/en-US/docs/Web/API/MediaRecorder)
- [Web Audio API](https://developer.mozilla.org/en-US/docs/Web/API/Web_Audio_API)

Integrated streaming and media projects:

- [LiveKit](https://livekit.com/)  
  docs: [Connecting to LiveKit](https://docs.livekit.io/intro/basics/connect/)  
  repo: [livekit/client-sdk-js](https://github.com/livekit/client-sdk-js)
- [VDO.Ninja](https://vdo.ninja/)  
  repo: [steveseguin/vdo.ninja](https://github.com/steveseguin/vdo.ninja)

Pinned vendored SDK policy and version tracking:

- [docs/Features/VendoredStreamingSdkReleases.md](docs/Features/VendoredStreamingSdkReleases.md)

Currently pinned in the repository:

- `livekit/client-sdk-js` `v2.18.0`
- `steveseguin/vdo.ninja` `v29.0`

## Fonts, Icons, And Visual Sources

- [Inter](https://rsms.me/inter/)
- [JetBrains Mono](https://www.jetbrains.com/lp/mono/)
- [Playfair Display](https://fonts.google.com/specimen/Playfair+Display)
- [Google Fonts](https://fonts.google.com/)
- [Feather Icons](https://feathericons.com/)

## Credits And Inspirations

These are the external projects, APIs, and references that `PrompterLive` builds on:

### Platform And Runtime

- [.NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [Blazor](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor)
- [GitHub Pages](https://docs.github.com/en/pages)

### Test Infrastructure

- [xUnit](https://xunit.net/)
- [bUnit](https://bunit.dev/)
- [Playwright](https://playwright.dev/)

### Browser Media And Streaming

- [LiveKit](https://livekit.com/)
- [LiveKit connection docs](https://docs.livekit.io/intro/basics/connect/)
- [livekit/client-sdk-js](https://github.com/livekit/client-sdk-js)
- [VDO.Ninja](https://vdo.ninja/)
- [steveseguin/vdo.ninja](https://github.com/steveseguin/vdo.ninja)
- [WebRTC API](https://developer.mozilla.org/en-US/docs/Web/API/WebRTC_API)
- [`getUserMedia()`](https://developer.mozilla.org/en-US/docs/Web/API/MediaDevices/getUserMedia)
- [MediaRecorder](https://developer.mozilla.org/en-US/docs/Web/API/MediaRecorder)
- [Web Audio API](https://developer.mozilla.org/en-US/docs/Web/API/Web_Audio_API)

### RSVP And Speed-Reading Inspiration

- [cameron/squirt](https://github.com/cameron/squirt)

### Typography And Icons

- [Inter](https://rsms.me/inter/)
- [JetBrains Mono](https://www.jetbrains.com/lp/mono/)
- [Playfair Display](https://fonts.google.com/specimen/Playfair+Display)
- [Google Fonts](https://fonts.google.com/)
- [Feather Icons](https://feathericons.com/)

If we missed someone in the credits, open an issue or PR and add the attribution explicitly instead of leaving the dependency implicit.

## License

This project is licensed under [MIT](LICENSE).
