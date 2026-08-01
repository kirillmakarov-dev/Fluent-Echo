# Fluent Echo Prototype

## Why This Prototype Exists

Fluent Echo is a local, portfolio-oriented speech practice vertical slice for English pronunciation and speaking practice.
It shows local speech recognition in Unity, clean scene-owned UI wiring, and a transparent feedback pipeline.

The project is intentionally built as a 2D interface inside a 3D URP project so the visual layer can later evolve into a fuller character-driven presentation.

## What Already Works

- Ready scene: `Assets/_FluentEcho/Demo/Scenes/FluentEchoPrototype.unity`
- First-launch onboarding that explains privacy and practice paths in two steps
- Category-first practice flow with three paths: Words, Short Sentences, and Challenge Sentences
- Multiple lessons per category, with `Next` / `Prev` navigation and saved progress
- Local speech recognition through Whisper without sending audio to the cloud
- Automatic pause detection and recognized-text output
- Word coverage checking, accepted alternatives like `big|large`, and small recognition errors
- Correct word highlighting, practice-score result flow, and retry support
- Separate result breakdown for practice score, confidence, word match, rhythm, and focus next
- Result actions for `TRY AGAIN`, `NEXT MISSION`, and `CLOSE`
- Deterministic Demo Mode for UI verification without a microphone
- ScriptableObject lesson configuration
- Editor tests for the core matching logic

## Core Structure

- `Runtime/Data` - lesson data
- `Runtime/Domain` - Unity-independent matching rules and session state
- `Runtime/Services` - speech recognition interface, Whisper, and mock implementations
- `Runtime/Presentation` - presenter that connects lesson, service, and view
- `Runtime/Views` - Unity UI and word chips
- `Runtime/Bootstrap` - scene dependency wiring
- `Editor/FluentEchoPrototypeBuilder.cs` - full demo-scene rebuild
- `Tests/Editor` - edit-mode tests for the matcher, scoring, and presenter flows

## Whisper Models

The prototype currently supports these local Whisper profiles:

- `Fast` -> `Assets/StreamingAssets/Whisper/ggml-tiny.en.bin`
- `Balanced` -> `Assets/StreamingAssets/Whisper/ggml-base.en.bin`
- `Accurate` -> `Assets/StreamingAssets/Whisper/ggml-small.en.bin`

Recommended usage right now:

- `Words` -> `Fast`
- `Short Sentences` -> `Balanced`
- `Challenge Sentences` -> `Balanced` first, `Accurate` only when needed

This recommendation reflects the current recognition pipeline.
Short single-word drills currently behave best on `Fast`, while sentence exercises benefit more from the larger profiles.

## How To Verify

1. If Unity asks to reload the changed scene from disk, choose `Reload`.
2. If needed, run `Tools > Fluent Echo > Rebuild Prototype`.
3. Open `FluentEchoPrototype` and press Play.
4. Press `SHOW DEMO`.
5. After a short pause, the expected transcript should appear, the word chips should turn green, and the result flow should open.
6. Press `TRY AGAIN` to reset the exercise.
7. If Demo Mode is enabled, turn it off in the scene-owned controls first.
8. Press `START SPEAKING`, say the expected word or sentence, and pause.
9. Whisper should output text, highlight matched words, and complete the exercise when the answer is accepted.

If Windows or Unity asks for microphone permission, grant it manually.
The first Whisper warm-up can take a few seconds while the model loads.

Edit-mode tests can be run through `Window > General > Test Runner > EditMode > Run All`.
The expected result is the full edit-mode test set passing.

## Current Limitation

The prototype uses a local heuristic practice score, not true phoneme quality scoring.
Professional pronunciation, stress, and accent scoring should be added through a separate scoring service without changing the UI or presenter boundary.
The UI is honest about that limitation by separating transcript, practice score, confidence, word match, rhythm, and focus-next feedback.
The next portfolio step is to keep the story readable while the stronger scoring pipeline is designed.

## Case Study

For a portfolio explanation, start with `CASE_STUDY_BRIEF.md`.
It gives a one-minute story, the current architecture, the limitations, and the next steps for a reviewer.
For a live walkthrough, open `PORTFOLIO_DEMO_SCRIPT.md`.
For the higher-level system map, read `MB_ARCHITECTURE.md`.
