<h1 align="center">PrompterOne</h1>

<p align="center">
  <strong>Write. Rehearse. Read. Go live. All from the browser.</strong><br/>
  An open-source, browser-first teleprompter studio built around <a href="https://github.com/managedcode/TPS">TPS</a> so the same script can move from first draft to rehearsal, reader mode, recording, and live delivery without a backend.
</p>

<p align="center">
  <a href="https://app.prompter.one/">Try it now</a> &middot;
  <a href="https://github.com/managedcode/PrompterOne">GitHub</a> &middot;
  <a href="https://github.com/managedcode/TPS">TPS</a> &middot;
  <a href="docs/Architecture.md">Architecture</a> &middot;
  <a href="#quick-start">Run locally</a> &middot;
  <a href="#license">License</a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet" alt=".NET 10" />
  <img src="https://img.shields.io/badge/Blazor-WebAssembly-512BD4?logo=blazor" alt="Blazor WASM" />
  <img src="https://img.shields.io/badge/license-MIT-blue" alt="MIT License" />
</p>

<h2 align="center">🚀 Build In Public</h2>

<p align="center">
  <a href="https://www.youtube.com/watch?v=SBcsYblO1AI&list=PLIyi2UvDCig2IgzRQLJXcS5MYdZVDu2EM">
    <img src="https://img.youtube.com/vi/SBcsYblO1AI/maxresdefault.jpg" width="600" alt="Watch on YouTube" />
  </a>
</p>

<p align="center">
  <a href="https://www.youtube.com/@managed-code">
    <img src="https://img.shields.io/badge/YouTube-Subscribe-red?style=for-the-badge&logo=youtube" />
  </a>
</p>



---

## The Problem

If you speak on camera, stream, or present live, the workflow usually fragments immediately. Drafts live in one app. Rehearsal happens somewhere else. The teleprompter is a separate tool. Recording and streaming need another stack entirely. Every switch breaks context, strips timing cues, and turns delivery into a copy-paste problem.

**PrompterOne** keeps that flow in one browser tab. You write once in TPS, rehearse the same script in Learn, read it in the teleprompter, and send the same composed program feed into local recording or transport-aware live output. Pacing, emphasis, structure, metadata, and reader settings stay attached to the script instead of getting rebuilt at every stage.

No PrompterOne backend. No desktop install. No account wall. Open the app, start writing, and keep the runtime in the browser.

## What Works Today

- **Library**: browse scripts, create folders, move documents, search by title, file name, and script content, then jump straight into edit, Learn, or Teleprompter flows from the same card.
- **Editor**: author real TPS with Monaco-native syntax support, metadata hydration, structure-aware navigation, floating formatting controls, in-document find, syntax-aware rendering, browser-local autosave/history, a script knowledge-graph view, and responsive large-draft typing.
- **Learn**: rehearse with ORP-aligned RSVP, context rails, phrase-aware timing, WPM controls, stepping, looping, and punctuation-safe word progression.
- **Teleprompter**: read with persisted font and width controls, focal-line positioning, horizontal and vertical mirror toggles, orientation switching, browser fullscreen, segmented progress, and optional camera background.
- **Onboarding**: walk the first-run flow with a localized tour that explains TPS, RSVP, the editor, Learn, Teleprompter, and Go Live, then reopen that tour later from Settings.
- **Settings**: manage appearance, browser language, media permissions, camera and microphone setup, sync offsets, recording defaults, minimal active AI provider setup, cloud snapshot targets, transport credentials, and onboarding restart from one routed screen.
- **AI Spotlight**: when an AI provider is configured, run the global assistant as a real Microsoft Agent Framework agent with route context, editor text, selected ranges, graph summary, PrompterOne MCP-style tools, and structured output that separates chat replies from exact document edits.
- **Go Live**: run the browser-owned studio shell and save the composed program feed locally with decodable video and audio while remote transport and destination routing continue to expand.

## What You Get

### Script Library

The operating desk for the rest of the app. The library keeps starter scripts and your own documents in browser storage, supports nested folders, lets you search by title, file name, or body text, and branches straight into editing, rehearsal, or reading from the same card. It is built for quick route switching instead of export-import churn.

![Library](docs/screenshots/readme/library.png)

---

### Smart Script Editor

This is not a plain textarea. The editor understands **TPS** (Teleprompter Script), so you can write in segments, blocks, pacing markers, emphasis, emotion tags, pronunciation guides, pause cues, and speed modifiers directly in the source. Front matter is parsed into the metadata rail and kept out of the visible body instead of lingering inline.

The authoring surface includes structure navigation on the left, a full formatting and insert toolbar, floating selection controls, a metadata rail for front matter and speed offsets, in-document find, import and export actions, browser-local autosave with revision history, syntax-aware highlighting over the live source, and a first-class script graph tab. The graph view can run beside or over the source, lets writers inspect the script's high-level knowledge map, and keeps jump-back-to-source context attached to graph nodes. Its **Build AI graph** action uses the configured LLM extractor; tokenizer similarity stays an explicit lower-fidelity fallback instead of silently pretending to be semantic analysis. Recent UI work moved TPS authoring fully onto the Monaco editor surface, tightened dropdown and tooltip behavior, cleaned up gutter spacing, and kept large-draft responsiveness intact on both polished demo scripts and very large seeded drafts.

![Editor](docs/screenshots/readme/editor.png)

---

### TPS Cue Language

PrompterOne treats TPS cues as reading instructions, not markup noise. The editor and reader now carry the same cue intent forward for voice, delivery, pace, emphasis, highlight, pronunciation, phonetics, stress, breath marks, staccato, legato, energy, melody, edit points, speaker/archetype metadata, aside, rhetorical turns, building delivery, sarcasm, loud, soft, whisper, warm, urgent, and related emotion cues.

In the editor, TPS authoring stays readable while Monaco colors cue tags, underlines articulation, and keeps pronunciation and delivery hints visible beside the clean script text.

![TPS cue styling in the editor](docs/screenshots/readme/tps-editor-cues.png)

In the teleprompter, the clean reading line keeps those cues visible through music-inspired reading marks: pace changes affect timing and word shape, slower cues widen the word, faster cues keep bounded non-overlapping tracking, loud cues get larger and weightier, building delivery adds a crescendo-style hairpin, breath and pause cues read like rests, legato uses a music-like slur, staccato uses dots, and energy or melody adds stronger visual rhythm without exposing raw TPS tags. The notation model follows common score-reading conventions for slurs, staccato, and crescendo hairpins from music notation references such as [List of musical symbols](https://en.wikipedia.org/wiki/List_of_musical_symbols) and [Music Theory Academy's dynamics guide](https://www.musictheoryacademy.com/how-to-read-sheet-music/dynamics/).

![TPS cue styling in the teleprompter](docs/screenshots/readme/tps-teleprompter-cues.png)

The screenshot matrix below is generated from `starter-tps-cue-matrix.tps`. Each row uses one plain reader card, one normal sentence, and one isolated cue scope. The central focus word names the cue whenever possible, for example `[sad]sad[/sad]`, plus phrase-span examples where TPS tags intentionally wrap a short cue phrase.

| TPS cue | Reader meaning | Visible reader treatment | Reader screenshot |
| --- | --- | --- | --- |
| Structure baseline | Plain block text with no inline cue | Shows the clean reading sentence and segmented card shape without raw TPS headers | <img src="docs/screenshots/readme/tps-cues/01-structure-baseline.png" alt="Structure baseline reader screenshot" width="360"> |
| `/` | Short pause before the next word | Adds one compact rest marker before the central word while keeping the spoken word clean | <img src="docs/screenshots/readme/tps-cues/02-pause-slash.png" alt="Slash pause reader screenshot" width="360"> |
| `//` | Stronger short pause before the next word | Adds one visible rest marker before the central word without exposing the slash text | <img src="docs/screenshots/readme/tps-cues/03-pause-double-slash.png" alt="Double slash pause reader screenshot" width="360"> |
| `[pause:500ms]` | Explicit half-second rest | Renders a pause cue before the central word and preserves phrase spacing | <img src="docs/screenshots/readme/tps-cues/04-pause-500ms.png" alt="500 millisecond pause reader screenshot" width="360"> |
| `[pause:1s]` | Explicit one-second rest | Renders a pause cue before the central word and keeps the next word readable | <img src="docs/screenshots/readme/tps-cues/05-pause-1s.png" alt="One second pause reader screenshot" width="360"> |
| `[breath]` | Breath cue | Renders a breath rest before the central word without turning the cue into spoken text | <img src="docs/screenshots/readme/tps-cues/06-breath.png" alt="Breath cue reader screenshot" width="360"> |
| `[xslow]` | Extra-slow pace | Broadens the central word with restrained open tracking and slower timing | <img src="docs/screenshots/readme/tps-cues/07-speed-xslow.png" alt="Xslow cue reader screenshot" width="360"> |
| `[slow]` | Slow pace | Opens the central word with subtle tracking while preserving normal readable gaps | <img src="docs/screenshots/readme/tps-cues/08-speed-slow.png" alt="Slow cue reader screenshot" width="360"> |
| `[normal]` | Reset to normal pace | Returns the central word to the base pace treatment | <img src="docs/screenshots/readme/tps-cues/09-speed-normal.png" alt="Normal pace cue reader screenshot" width="360"> |
| `[fast]` | Fast pace | Tightens the central word with compact tracking and faster timing | <img src="docs/screenshots/readme/tps-cues/10-speed-fast.png" alt="Fast cue reader screenshot" width="360"> |
| `[xfast]` | Extra-fast pace | Makes the central word visibly narrower without colliding with neighbors | <img src="docs/screenshots/readme/tps-cues/11-speed-xfast.png" alt="Xfast cue reader screenshot" width="360"> |
| `[180WPM]` | Explicit WPM pace | Maps the central word to a measured fast timing and compact spacing contract | <img src="docs/screenshots/readme/tps-cues/12-speed-180wpm.png" alt="180 WPM cue reader screenshot" width="360"> |
| `[loud]` | Strong vocal intensity | Makes the central word visibly larger and weightier with a warmer projected tone | <img src="docs/screenshots/readme/tps-cues/13-volume-loud.png" alt="Loud cue reader screenshot" width="360"> |
| `[soft]` | Softer delivery | Uses a smaller, lighter, cooler treatment while keeping the central word legible | <img src="docs/screenshots/readme/tps-cues/14-volume-soft.png" alt="Soft cue reader screenshot" width="360"> |
| `[whisper]` | Whispered delivery | Uses a smaller, airy italic treatment with subdued opacity and dotted texture | <img src="docs/screenshots/readme/tps-cues/15-volume-whisper.png" alt="Whisper cue reader screenshot" width="360"> |
| `[warm]` | Warm emotional tone | Tints the central word with an amber/orange warm treatment | <img src="docs/screenshots/readme/tps-cues/16-emotion-warm.png" alt="Warm emotion cue reader screenshot" width="360"> |
| `[urgent]` | Urgent emotional tone | Gives the central word a red/crimson urgent treatment | <img src="docs/screenshots/readme/tps-cues/17-emotion-urgent.png" alt="Urgent emotion cue reader screenshot" width="360"> |
| `[excited]` | Excited emotional tone | Gives the central word a magenta/violet excited treatment | <img src="docs/screenshots/readme/tps-cues/18-emotion-excited.png" alt="Excited emotion cue reader screenshot" width="360"> |
| `[happy]` | Happy emotional tone | Tints the central word for upbeat delivery | <img src="docs/screenshots/readme/tps-cues/19-emotion-happy.png" alt="Happy emotion cue reader screenshot" width="360"> |
| `[sad]` | Sad emotional tone | Tints the central word with a lower-intensity mood | <img src="docs/screenshots/readme/tps-cues/20-emotion-sad.png" alt="Sad emotion cue reader screenshot" width="360"> |
| `[calm]` | Calm emotional tone | Keeps the central word muted and steady | <img src="docs/screenshots/readme/tps-cues/21-emotion-calm.png" alt="Calm emotion cue reader screenshot" width="360"> |
| `[energetic]` | Energetic emotional tone | Gives the central word a stronger active treatment | <img src="docs/screenshots/readme/tps-cues/22-emotion-energetic.png" alt="Energetic emotion cue reader screenshot" width="360"> |
| `[professional]` | Professional emotional tone | Keeps the central word controlled and formal | <img src="docs/screenshots/readme/tps-cues/23-emotion-professional.png" alt="Professional emotion cue reader screenshot" width="360"> |
| `[focused]` | Focused emotional tone | Highlights the central word as concentrated delivery | <img src="docs/screenshots/readme/tps-cues/24-emotion-focused.png" alt="Focused emotion cue reader screenshot" width="360"> |
| `[concerned]` | Concerned emotional tone | Uses a concerned tint on the central word | <img src="docs/screenshots/readme/tps-cues/25-emotion-concerned.png" alt="Concerned emotion cue reader screenshot" width="360"> |
| `[motivational]` | Motivational emotional tone | Gives the central word an encouraging emphasis | <img src="docs/screenshots/readme/tps-cues/26-emotion-motivational.png" alt="Motivational emotion cue reader screenshot" width="360"> |
| `[neutral]` | Neutral tone reset | Gives the central word a readable silver neutral treatment without decorative emotion color | <img src="docs/screenshots/readme/tps-cues/27-emotion-neutral.png" alt="Neutral emotion cue reader screenshot" width="360"> |
| `[aside]` | Aside delivery shape | Gives the central word a delivery-specific aside cue | <img src="docs/screenshots/readme/tps-cues/28-delivery-aside.png" alt="Aside delivery cue reader screenshot" width="360"> |
| `[rhetorical]` | Rhetorical delivery shape | Marks the central word with a rhetorical treatment | <img src="docs/screenshots/readme/tps-cues/29-delivery-rhetorical.png" alt="Rhetorical delivery cue reader screenshot" width="360"> |
| `[building]` | Building delivery shape | Adds a crescendo-style hairpin and progressive weight so delivery can rise through the phrase | <img src="docs/screenshots/readme/tps-cues/30-delivery-building.png" alt="Building delivery cue reader screenshot" width="360"> |
| `[sarcasm]` | Sarcastic delivery shape | Marks the central word with a sarcastic delivery cue | <img src="docs/screenshots/readme/tps-cues/31-delivery-sarcasm.png" alt="Sarcasm delivery cue reader screenshot" width="360"> |
| `[legato]` | Smooth articulation | Adds a music-like curved slur below the central word | <img src="docs/screenshots/readme/tps-cues/32-articulation-legato.png" alt="Legato articulation cue reader screenshot" width="360"> |
| `[staccato]` | Clipped articulation | Adds clipped music-like dots below the central word | <img src="docs/screenshots/readme/tps-cues/33-articulation-staccato.png" alt="Staccato articulation cue reader screenshot" width="360"> |
| `[energy:8]` | High intensity contour | Adds stronger energy weight and rhythm to the central word | <img src="docs/screenshots/readme/tps-cues/34-contour-energy.png" alt="Energy contour cue reader screenshot" width="360"> |
| `[melody:3]` | Melodic movement | Adds a subtle melodic contour to the central word | <img src="docs/screenshots/readme/tps-cues/35-contour-melody.png" alt="Melody contour cue reader screenshot" width="360"> |
| `[highlight]` | Editorial highlight | Uses a shaped translucent marker background behind the cue word without broken half-height fills | <img src="docs/screenshots/readme/tps-cues/36-editorial-highlight.png" alt="Highlight cue reader screenshot" width="360"> |
| `[emphasis]` | Editorial emphasis | Uses a stronger word shape and weight treatment without connector underlines between words | <img src="docs/screenshots/readme/tps-cues/37-editorial-emphasis.png" alt="Emphasis cue reader screenshot" width="360"> |
| Markdown bold | Strong editorial emphasis | Shows a heavier bold word shape without raw markdown markers | <img src="docs/screenshots/readme/tps-cues/38-markdown-bold.png" alt="Markdown bold reader screenshot" width="360"> |
| Markdown italic | Light editorial emphasis | Shows a visibly slanted italic word shape without raw markdown markers | <img src="docs/screenshots/readme/tps-cues/39-markdown-italic.png" alt="Markdown italic reader screenshot" width="360"> |
| `[pronunciation:/prəˌnʌnsiˈeɪʃən/]` | Pronunciation note | Uses the guide itself as the primary reader text and keeps the original spelling as compact secondary context | <img src="docs/screenshots/readme/tps-cues/40-guide-pronunciation.png" alt="Pronunciation guide reader screenshot" width="360"> |
| `[phonetic:/fəˈnɛtɪk/]` | Phonetic note | Uses the phonetic guide itself as the primary reader text and keeps the original spelling as compact secondary context | <img src="docs/screenshots/readme/tps-cues/41-guide-phonetic.png" alt="Phonetic guide reader screenshot" width="360"> |
| `[stress:rising]` | Stress guide | Adds a stress mark and dedicated double stress stroke so rehearsal stress is separate from emphasis and bold | <img src="docs/screenshots/readme/tps-cues/42-guide-stress.png" alt="Stress guide reader screenshot" width="360"> |
| `[edit_point]` | Production cut point | Stays out of the spoken word while leaving a compact standard edit marker before the next word | <img src="docs/screenshots/readme/tps-cues/43-edit-point.png" alt="Edit point reader screenshot" width="360"> |
| `[edit_point:medium]` | Medium production cut point | Stays out of the spoken line and adds a stronger medium-priority edit marker | <img src="docs/screenshots/readme/tps-cues/44-edit-point-medium.png" alt="Medium edit point reader screenshot" width="360"> |
| `[edit_point:high]` | High production cut point | Stays out of the spoken line and adds the strongest high-priority edit marker | <img src="docs/screenshots/readme/tps-cues/45-edit-point-high.png" alt="High edit point reader screenshot" width="360"> |
| `Speaker:Narrator` | Speaker metadata | Keeps persona metadata in validation, graph, and optional chrome without raw reader text | <img src="docs/screenshots/readme/tps-cues/46-metadata-speaker.png" alt="Speaker metadata reader screenshot" width="360"> |
| `Archetype:Coach` | Archetype metadata | Keeps delivery persona metadata for validation, graph, and assistant context without decorative reader styling | <img src="docs/screenshots/readme/tps-cues/47-metadata-archetype.png" alt="Archetype metadata reader screenshot" width="360"> |
| `[slow]slow cadence[/slow]` | Phrase pace span | Applies the same slow timing and open tracking across a two-word cue phrase | <img src="docs/screenshots/readme/tps-cues/48-phrase-speed-slow.png" alt="Slow phrase reader screenshot" width="360"> |
| `[urgent]urgent cadence[/urgent]` | Phrase emotion span | Applies the urgent color treatment across a two-word cue phrase | <img src="docs/screenshots/readme/tps-cues/49-phrase-emotion-urgent.png" alt="Urgent phrase reader screenshot" width="360"> |
| `[loud]loud cadence[/loud]` | Phrase volume span | Applies stronger vocal weight and tone across a two-word cue phrase | <img src="docs/screenshots/readme/tps-cues/50-phrase-volume-loud.png" alt="Loud phrase reader screenshot" width="360"> |
| `[building]building cadence[/building]` | Phrase delivery span | Applies one crescendo-style hairpin and progressive build treatment across the two-word cue phrase | <img src="docs/screenshots/readme/tps-cues/51-phrase-delivery-building.png" alt="Building phrase reader screenshot" width="360"> |
| `[legato]legato cadence[/legato]` | Phrase articulation span | Applies one smooth legato slur across the two-word cue phrase | <img src="docs/screenshots/readme/tps-cues/52-phrase-articulation-legato.png" alt="Legato phrase reader screenshot" width="360"> |
| `[energy:1]` | Low intensity contour | Shows the restrained floor of the energy contour so quiet energy still reads differently from neutral text | <img src="docs/screenshots/readme/tps-cues/53-contour-energy-low.png" alt="Low energy contour reader screenshot" width="360"> |
| `[energy:5]` | Mid intensity contour | Shows the middle energy contour weight used for steady projected delivery | <img src="docs/screenshots/readme/tps-cues/54-contour-energy-mid.png" alt="Mid energy contour reader screenshot" width="360"> |
| `[energy:10]` | Peak intensity contour | Shows the strongest energy contour weight and rhythm without letting the word spill into its neighbors | <img src="docs/screenshots/readme/tps-cues/55-contour-energy-peak.png" alt="Peak energy contour reader screenshot" width="360"> |
| `[melody:1]` | Low melodic contour | Shows the lightest melodic contour so melodic marks stay visible even at the soft end of the range | <img src="docs/screenshots/readme/tps-cues/56-contour-melody-low.png" alt="Low melody contour reader screenshot" width="360"> |
| `[melody:5]` | Mid melodic contour | Shows the center melodic contour used for readable pitch movement | <img src="docs/screenshots/readme/tps-cues/57-contour-melody-mid.png" alt="Mid melody contour reader screenshot" width="360"> |
| `[melody:10]` | Peak melodic contour | Shows the strongest melodic contour while keeping the phrase readable at README scale | <img src="docs/screenshots/readme/tps-cues/58-contour-melody-peak.png" alt="Peak melody contour reader screenshot" width="360"> |

The full implemented rendering contract, animation constraints, and verification evidence are recorded in [TPS Cue Rendering Specification](TPS_CUE_RENDERING_PLAN.md).

---

### First-Run Onboarding

The app now opens with a guided first-run tour instead of expecting you to infer the workflow from the chrome. It explains what PrompterOne is for, why TPS exists, what RSVP rehearsal does, how the editor differs from Learn and Teleprompter, and what Go Live is responsible for in the browser-owned studio flow.

The tour is localized, can be dismissed if you already know the app, and can be reopened later from Settings when you need the full product map again.

---

### RSVP Rehearsal (Learn)

**Learn** is the rehearsal surface. It presents one word at a time using RSVP (Rapid Serial Visual Presentation) with an ORP-style focal point so the eye lands in a predictable place even as word lengths change.

Context rails show nearby words without clipping into the focal lane, phrase-aware pauses come from the TPS source, and speed is adjustable while you rehearse. Step controls, loop mode, stop-at-end behavior, and sentence-local context make it useful for both memorization and pacing work before you step into the reader.

![Learn](docs/screenshots/readme/learn.png)

---

### Teleprompter

The delivery surface. Large readable text, phrase-aware emphasis, adjustable font size, adjustable reader width, and a focal guide you can reposition without leaving the route. TPS formatting carries through here too: speed modifiers affect timing while preserving readable word gaps, inline emphasis stays intact, punctuation is attached correctly, and emphasis groups stay continuous instead of breaking word by word.

A live camera feed can run behind the reader as a background layer, and the operator controls stay on-screen: horizontal mirror, vertical mirror, reader orientation toggle, browser fullscreen, font controls, width controls, focal positioning, segmented block progress, and transport-style playback controls all live inside the same reader shell. Smooth block-to-block transitions keep the reading flow readable in both forward and backward navigation.

Reader preferences persist between sessions, so your chosen layout, focal position, mirrors, and camera background do not have to be rebuilt every time.

![Teleprompter](docs/screenshots/readme/teleprompter.png)

---

### Go Live

A browser-owned studio session. PrompterOne captures a composed program feed directly in the browser, so canvas composition, audio monitoring, and source switching stay client-side.

**Local recording** saves the composed feed to a file on your machine and is the strongest part of the current live stack. **Remote publishing** is transport-aware through [LiveKit](https://livekit.io/) and [VDO.Ninja](https://vdo.ninja/): the browser keeps one real upstream transport path active for a session, and downstream targets are routed or blocked according to what that transport can actually service.

Distribution targets such as YouTube, Twitch, and custom RTMP are capability-gated: unsupported paths are blocked instead of being faked. PrompterOne does not hide a private relay tier behind the UI. The browser remains the only app runtime.

| Settings | Go Live |
| :---: | :---: |
| ![Settings](docs/screenshots/readme/settings.png) | ![Go Live](docs/screenshots/readme/go-live.png) |

---

### Settings

Settings holds the operational state for the rest of the app: appearance, browser language, cloud snapshot targets, camera selection with preview, microphone setup with live meters, delay and sync offsets, output quality profiles, recording defaults, minimal AI provider preferences, transport credentials, and onboarding restart. AI provider setup stays intentionally small: choose one active provider, enter only the endpoint or base URL when needed, the API key when needed, and the model or deployment names you actually use. Theme changes and layout preferences persist, and appearance changes propagate across tabs instead of drifting out of sync.

---

### Localization

PrompterOne negotiates the initial language from your browser and remembers your explicit choice after that. The routed chrome, onboarding, diagnostics, settings, library actions, editor command surfaces, cloud sync status, AI Spotlight shell, and core reader controls all use the shared localization catalog across English, German, Spanish, French, Italian, Portuguese, and Ukrainian. Technical protocol names, API-key examples, and product brand text stay intentionally unlocalized.

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

PrompterOne is in **active alpha**: the core authoring, rehearsal, reader, and local-recording flow is solid now; remote publishing and portability layers are still expanding.

| Area | Status | Current reality |
| --- | :---: | --- |
| **Library** | ✅ | Script browsing, folder organization, create and move flows, workflow launchers, persisted browser storage |
| **Editor** | ✅ | Monaco-native TPS authoring, front-matter hydration, metadata rail, floating formatting controls, in-document find, script graph tab, local autosave/history, syntax-aware cue rendering, responsive large-draft typing |
| **Learn** | ✅ | ORP-aligned RSVP, phrase-aware timing, context rails, WPM controls, stepping, looping, punctuation-safe progression |
| **Teleprompter** | ✅ | Reader width and font controls, focal positioning, TPS cue contour rendering, horizontal and vertical mirror toggles, orientation toggle, browser fullscreen, segmented progress, persisted layout |
| **Onboarding** | ✅ | Localized first-run walkthrough plus Settings-driven tour restart |
| **Settings** | ✅ | Appearance sync, browser language, media permissions, camera and mic setup, delay offsets, recording defaults, AI provider preferences, cloud snapshot forms, transport configuration |
| **Local recording** | ✅ | Browser-side recording of the composed program feed with decodable video and audio |
| **Localization** | ✅ | Browser-negotiated language, persisted manual override, shared resource parity across supported languages, localized onboarding, diagnostics, settings, library actions, editor command surfaces, AI Spotlight shell, and reader controls |
| **Go Live studio shell** | ✅ | Source rails, scene switching, preview/program layout, runtime telemetry, session chrome, browser-owned operator workflow |
| **VDO.Ninja transport** | 🟡 | Real transport-aware browser integration, with operational polish still expanding |
| **LiveKit transport** | 🟡 | Real transport-aware browser integration and guest-path work, with operational polish still expanding |
| **Distribution routing** | 🟡 | Targets are capability-gated and blocked when the selected transport cannot service them |
| **Cloud storage snapshots** | 🟡 | Browser-local provider configuration ships now; broader import/export maturity is still expanding |
| **AI provider execution** | ✅ | AI Spotlight runs a configured Microsoft Agent Framework agent with route/editor context, MCP-style tools, structured chat-plus-edit output, and exact range edit application; script graph analysis uses the configured LLM extractor before any explicit tokenizer fallback |
| **Generic RTMP fan-out** | ❌ | Intentionally unsupported without a real upstream transport path |
| **PrompterOne backend** | ❌ | By design: the browser is the only app runtime |

## Roadmap

These are product directions, not release-date promises.

**Near term:**
- Remote publish polish for VDO.Ninja and LiveKit transport flows
- Broader cloud portability for scripts, settings, and snapshots
- More public documentation and real-world workflow examples

**After that:**
- Stronger guest and destination-routing workflows on top of the browser studio
- More agent-callable editor, media, and streaming operations on top of the existing AI runtime
- Deeper operational telemetry and operator ergonomics in Go Live

## Quick Start

**Requirements:** [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

```bash
git clone https://github.com/managedcode/PrompterOne.git
cd PrompterOne
dotnet run --project src/PrompterOne.Web
```

Or just open [app.prompter.one](https://app.prompter.one/) — no install needed.

## Technology

PrompterOne is a standalone [Blazor WebAssembly](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor) app on [.NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0). The browser is the runtime: media capture, composition, recording, and transport-aware output happen through browser APIs such as MediaDevices, WebRTC, MediaRecorder, Web Audio, and Canvas. Transport integrations use [LiveKit](https://livekit.io/) and [VDO.Ninja](https://vdo.ninja/). Verification uses [TUnit](https://tunit.dev/), [bUnit](https://bunit.dev/), and [Playwright](https://playwright.dev/), with browser scenarios acting as the main release bar. Deployment is a static GitHub Pages build.

## For Contributors

```bash
dotnet build ./PrompterOne.slnx -warnaserror
dotnet test @./tests/dotnet-test-progress.rsp --solution ./PrompterOne.slnx
dotnet format ./PrompterOne.slnx
dotnet test @./tests/dotnet-test-progress.rsp --project ./tests/PrompterOne.Core.Tests/PrompterOne.Core.Tests.csproj --coverage --coverage-output-format cobertura
dotnet test @./tests/dotnet-test-progress.rsp --project ./tests/PrompterOne.Web.Tests/PrompterOne.Web.Tests.csproj --coverage --coverage-output-format cobertura
dotnet test @./tests/dotnet-test-progress.rsp --project ./tests/PrompterOne.Web.UITests.Shell/PrompterOne.Web.UITests.Shell.csproj --coverage --coverage-output-format cobertura
dotnet test @./tests/dotnet-test-progress.rsp --project ./tests/PrompterOne.Web.UITests.Studio/PrompterOne.Web.UITests.Studio.csproj --coverage --coverage-output-format cobertura
dotnet test @./tests/dotnet-test-progress.rsp --project ./tests/PrompterOne.Web.UITests.Editor/PrompterOne.Web.UITests.Editor.csproj --coverage --coverage-output-format cobertura
dotnet test @./tests/dotnet-test-progress.rsp --project ./tests/PrompterOne.Web.UITests.Reader/PrompterOne.Web.UITests.Reader.csproj --coverage --coverage-output-format cobertura
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
