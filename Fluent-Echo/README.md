# Fluent Echo

Fluent Echo is a Unity-based offline English speech-practice prototype built as a portfolio case study.
It demonstrates a local-first speech loop with Whisper, scene-owned UI, category-based lesson content, and a feedback flow that stays honest about what the current system can and cannot measure.

This project is meant to show product thinking and architecture, not just a visual mockup.
The current app focuses on:

- local Whisper transcription;
- transparent practice scoring instead of fake "AI pronunciation magic";
- scene-driven UI that can be adjusted by hand in Unity;
- ScriptableObject-based lesson content;
- a clean separation between composition root, presentation, domain, services, and content.

## Project Goal

The product promise is simple:

- audio stays on the device;
- speech recognition runs locally;
- the app supports repeatable speaking practice without a cloud dependency;
- the UI does not pretend the current heuristic scorer is full phoneme-level pronunciation assessment.

That makes Fluent Echo a strong portfolio slice for:

- Unity application architecture;
- local AI integration;
- UX around privacy and speech interaction;
- maintainable content pipelines for learning products.

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

The runtime should update state and text, but it should not silently rebuild the layout or override manual scene composition.

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

- [`Assets/_FluentEcho/Docs/DEPENDENCY_MAP.md`](C:/Portfolio%20Projects/Fluent-Echo/Fluent-Echo/Assets/_FluentEcho/Docs/DEPENDENCY_MAP.md)
- [`Assets/_FluentEcho/Docs/MB_ARCHITECTURE.md`](C:/Portfolio%20Projects/Fluent-Echo/Fluent-Echo/Assets/_FluentEcho/Docs/MB_ARCHITECTURE.md)

## Whisper Models And Recommended Profiles

The project currently supports three local Whisper profiles:

- `Fast` -> `ggml-tiny.en.bin`
- `Balanced` -> `ggml-base.en.bin`
- `Accurate` -> `ggml-small.en.bin`

Configured in:

- [`Assets/_FluentEcho/Runtime/Services/WhisperSettingsSO.cs`](C:/Portfolio%20Projects/Fluent-Echo/Fluent-Echo/Assets/_FluentEcho/Runtime/Services/WhisperSettingsSO.cs)

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

- the new recognition bias is tuned for short utterances;
- tiny English models can behave better on narrow expected vocabulary;
- larger models sometimes normalize short speech into the wrong but plausible English output.

In other words:

- `Fast` is currently the safest default for word drills;
- `Balanced` is the safer default for sentence drills;
- `Accurate` should be treated as an optional heavier pass, not an automatic upgrade for every lesson type.

## How To Set Up The Project

1. Open the Unity project:
   - [`Fluent-Echo`](C:/Portfolio%20Projects/Fluent-Echo/Fluent-Echo)
2. Make sure the Whisper model files are present in `Assets/StreamingAssets/Whisper/`.
3. Open the scene:
   - [`Assets/_FluentEcho/Demo/Scenes/FluentEchoPrototype.unity`](C:/Portfolio%20Projects/Fluent-Echo/Fluent-Echo/Assets/_FluentEcho/Demo/Scenes/FluentEchoPrototype.unity)
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

- [`Assets/_FluentEcho/Demo/Audio/References/`](C:/Portfolio%20Projects/Fluent-Echo/Fluent-Echo/Assets/_FluentEcho/Demo/Audio/References/)

Then assign the clip in the corresponding `SpeechExerciseSO` asset through the `Reference Audio` field.

Example lesson assets:

- [`Assets/_FluentEcho/Demo/Data/Words_01_Apple.asset`](C:/Portfolio%20Projects/Fluent-Echo/Fluent-Echo/Assets/_FluentEcho/Demo/Data/Words_01_Apple.asset)
- [`Assets/_FluentEcho/Demo/Data/Short_04_Apple.asset`](C:/Portfolio%20Projects/Fluent-Echo/Fluent-Echo/Assets/_FluentEcho/Demo/Data/Short_04_Apple.asset)

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

- [`Assets/_FluentEcho/Runtime/Data/SpeechExerciseCatalogSO.cs`](C:/Portfolio%20Projects/Fluent-Echo/Fluent-Echo/Assets/_FluentEcho/Runtime/Data/SpeechExerciseCatalogSO.cs)

The content flow should stay:

`new lesson asset -> catalog assignment -> scene UI uses existing presenter flow`

That keeps content extensible without rewriting runtime code.

## How To Adjust The UI

The current direction is scene-driven UI.

That means you should adjust layout manually in Unity for:

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

The most useful next improvements are not random polish items.
They fall into a few clear buckets.

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

Why it matters:

- this is the clearest upgrade path if the goal is real assessment rather than transcript-only practice.

Suggested role in Fluent Echo:

- keep local Whisper for private/offline mode;
- add Azure as an optional `cloud scoring mode`;
- keep the scorer behind a separate service interface so the UI and presenter do not need a rewrite.

#### Google Cloud Speech-to-Text With Adaptation

Best use:

- cloud fallback recognition;
- phrase and vocabulary biasing for lesson-specific content;
- benchmarking local Whisper against a managed STT stack.

Why it matters:

- phrase adaptation is useful when the product uses a constrained lesson vocabulary and you want the recognizer to prefer expected words.

Suggested role in Fluent Echo:

- optional fallback recognizer for difficult microphones, noisy rooms, or evaluation builds;
- not a replacement for the current local-first positioning unless the product direction changes.

#### Deepgram Speech-to-Text

Best use:

- cloud fallback transcription;
- keyword boosting for target lesson vocabulary;
- latency and accuracy benchmarking against local Whisper.

Why it matters:

- it is a practical option when you want to compare local transcription quality against a modern speech API without redesigning the app.

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

## Useful Project Docs

- [`Assets/_FluentEcho/Docs/PROTOTYPE_OVERVIEW.md`](C:/Portfolio%20Projects/Fluent-Echo/Fluent-Echo/Assets/_FluentEcho/Docs/PROTOTYPE_OVERVIEW.md)
- [`Assets/_FluentEcho/Docs/DEPENDENCY_MAP.md`](C:/Portfolio%20Projects/Fluent-Echo/Fluent-Echo/Assets/_FluentEcho/Docs/DEPENDENCY_MAP.md)
- [`Assets/_FluentEcho/Docs/MB_ARCHITECTURE.md`](C:/Portfolio%20Projects/Fluent-Echo/Fluent-Echo/Assets/_FluentEcho/Docs/MB_ARCHITECTURE.md)
- [`Assets/_FluentEcho/Docs/IMPROVEMENT_ROADMAP.md`](C:/Portfolio%20Projects/Fluent-Echo/Fluent-Echo/Assets/_FluentEcho/Docs/IMPROVEMENT_ROADMAP.md)
- [`Assets/_FluentEcho/Docs/SPRINT_ROADMAP_LOCAL_SCORING_ONBOARDING.md`](C:/Portfolio%20Projects/Fluent-Echo/Fluent-Echo/Assets/_FluentEcho/Docs/SPRINT_ROADMAP_LOCAL_SCORING_ONBOARDING.md)

## Portfolio Framing

If you are reviewing this repo as a hiring manager or teammate, the value of this project is in the combination of:

- local AI integration;
- honest UX around speech uncertainty;
- maintainable Unity architecture;
- content scalability through ScriptableObjects;
- scene-driven presentation that stays editable by hand.
