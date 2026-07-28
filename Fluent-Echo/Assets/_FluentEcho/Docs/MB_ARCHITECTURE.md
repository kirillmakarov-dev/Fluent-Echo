# Fluent Echo Architecture Brief

This brief summarizes how the current Fluent Echo prototype is organized in the active Unity checkout.

## Current shape

Fluent Echo is a local speech-practice slice built around three concerns:

1. Capture microphone audio and run local Whisper transcription.
2. Compare the transcript against lesson rules and save progress.
3. Present the result in a compact Unity UI that can later be expanded into a portfolio scene.

The current implementation keeps those concerns separated so the project can grow without turning into one large controller.

## Main layers

### `Runtime/Data`

ScriptableObject lesson content:

- `SpeechExerciseSO` stores the prompt, target words, accepted phrases, progress key, and reference audio.
- `SpeechExerciseCatalogSO` groups multiple lessons for lesson navigation.

### `Runtime/Domain`

Pure logic and state:

- `SpeechAnswerMatcher` handles transcript-to-lesson matching.
- `SpeechSession` tracks the current attempt state.
- `SpeechMatchResult` carries match flags and completion state.

### `Runtime/Services`

Infrastructure and scoring:

- `ISpeechRecognitionService` defines the speech pipeline contract.
- `WhisperSpeechRecognitionService` runs the local Whisper flow.
- `MockSpeechRecognitionService` provides deterministic demo behavior.
- `WhisperSettingsSO` stores model/profile configuration.
- `LessonProgressRepository` persists lesson progress in `PlayerPrefs`.
- `HeuristicPronunciationScoringService` provides the current MVP pronunciation score.

### `Runtime/Presentation`

The presenter wires everything together:

- It selects the active lesson.
- It binds the active speech service.
- It forwards transcript updates to the view.
- It controls success, retry, cancellation, and lesson switching.

### `Runtime/Views`

Unity UI components:

- `FluentEchoView` exposes buttons, labels, chips, and toggles.
- `WordChipView` shows per-word highlights.

### `Runtime/Bootstrap`

Scene composition:

- `FluentEchoBootstrap` creates or wires runtime UI controls.
- It also sets up lesson selection, microphone selection, Whisper quality selection, and the visible status labels.

### `Editor`

Prototype generation:

- `FluentEchoPrototypeBuilder` rebuilds the demo scene and demo assets.
- It also keeps the editor-safe Whisper settings in a CPU-only configuration.

### `Tests/Editor`

Current edit-mode coverage:

- `SpeechAnswerMatcherTests`
- `SpeechSessionTests`
- `WhisperSettingsTests`
- `PronunciationScoringServiceTests`

## Runtime flow

1. Bootstrap selects the lesson and wires UI controls.
2. Presenter binds the chosen speech service.
3. Service prepares Whisper or demo mode.
4. User starts recording.
5. Transcript updates stream into the presenter.
6. Presenter evaluates the answer, saves progress, and now also calculates heuristic pronunciation feedback.
7. View shows transcript, word highlights, progress, and the current pronunciation score.

## Important notes

- The app uses local Whisper, not a cloud speech API.
- Editor mode is forced onto the CPU path for Whisper to avoid the Vulkan crash path.
- The pronunciation score is still heuristic. It is useful for MVP feedback, but it is not true phonetic assessment yet.
- New systems should keep following the existing separation between data, domain, services, presentation, and view.
