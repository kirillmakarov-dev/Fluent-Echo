# Fluent Echo Architecture Brief

This brief describes how the current Fluent Echo prototype is structured in the active Unity checkout.

## Purpose

Fluent Echo is an offline English-speaking practice prototype built around three concerns:

1. capture microphone input locally in Unity;
2. transcribe it with Whisper;
3. compare the transcript against lesson-specific speech exercises.

The current prototype is still a vertical slice, but the code already separates UI, presentation, domain logic, and speech services.

## Runtime Layers

### `Runtime/Data`

ScriptableObject-based lesson and settings data.

- `SpeechExerciseSO` stores the prompt, accepted phrases, target words, and progress key.
- `SpeechExerciseCatalogSO` stores the lesson sequence used by the prototype.
- `WhisperSettingsSO` stores the active Whisper profile and model/configuration settings.

### `Runtime/Domain`

Pure logic with no direct Unity UI dependency.

- `SpeechAnswerMatcher` compares recognized text with lesson targets.
- `SpeechSession` tracks the current recording state.
- `SpeechMatchResult` stores the outcome of a transcript match.

### `Runtime/Services`

Speech infrastructure and persistence.

- `ISpeechRecognitionService` defines the speech input contract.
- `WhisperSpeechRecognitionService` wraps local Whisper transcription.
- `MockSpeechRecognitionService` provides the deterministic demo path.
- `LessonProgressRepository` stores per-lesson progress in `PlayerPrefs`.

### `Runtime/Presentation`

Presenter-facing orchestration.

- `FluentEchoPresenter` binds a lesson, the active service, and the view.
- `IFluentEchoView` keeps the presenter decoupled from the concrete Unity view.

### `Runtime/Views`

Unity UI implementation.

- `FluentEchoView` owns the interactive buttons, labels, chips, and status display.
- `WordChipView` renders word-level highlight chips.

### `Runtime/Bootstrap`

Scene wiring and startup logic.

- `FluentEchoBootstrap` selects the active lesson, wires services, and attaches runtime dropdowns.

### `Editor`

Prototype-scene generation and rebuild tooling.

- `FluentEchoPrototypeBuilder` reconstructs the demo scene, data assets, runtime objects, and UI.

## Current Flow

1. The prototype scene is built or opened.
2. Bootstrap resolves the selected lesson and the active Whisper profile.
3. Presenter loads the lesson, progress state, and UI bindings.
4. The active speech service prepares the model.
5. The user records a phrase.
6. Whisper produces a transcript.
7. `SpeechAnswerMatcher` checks the transcript against the lesson target.
8. Progress is saved when the attempt resolves.

## Key Implementation Notes

- Lesson switching is catalog-driven, not scene-driven.
- Whisper profile switching is persisted and the scene is reloaded to apply the selected model.
- Mic selection is persisted through the microphone component and exposed in UI.
- The prototype currently uses transcript matching, not phoneme-level pronunciation scoring.
- Cancellation is treated as an important UX path, especially during lesson switching and retry flows.

## Important Files

- `Assets/_FluentEcho/Runtime/Bootstrap/FluentEchoBootstrap.cs`
- `Assets/_FluentEcho/Runtime/Presentation/FluentEchoPresenter.cs`
- `Assets/_FluentEcho/Runtime/Views/FluentEchoView.cs`
- `Assets/_FluentEcho/Runtime/Services/WhisperSpeechRecognitionService.cs`
- `Assets/_FluentEcho/Runtime/Services/WhisperSettingsSO.cs`
- `Assets/_FluentEcho/Runtime/Data/SpeechExerciseSO.cs`
- `Assets/_FluentEcho/Editor/FluentEchoPrototypeBuilder.cs`

