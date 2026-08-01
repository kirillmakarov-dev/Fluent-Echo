# Fluent Echo

Fluent Echo is a portfolio-oriented Unity speech-practice prototype.

It exists to show how a small product can still be built with real architectural discipline: explicit data models, a clean runtime split, scene-owned UI, local speech processing, and a result flow that is honest about what the system can and cannot measure.

This is not a content dump project. It is a resume project meant to demonstrate product thinking, code organization, and careful scene composition.

## Product Snapshot

Fluent Echo is a local English speaking practice app built around this loop:

`open app -> choose a category -> record speech -> get local transcript -> get practice feedback -> retry or continue`

The key design choice is that audio stays on the device. Whisper runs locally, the UI stays inspectable in the scene, and the app does not depend on paid speech APIs.

## What The Project Demonstrates

- Local microphone capture with device selection.
- Local Whisper transcription with warm-up and profile switching.
- Category-based lesson navigation.
- Progress persistence per lesson.
- A transparent practice-score pipeline.
- Scene-owned UI wiring instead of hidden runtime layout rebuilding.
- An honest separation between transcript quality and pronunciation estimation.
- A result panel that reads like coaching feedback, not a debug log.

## Layer Map

The code is organized around small, explicit responsibilities:

- `Runtime/Data` - `SpeechExerciseSO`, `SpeechExerciseCatalogSO`, and `WhisperSettingsSO`.
- `Runtime/Domain` - `SpeechAnswerMatcher`, `SpeechSession`, and lesson state.
- `Runtime/Services` - speech recognition, scoring, persistence, and Whisper configuration.
- `Runtime/Presentation` - `FluentEchoPresenter` and the flow orchestration layer.
- `Runtime/Views` - `FluentEchoView` and the visible Unity panels.
- `Runtime/Bootstrap` - scene wiring and service composition.
- `Tests/Editor` - edit-mode coverage for matcher, session state, scoring, and presenter flows.

Sample content lives under `Fluent-Echo/Assets/_FluentEcho/Demo/Data`.
The main demo scene is `Fluent-Echo/Assets/_FluentEcho/Demo/Scenes/FluentEchoPrototype.unity`.

## Architecture

The runtime flow is intentionally simple:

`Scene -> Bootstrap -> Presenter -> Domain / Services -> View -> User`

The important boundary is this:

- Whisper turns speech into text.
- The domain layer checks whether the lesson target was hit.
- The scoring layer produces an honest practice estimate.
- The view renders the result.

Whisper is not treated as a pronunciation engine. The current scorer is intentionally described as a practice estimate, not a phoneme-level assessment.

For a fuller diagram, see [Fluent-Echo/Assets/_FluentEcho/Docs/DEPENDENCY_MAP.md](Fluent-Echo/Assets/_FluentEcho/Docs/DEPENDENCY_MAP.md).

## How The App Works

1. The scene loads.
2. `FluentEchoBootstrap` resolves the existing scene references.
3. `FluentEchoPresenter` selects the current category and lesson.
4. The active speech service prepares Whisper or demo mode.
5. The user records a phrase or chooses a demo attempt.
6. Whisper returns a transcript.
7. The matcher compares that transcript against the lesson target.
8. The scoring service builds a local practice estimate.
9. The view shows transcript, word match, confidence, rhythm, and a focused next step.
10. Progress is saved per lesson.

## How To Run

1. Open the Unity project in the nested `Fluent-Echo` folder.
2. Load `Fluent-Echo/Assets/_FluentEcho/Demo/Scenes/FluentEchoPrototype.unity`.
3. If Unity asks to reload the scene from disk, choose `Reload`.
4. Press Play.

If you want the deterministic path for quick checks, use Demo Mode first.

## How To Add A New Lesson

1. Create a new `SpeechExerciseSO` asset.
2. Fill in the prompt, target words, accepted phrases, and progress key.
3. Add an optional reference audio clip if you have one.
4. Add the exercise to the correct category inside `SpeechExerciseCatalogSO`.
5. Save the asset and reopen the scene if Unity does not refresh the catalog immediately.

## How To Add A New Category

Categories are data-driven, but the scene still needs to expose them intentionally.

1. Add a new `SpeechExerciseCategory` entry to `SpeechExerciseCatalogSO`.
2. Give it a clear display name and short description.
3. Assign the exercises that belong to that path.
4. Add or wire the matching category button in the scene if the UI should expose it directly.
5. Update the category screen copy so the new path makes sense to a user at first glance.

If you only add lesson assets, the catalog can still consume them. If you want a new first-class category in the UI, the scene should expose it explicitly.

## Honest Scoring Model

The current scoring pipeline is deliberate about its limits.

- Whisper provides transcript text.
- The matcher checks word coverage and accepted alternatives.
- The scorer turns that into a practice estimate.
- The UI labels it as an estimate, not as full phoneme scoring.

That choice matters for the portfolio story. It shows engineering honesty instead of overclaiming capability.

## Why This Works For A Resume

This project is useful in a portfolio because it shows more than feature output.

It shows:

- how the UI is composed and owned by the scene;
- how data is isolated from presentation;
- how an app can stay local and privacy-first;
- how to structure a real-time flow without letting the code turn into one big controller;
- how to present product limitations clearly instead of hiding them.

In other words, the value here is not only that the app works. The value is that the app is built in a way another engineer can read, extend, and trust.

## Documentation

- [Architecture Brief](Fluent-Echo/Assets/_FluentEcho/Docs/MB_ARCHITECTURE.md)
- [Next Steps Brief](Fluent-Echo/Assets/_FluentEcho/Docs/MB_NEXT_STEPS.md)
- [Case Study Brief](Fluent-Echo/Assets/_FluentEcho/Docs/CASE_STUDY_BRIEF.md)
- [Portfolio Demo Script](Fluent-Echo/Assets/_FluentEcho/Docs/PORTFOLIO_DEMO_SCRIPT.md)
- [Sprint Roadmap](Fluent-Echo/Assets/_FluentEcho/Docs/SPRINT_ROADMAP_LOCAL_SCORING_ONBOARDING.md)
- [Dependency Map](Fluent-Echo/Assets/_FluentEcho/Docs/DEPENDENCY_MAP.md)

## Scope

This repository is intentionally a vertical slice.
The goal is to present a strong engineering story, not to pretend the prototype is a finished consumer app.

The current roadmap keeps the work honest:

1. keep the interaction loop stable;
2. improve the local scoring story;
3. polish the portfolio presentation;
4. keep the architecture readable enough that the next engineer can extend it without reverse engineering the scene.
