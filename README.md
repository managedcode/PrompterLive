<h1 align="center">PrompterOne</h1>

<p align="center">
  <strong>Write. Rehearse. Read. Go live. All from the browser.</strong><br/>
  An open-source teleprompter studio that takes your script from first draft to live delivery — no backend, no installs, no accounts.
</p>

<p align="center">
  <a href="https://prompter.managed-code.com/">Try it now</a> &middot;
  <a href="docs/Architecture.md">Architecture</a> &middot;
  <a href="#quick-start">Run locally</a> &middot;
  <a href="#license">License</a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet" alt=".NET 10" />
  <img src="https://img.shields.io/badge/Blazor-WebAssembly-512BD4?logo=blazor" alt="Blazor WASM" />
  <img src="https://img.shields.io/badge/license-MIT-blue" alt="MIT License" />
</p>

---

## The Problem

If you speak on camera, stream, or present live — you already know how fragmented the workflow is. Scripts live in Google Docs. Rehearsal is reading out loud. The teleprompter is a separate app. Recording and streaming need yet another tool. Every switch means context loss, copy-paste, and broken flow.

**PrompterOne** puts the entire journey in one place. You write a script, rehearse it at speed, read it on camera, and go live — all without leaving the browser tab. Your script carries its pacing, emphasis, and structure through every stage, so what you author is exactly what you deliver.

No server. No desktop install. No sign-up. Open the app, start writing, and everything stays in your browser.

## What You Get

### Script Library

The starting point. Browse your scripts, organize them into folders, and jump straight into editing, rehearsal, or reading from the same card. Everything persists in browser storage — your scripts are yours and stay local.

![Library](docs/screenshots/readme/library.png)

---

### Smart Script Editor

Not just a text box. The editor understands **TPS** (Teleprompter Script) — a format designed for delivery. You write with structure: segments, blocks, pacing markers, emphasis, emotion tags, pronunciation guides, and speed modifiers. All of that metadata travels with the script into rehearsal and live reading.

The authoring surface includes a toolbar with formatting, structure navigation in the sidebar, a metadata rail for front matter and speed offsets, and syntax-aware highlighting — all inline, without switching between edit and preview modes.

![Editor](docs/screenshots/readme/editor.png)

---

### RSVP Rehearsal (Learn)

**Learn** is rapid-fire rehearsal. It presents one word at a time using RSVP (Rapid Serial Visual Presentation) with an ORP-style focal point — the letter your eye naturally lands on is always centered.

Context rails show the words before and after the current one, so you never feel lost. Speed is adjustable, phrase-aware pauses are built in from your TPS script, and you can loop or stop at the end. It is the fastest way to internalize delivery before stepping in front of the camera.

![Learn](docs/screenshots/readme/learn.png)

---

### Teleprompter

The delivery surface. Large readable text, phrase-aware emphasis, adjustable font size, text width, and a focal guide you can position where you need it. TPS formatting carries through — speed modifiers affect letter spacing, emotion tags tint accents, and inline emphasis stays visible as you read.

A live camera feed runs behind the text as a background layer, so you can see yourself while reading. Horizontal and vertical mirror toggles handle reflected-glass and tablet setups without leaving the screen. Smooth block-to-block transitions keep the reading flow natural.

All reader preferences — font scale, width, focal position, camera setting — persist between sessions.

![Teleprompter](docs/screenshots/readme/teleprompter.png)

---

### Go Live

A browser-owned studio session. PrompterOne captures a composed program feed directly in the browser — canvas composition, audio mixing, and source switching happen client-side.

**Local recording** saves the composed feed to a file on your machine. **Live publishing** connects through [LiveKit](https://livekit.io/) and [VDO.Ninja](https://vdo.ninja/) — real transport protocols, not fake broadcast buttons. Both transports can run concurrently. Source switching, scene controls, and runtime telemetry live in the operator surface.

Distribution targets like YouTube, Twitch, and custom RTMP are capability-gated: if the active transport path does not support a target, the UI blocks it instead of pretending it works. No hidden media server, no relay tier — the browser is the only runtime.

| Settings | Go Live |
| :---: | :---: |
| ![Settings](docs/screenshots/readme/settings.png) | ![Go Live](docs/screenshots/readme/go-live.png) |

---

### Settings

Device configuration lives here: camera selection with live preview, microphone setup with real-time level meters, mirror toggles, delay and sync offsets, output quality profiles, recording defaults, and transport connection credentials. Everything is explicit and persisted — so when you open Go Live, the session is already shaped.

---

### Localization

PrompterOne negotiates the initial language from your browser and remembers your choice. Currently supported: **English**, **Ukrainian**, **French**, **Spanish**, **Portuguese**, **Italian**, and **German**.

## The Full Flow

```mermaid
graph LR
    A["Library<br/><sub>browse & organize</sub>"] --> B["Editor<br/><sub>write with TPS markup</sub>"]
    B --> C["Learn<br/><sub>rehearse at speed</sub>"]
    C --> D["Teleprompter<br/><sub>read on camera</sub>"]
    D --> E["Go Live<br/><sub>record & stream</sub>"]
```

Every stage works with the same script. Pacing, emphasis, structure, and metadata you add in the editor show up in rehearsal, in the teleprompter, and in the live session. No re-importing, no copy-paste, no format conversion.

## Product Status

PrompterOne is in **active alpha** — usable today for real work, with some areas still expanding.

| Feature | Status | Details |
| --- | :---: | --- |
| **Library** | ✅ | Script browsing, folder organization, script creation, workflow launch |
| **Editor** | ✅ | TPS authoring with structure, formatting, pacing, emphasis, metadata, speed offsets, AI-assisted rewriting, inline syntax highlighting |
| **Learn** | ✅ | RSVP rehearsal with ORP focal point, adjustable speed, phrase-aware timing, context rails, loop toggle |
| **Teleprompter** | ✅ | Focal controls, camera background, mirror toggles, TPS formatting parity, block transitions, persisted preferences |
| **Settings** | ✅ | Camera and microphone setup with live feedback, device sync, output profiles, recording defaults, transport config, distribution targets |
| **Local recording** | ✅ | Browser-side recording of the composed program feed |
| **Localization** | ✅ | 7 languages with browser negotiation and persisted user override |
| **Go Live core** | 🟡 | Browser program feed, source rails, scene controls, runtime telemetry, session bar |
| **VDO.Ninja** | 🟡 | Transport-aware connection and publish from the browser |
| **LiveKit** | 🟡 | Transport-aware connection and publish from the browser |
| **Distribution targets** | 🟡 | Capability-gated routing — unsupported paths are blocked, not faked |
| **Cloud storage** | 🟡 | Provider-backed snapshot import/export for scripts and settings |
| **Generic RTMP fan-out** | ❌ | Intentionally not supported — downstream targets require a real transport path |
| **PrompterOne backend** | ❌ | By design — the browser is the only runtime, no server tier |

## Roadmap

These are product directions, not release-date promises.

**Near term:**
- Go Live operational polish — source switching, destination health indicators, session telemetry depth
- Smoother transport setup flows for VDO.Ninja and LiveKit
- Better onboarding for first-time users through the write-rehearse-read-live flow
- Broader documentation and real-world usage examples

**After that:**
- Export, sync, and portability workflows for scripts and settings
- Remote guest workflows and destination routing
- AI-assisted writing and transformation once provider integration is mature enough to be honest by default

## Quick Start

**Requirements:** [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

```bash
git clone https://github.com/managedcode/PrompterOne.git
cd PrompterOne
dotnet run --project src/PrompterOne.App
```

Or just open [prompter.managed-code.com](https://prompter.managed-code.com/) — no install needed.

## Technology

PrompterOne is a standalone [Blazor WebAssembly](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor) app on [.NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0). The browser is the entire runtime — media capture, composition, recording, and streaming all happen through browser APIs (MediaDevices, WebRTC, MediaRecorder, Web Audio, Canvas). Transport integrations use [LiveKit](https://livekit.io/) and [VDO.Ninja](https://vdo.ninja/). Testing uses [xUnit](https://xunit.net/), [bUnit](https://bunit.dev/), and [Playwright](https://playwright.dev/). Deployment is a static GitHub Pages build.

## For Contributors

```bash
dotnet build -warnaserror          # build with warnings as errors
dotnet test                        # run all tests
dotnet test --collect:"XPlat Code Coverage"  # with coverage
```

Architecture and ownership boundaries are documented in [docs/Architecture.md](docs/Architecture.md). Each project has a local `AGENTS.md` that describes purpose, entry points, and rules for that area. Feature docs live in [docs/Features/](docs/Features/).

## Credits

- [LiveKit](https://livekit.io/) and [VDO.Ninja](https://vdo.ninja/) for transport infrastructure
- [cameron/squirt](https://github.com/cameron/squirt) for RSVP inspiration
- [Inter](https://rsms.me/inter/), [JetBrains Mono](https://www.jetbrains.com/lp/mono/), [Playfair Display](https://fonts.google.com/specimen/Playfair+Display)
- [Feather Icons](https://feathericons.com/)
- Deployed on [GitHub Pages](https://docs.github.com/en/pages)

## License

[MIT](LICENSE)
