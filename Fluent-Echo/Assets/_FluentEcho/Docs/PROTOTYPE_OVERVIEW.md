# Fluent Echo Prototype

## Why This Prototype Exists

Fluent Echo is a local, portfolio-oriented speech practice vertical slice for English pronunciation and speaking practice.
It shows local speech recognition in Unity, clean scene-owned UI wiring, and a transparent feedback pipeline.

The project is intentionally built as a 2D interface inside a 3D URP project so the visual layer can later evolve into a fuller character-driven presentation.

## What Already Works

- Ready scene: `Assets/_FluentEcho/Demo/Scenes/FluentEchoPrototype.unity`
- Practice flow: say `The dog is big`
- Local speech recognition through Whisper without sending audio to the cloud
- Automatic pause detection and recognized-text output
- Word coverage checking, accepted alternatives like `big|large`, and small recognition errors
- Correct word highlighting, result flow, and retry support
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

## Whisper Model

The Whisper model is stored in:

`Assets/StreamingAssets/Whisper/ggml-tiny.en.bin`

## How To Verify

1. If Unity asks to reload the changed scene from disk, choose `Reload`.
2. If needed, run `Tools > Fluent Echo > Rebuild Prototype`.
3. Open `FluentEchoPrototype` and press Play.
4. Press `RUN DEMO ANSWER`.
5. After a short pause, `the dog is big` should appear, the word chips should turn green, and the status should read `ANSWER ACCEPTED`.
6. Press `NEW ATTEMPT` to reset the exercise.
7. Turn off `Use deterministic demo engine`.
8. Press `START SPEAKING`, say `The dog is big`, and pause.
9. Whisper should output text, highlight words, and complete the exercise.

If Windows or Unity asks for microphone permission, grant it manually.
The first Whisper warm-up can take a few seconds while the model loads.

Edit-mode tests can be run through `Window > General > Test Runner > EditMode > Run All`.
The expected result is the full edit-mode test set passing.

## Current Limitation

The prototype checks transcript quality, not phoneme quality.
Professional pronunciation, stress, and accent scoring should be added through a separate scoring service without changing the UI or presenter boundary.

## Case Study

For a portfolio explanation, start with `CASE_STUDY_BRIEF.md`.
It gives a one-minute story, the core architecture, the limitations, and the next steps for a reviewer.
