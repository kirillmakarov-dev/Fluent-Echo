# Fluent Echo

Fluent Echo is a Unity-based offline English speech-practice prototype.
It combines local Whisper transcription, scene-owned UI, category-based lesson content, and a feedback flow designed to stay explicit about the current system's capabilities and limits.

The project is structured as a production-style application slice that can continue evolving without a rewrite.
The current implementation focuses on:

- local Whisper transcription;
- transparent practice scoring instead of overstated "AI pronunciation" claims;
- scene-driven UI that remains editable by hand in Unity;
- ScriptableObject-based lesson content;
- a clear separation between composition root, presentation, domain, services, and content.

## Project Goal

The product promise is simple:

- audio stays on the device;
- speech recognition runs locally;
- the app supports repeatable speaking practice without a mandatory cloud dependency;
- the UI does not present the current heuristic scorer as full phoneme-level assessment.

These constraints shape the current engineering direction:

- predictable Unity application architecture;
- local AI integration without a required online service;
- privacy-aware UX for speech interaction;
- maintainable lesson and content pipelines.

## Current User Flow

The current vertical slice follows this loop:

`launch -> onboarding -> choose category -> choose lesson -> record -> transcript -> practice estimate -> feedback -> retry or continue`

Three practice categories are currently available:

- `Words`
- `Short Sentences`
- `Challenge Sentences`

Each category contains multiple lessons configured as ScriptableObjects.

## What Already Works

- category-first lesson selection;
- local Whisper transcription;
- microphone device selection from the scene;
- Whisper profile selection from the scene;
- saved lesson progress;
- a result panel with retry, close, and next-mission actions;
- deterministic demo mode for UI verification;
- scene-owned UI wiring instead of hidden runtime recreation;
- edit-mode coverage for matcher, presenter, settings, and scoring flows.

## Architecture At A Glance

The project is intentionally organized around small layers with clear responsibilities.

### Scene-Owned UI

The scene owns:

- panel layout;
- anchors and positions;
- buttons and labels;
- settings panel placement;
- feedback panel placement;
- visual hierarchy.

The runtime updates state and text, but it should not silently rebuild the layout or override manual scene composition.

### Composition Root

`FluentEchoBootstrap` wires scene references, services, and saved selections.

### Presentation

`FluentEchoPresenter` owns flow and state transitions.

`FluentEchoView` renders state and raises UI events.

### Domain

Core logic lives in types such as:

- `SpeechAnswerMatcher`
- `SpeechSession`
- `SpeechMatchResult`
- `LessonProgressState`

### Services

Speech and persistence live in:

- `WhisperSpeechRecognitionService`
- `MockSpeechRecognitionService`
- `HeuristicPronunciationScoringService`
- `LessonProgressRepository`

### Content

Lessons and settings live in:

- `SpeechExerciseSO`
- `SpeechExerciseCatalogSO`
- `WhisperSettingsSO`

For a fuller engineering map, read:

- [`Fluent-Echo/Assets/_FluentEcho/Docs/DEPENDENCY_MAP.md`](Fluent-Echo/Assets/_FluentEcho/Docs/DEPENDENCY_MAP.md)
- [`Fluent-Echo/Assets/_FluentEcho/Docs/MB_ARCHITECTURE.md`](Fluent-Echo/Assets/_FluentEcho/Docs/MB_ARCHITECTURE.md)

## Whisper Models And Recommended Profiles

The project currently supports three local Whisper profiles:

- `Fast` -> `ggml-tiny.en.bin`
- `Balanced` -> `ggml-base.en.bin`
- `Accurate` -> `ggml-small.en.bin`

Configured in:

- [`Fluent-Echo/Assets/_FluentEcho/Runtime/Services/WhisperSettingsSO.cs`](Fluent-Echo/Assets/_FluentEcho/Runtime/Services/WhisperSettingsSO.cs)

Expected model location:

- `Assets/StreamingAssets/Whisper/ggml-tiny.en.bin`
- `Assets/StreamingAssets/Whisper/ggml-base.en.bin`
- `Assets/StreamingAssets/Whisper/ggml-small.en.bin`

### Recommended Usage Right Now

Based on the current recognition pipeline, these are the recommended profiles:

| Practice type | Recommended profile | Why |
| --- | --- | --- |
| `Words` | `Fast` | The current short-word bias works best here and tends to overthink less on single-word exercises |
| `Short Sentences` | `Balanced` | Better stability than `Fast` while still staying responsive |
| `Challenge Sentences` | `Balanced` first, `Accurate` if needed | Longer utterances benefit more from the larger model |

### Important Note About Accuracy

Bigger is not always better for this prototype.

For short single-word exercises such as `Apple`, `Water`, or `Window`, `Fast` may currently outperform `Balanced` or `Accurate` because:

- the current recognition bias is tuned for short utterances;
- tiny English models can behave better on narrow expected vocabulary;
- larger models sometimes normalize short speech into the wrong but plausible English output.

In other words:

- `Fast` is currently the safest default for word drills;
- `Balanced` is the safer default for sentence drills;
- `Accurate` should be treated as an optional heavier pass, not an automatic upgrade for every lesson type.

## How To Set Up The Project

1. Open the Unity project in the nested `Fluent-Echo` folder.
2. Make sure the Whisper model files are present in `Assets/StreamingAssets/Whisper/`.
3. Open the scene:
   - [`Fluent-Echo/Assets/_FluentEcho/Demo/Scenes/FluentEchoPrototype.unity`](Fluent-Echo/Assets/_FluentEcho/Demo/Scenes/FluentEchoPrototype.unity)
4. Press Play.

If needed, rebuild the demo scene from:

- `Tools > Fluent Echo > Rebuild Prototype`

## How To Verify The Prototype

1. Launch the `FluentEchoPrototype` scene.
2. Pick a category.
3. Pick a lesson.
4. Use `SHOW DEMO` to verify the happy path without a microphone.
5. Use `START SPEAKING` or `CHECK ANSWER` for live practice.
6. Confirm that transcript, word chips, result panel, and progress update correctly.

If Windows asks for microphone permission, grant it in system privacy settings first.

The first Whisper warm-up may take a few seconds while the selected model is loading.

## Reference Audio Placement

If you already have generated audio for lesson playback, place it here:

- [`Fluent-Echo/Assets/_FluentEcho/Demo/Audio/References/`](Fluent-Echo/Assets/_FluentEcho/Demo/Audio/References/)

Then assign the clip in the corresponding `SpeechExerciseSO` asset through the `Reference Audio` field.

Example lesson assets:

- [`Fluent-Echo/Assets/_FluentEcho/Demo/Data/Words_01_Apple.asset`](Fluent-Echo/Assets/_FluentEcho/Demo/Data/Words_01_Apple.asset)
- [`Fluent-Echo/Assets/_FluentEcho/Demo/Data/Short_04_Apple.asset`](Fluent-Echo/Assets/_FluentEcho/Demo/Data/Short_04_Apple.asset)

## How To Add New Lessons

### Add A New Exercise

1. Create or duplicate a `SpeechExerciseSO`.
2. Set:
   - `Prompt`
   - `Target Words`
   - `Accepted Phrases`
   - `Progress Key`
   - optional `Reference Audio`
3. Add the exercise to the correct category inside the catalog.

### Update The Catalog

Use:

- [`Fluent-Echo/Assets/_FluentEcho/Runtime/Data/SpeechExerciseCatalogSO.cs`](Fluent-Echo/Assets/_FluentEcho/Runtime/Data/SpeechExerciseCatalogSO.cs)

The content flow should stay:

`new lesson asset -> catalog assignment -> scene UI uses existing presenter flow`

That keeps content extensible without rewriting runtime code.

## How To Adjust The UI

The current direction is scene-driven UI.

That means layout should be adjusted manually in Unity for:

- panel positions;
- text placement;
- anchors;
- button placement;
- settings panel composition;
- feedback panel composition.

The runtime should not pull those objects back into a hidden default layout.

## Honest Limitation

The current scoring flow is still a heuristic practice estimate.

It is useful and product-friendly, but it is not yet:

- true phoneme scoring;
- stress scoring;
- accent scoring;
- production-grade speech evaluation.

That future work is already separated at the architecture level so the app can evolve without a rewrite.

## Future Enhancements

The most useful next improvements fall into a few clear buckets.

### 1. Better Pronunciation Scoring

The strongest upgrade would be a real pronunciation-assessment pipeline instead of the current heuristic estimate.

Good directions:

- local forced alignment and phoneme-aware scoring;
- a hybrid architecture where Whisper remains local for transcript generation and a dedicated scorer handles pronunciation quality;
- an optional cloud pronunciation service for higher-fidelity scoring in a future production version.

### 2. Better Recognition For Real Learners

The prototype is now much better on short drills, but the next practical upgrades would be:

- per-category profile recommendations in the UI;
- phrase-set biasing for difficult lesson vocabulary;
- noisy-room and weak-microphone testing;
- accent-coverage validation across more speakers.

### 3. Better Reference Audio

The current reference-audio path is asset-driven, which is good for control and repeatability.
Possible upgrades:

- batch-generated lesson audio for every exercise;
- optional dynamic TTS generation for new content;
- character lip-sync tied to lesson reference audio.

### 4. Better Productization

For a more complete application version, useful additions would be:

- onboarding that checks microphone and model readiness before first practice;
- downloadable model management from inside the app;
- lesson analytics that do not store raw user audio;
- exportable practice summaries;
- teacher or reviewer mode for curated lesson sets.

## Optional External API Integrations

The project does not need cloud APIs to stay useful.
However, a production-oriented version could benefit from carefully chosen external integrations.

### Recommended External API Directions

#### Azure AI Speech Pronunciation Assessment

Best use:

- real pronunciation scoring;
- word-level and fluency-oriented learner feedback;
- a stronger future replacement for the current heuristic scorer.

Suggested role in Fluent Echo:

- keep local Whisper for private/offline mode;
- add Azure as an optional `cloud scoring mode`;
- keep the scorer behind a separate service interface so the UI and presenter do not need a rewrite.

#### Google Cloud Speech-to-Text With Adaptation

Best use:

- cloud fallback recognition;
- phrase and vocabulary biasing for lesson-specific content;
- benchmarking local Whisper against a managed STT stack.

Suggested role in Fluent Echo:

- optional fallback recognizer for difficult microphones, noisy rooms, or evaluation builds;
- not a replacement for the current local-first positioning unless the product direction changes.

#### Deepgram Speech-to-Text

Best use:

- cloud fallback transcription;
- keyword boosting for target lesson vocabulary;
- latency and accuracy benchmarking against local Whisper.

Suggested role in Fluent Echo:

- optional benchmark mode or cloud fallback path;
- especially useful if future testing shows that some lesson types consistently fail on-device hardware.

### Integration Rule

If external APIs are introduced later, keep this split:

- local Whisper remains the private, offline default;
- cloud recognizers stay optional;
- cloud pronunciation scoring lives behind a separate scoring interface;
- UI copy must clearly state when audio stays local and when it is sent to a cloud service.

That rule preserves the current architecture and keeps the product honest.

## Documentation

- [Architecture Brief](Fluent-Echo/Assets/_FluentEcho/Docs/MB_ARCHITECTURE.md)
- [Prototype Overview](Fluent-Echo/Assets/_FluentEcho/Docs/PROTOTYPE_OVERVIEW.md)
- [Improvement Roadmap](Fluent-Echo/Assets/_FluentEcho/Docs/IMPROVEMENT_ROADMAP.md)
- [Dependency Map](Fluent-Echo/Assets/_FluentEcho/Docs/DEPENDENCY_MAP.md)
- [Case Study Brief](Fluent-Echo/Assets/_FluentEcho/Docs/CASE_STUDY_BRIEF.md)
- [Portfolio Demo Script](Fluent-Echo/Assets/_FluentEcho/Docs/PORTFOLIO_DEMO_SCRIPT.md)

## Engineering Focus

The current implementation emphasizes a few explicit priorities:

- local AI integration with a clear privacy boundary;
- maintainable Unity architecture with separated runtime responsibilities;
- content scalability through ScriptableObjects and catalog-based lesson composition;
- scene-driven presentation that remains editable in the Unity editor;
- transparent UX around recognition quality, uncertainty, and scoring limits.

These priorities are intended to keep the project readable, extensible, and stable as additional scoring, content, and presentation systems are introduced.
