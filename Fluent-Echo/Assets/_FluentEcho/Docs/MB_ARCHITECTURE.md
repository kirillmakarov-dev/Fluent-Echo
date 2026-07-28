# Fluent Echo Architecture Brief

This document describes how the current Fluent Echo prototype is structured in the active Unity checkout.
It is meant to help a future contributor or portfolio reviewer understand the app quickly without reading every script.

## Product Snapshot

Fluent Echo is a local speech-practice vertical slice built for portfolio presentation.
The app is designed around this loop:

`launch -> onboarding -> choose practice path -> record -> transcript -> estimate -> feedback -> retry or continue`

The key promise is privacy:

- audio stays on the device;
- Whisper runs locally;
- the app does not require paid APIs for the current roadmap;
- the UI should never pretend local Whisper is a phoneme-level pronunciation engine.

## What the Current App Demonstrates

- microphone selection and speech profile selection;
- local Whisper transcription with warm-up and profile switching;
- three lesson categories with multiple exercises;
- progress persistence per lesson;
- onboarding and user-friendly failure states;
- a heuristic pronunciation estimate that is explicitly presented as a practice score, not true phoneme scoring;
- a clear separation between the main result flow and the future phoneme-scoring roadmap;
- persisted lesson progress that now stores the best and last match quality breakdown as exact / approximate / missed counts;
- a result panel that separates practice score, confidence, word match, rhythm, and focus-next guidance;
- a result flow that supports retry, close, and next mission actions;
- inspector-friendly scene-owned UI wiring.

## Scene Ownership and Wiring

The visual layout should be controlled from the scene, not hidden runtime recreation.

### Scene-owned UI elements

- microphone dropdown;
- Whisper profile dropdown;
- lesson/category navigation;
- settings panel;
- result panel;
- onboarding / notice overlays;
- practice buttons and status text.

### What bootstrap should do

- resolve references that are already placed in the scene;
- bind missing callbacks;
- restore persisted selections;
- initialize services;
- keep the app functional when the scene is reloaded.

### What bootstrap should not do

- silently replace the panel layout with a different arrangement;
- move panels to new positions after Play mode starts unless a specific flow requires it;
- hide designer changes behind a code-driven layout reset.

This distinction matters because the scene should remain editable by hand.
A designer or developer should be able to move a panel, change anchors, or restyle a button in Edit mode and see the same composition in Play mode.

## Layer Map

### `Runtime/Data`

ScriptableObject content and cataloging:

- `SpeechExerciseSO` stores the prompt, target words, accepted phrases, progress key, and reference audio.
- `SpeechExerciseCatalogSO` groups exercises into practice categories and powers lesson navigation.

### `Runtime/Domain`

Pure logic and session state:

- `SpeechAnswerMatcher` compares the transcript with the lesson target;
- `SpeechSession` tracks whether the app is idle, listening, analyzing, retrying, cancelling, or in error;
- `SpeechMatchResult` carries the transcript-match flags and completion state.

### `Runtime/Services`

Infrastructure and scoring:

- `ISpeechRecognitionService` defines the speech pipeline contract;
- `WhisperSpeechRecognitionService` runs the local Whisper transcription flow;
- `MockSpeechRecognitionService` provides deterministic demo mode behavior;
- `WhisperSettingsSO` stores model/profile configuration and progress-friendly lesson history data;
- `LessonProgressRepository` persists progress in `PlayerPrefs`;
- `IPronunciationScoringService` and `HeuristicPronunciationScoringService` compute the current local pronunciation estimate.
- `IPhonemeAlignmentService` and `PhonemeAlignmentResult` define the future phoneme-aware scoring boundary without changing the current heuristic path.

Important note:

- the scoring service is heuristic;
- it combines transcript match, rhythm, word quality, and confidence into a transparent practice score;
- it does not claim to measure phonemes directly.
- the future phoneme-aware path is now modeled as a separate additive contract instead of a rewrite of Whisper.

### `Runtime/Presentation`

Presenter layer that orchestrates the flow:

- selects the active lesson and category;
- binds the active speech service;
- handles retry, cancellation, category switching, and lesson switching;
- maps internal service errors to user-facing onboarding and failure states;
- keeps the result text readable and coach-like.

### `Runtime/Views`

Unity UI bindings:

- `FluentEchoView` exposes buttons, labels, chips, toggles, and the visible panels;
- it owns the category screen, settings panel, result panel, and notice/onboarding overlay;
- `WordChipView` shows per-word highlights.

### `Runtime/Bootstrap`

Scene composition and inspector-friendly wiring:

- `FluentEchoBootstrap` wires or repairs scene-owned UI controls;
- it sets up microphone dropdowns, Whisper profile dropdowns, lesson dropdowns, result panel labels, and settings panel access;
- it keeps the main scene usable even when parts of the UI are reconstructed at runtime.

### `Editor`

Prototype generation:

- `FluentEchoPrototypeBuilder` rebuilds the demo scene and demo assets;
- it keeps the scene and catalog in sync;
- it also preserves the CPU-safe Whisper setup used in the editor.

### `Tests/Editor`

Edit-mode coverage currently focuses on:

- `SpeechAnswerMatcherTests`
- `SpeechSessionTests`
- `WhisperSettingsTests`
- `PronunciationScoringServiceTests`
- `FluentEchoPresenterTests`

## Runtime Flow

1. Bootstrap resolves the current exercise, wiring, and saved selection.
2. Presenter binds the current category, lesson, and speech service.
3. Service prepares Whisper or demo mode.
4. First launch may show onboarding and microphone guidance.
5. User records speech.
6. Whisper returns transcript text.
7. The matcher checks word coverage and accepted phrases.
8. The heuristic scorer produces a local pronunciation estimate and confidence band.
9. The progress layer stores the attempt history, including best and last match quality breakdowns.
10. The view shows transcript, word highlighting, progress, and a result panel broken into practice score, confidence, word match, rhythm, match-quality summary, and focus-next guidance.
11. The user can retry, close the result panel, move to the next mission, or return to categories.

## User-Facing Panels

The current scene is intentionally split into visible layers:

- the main practice area;
- a separate settings panel for microphone and Whisper profile controls;
- a separate category screen for practice paths;
- a separate result panel for end-of-lesson feedback;
- a notice/onboarding overlay for first launch and friendly error handling.

This separation is important because it lets the visual layout be adjusted in the scene without rewriting the core logic.

## Honest Scoring Model

The app now uses language that is intentionally careful:

- `Practice Score` and `Pronunciation estimate` language instead of a claim of full phoneme scoring;
- `Recognition confidence` instead of pretending the model knows accent quality;
- `Transcript match`, `Word match`, `Rhythm`, and `Word focus` as visible sub-signals;
- a clear path for future phoneme-level scoring without pretending it exists today.

The current scorer is useful for MVP feedback and portfolio demonstration, but it should be treated as an estimate pipeline.

## Portfolio Case Study Angle

When presenting the project, the strongest story is:

1. The app is private by design.
2. The experience is completely local.
3. The architecture separates speech transcription, answer matching, scoring, and UI.
4. The UI is honest about limitations.
5. The result flow is coach-like, not debug-like.
6. The roadmap leaves room for true phoneme scoring later without breaking the current system.

Suggested one-minute case-study summary:

- a user launches the app;
- the app explains privacy and microphone use;
- the user chooses a practice category;
- the user records speech locally;
- Whisper returns a transcript;
- the app shows a transparent pronunciation estimate and clear next-step feedback;
- the user retries or continues to the next mission.

## Important Notes

- The app uses local Whisper, not a cloud speech API.
- Editor mode is forced onto the CPU path for Whisper stability.
- The pronunciation pipeline is intentionally honest about its limitations.
- Any future system should keep the same separation between data, domain, services, presentation, and views.
