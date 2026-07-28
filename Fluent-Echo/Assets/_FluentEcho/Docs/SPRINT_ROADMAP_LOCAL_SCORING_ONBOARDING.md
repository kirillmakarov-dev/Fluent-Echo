# Fluent Echo Sprint Roadmap - Local Scoring and Onboarding

This roadmap describes the next portfolio-quality work without paid APIs.
The goal is to make Fluent Echo feel like a finished local speech-practice app while staying honest about what local Whisper can and cannot score.

## Product Goal

Bring the project from a strong prototype to a polished portfolio vertical slice:

`open app -> understand privacy -> choose practice path -> record -> get clear feedback -> retry or continue`

The app should not claim true phoneme-level pronunciation assessment until a real phoneme/alignment scorer exists.
Instead, it should present the current local scoring as a transparent pronunciation estimate based on recognition quality, word matching, rhythm, and attempt history.

## Constraints

- No paid APIs.
- No cloud speech processing.
- Keep audio local.
- Keep the current architecture separation: Data, Domain, Services, Presentation, Views, Bootstrap.
- Preserve inspector-driven UI wiring where possible.
- Do not hide limitations from the user or the portfolio case study.

## Sprint 1 - Polished Onboarding and Error States

### Objective

Make the first launch and failure states understandable for a new user.
The user should always know what is happening, what to press next, and how to fix microphone/model problems.

### Scope

- Add a lightweight onboarding panel or first-launch overlay.
- Explain the core promise: private offline English speech practice.
- Explain the three practice paths: Words, Short Sentences, Challenge Sentences.
- Explain microphone permission in friendly language.
- Add user-facing states for missing microphone, blocked permission, missing model, first model load, no speech detected, and failed analysis.
- Keep settings accessible but not visually mixed with the lesson flow.

### Recommended UX Flow

1. Welcome
   - Title: `Practice English privately`
   - Body: `Fluent Echo listens on this device and uses a local speech model.`
   - CTA: `CONTINUE`

2. Choose Practice Path
   - Words: `Start with focused single-word practice.`
   - Short Sentences: `Build confidence with short, clear lines.`
   - Challenge Sentences: `Practice longer lines with smoother rhythm.`
   - CTA: `CHOOSE PATH`

3. Microphone Ready
   - Body: `Allow microphone access so Fluent Echo can check your answer. Your voice stays local.`
   - CTA: `START PRACTICING`

### Error and Empty States

Use these product-facing messages instead of technical logs:

- No microphone:
  - Title: `Microphone not found`
  - Body: `Connect a microphone or choose another input in Settings.`
  - Action: `OPEN SETTINGS`

- Permission blocked:
  - Title: `Microphone permission needed`
  - Body: `Allow microphone access in Windows privacy settings, then try again.`
  - Action: `TRY AGAIN`

- Model missing:
  - Title: `Speech model missing`
  - Body: `Add the local Whisper model to StreamingAssets/Whisper before practicing.`
  - Action: `OPEN SETTINGS`

- First load:
  - Title: `Preparing speech model`
  - Body: `The first launch can take a few seconds.`
  - Action: disabled until ready

- No speech detected:
  - Title: `I did not catch that`
  - Body: `Move closer to the microphone and try again.`
  - Action: `TRY AGAIN`

- Analysis failed:
  - Title: `Could not check this attempt`
  - Body: `The recording stopped before analysis finished. Please try again.`
  - Action: `TRY AGAIN`

### Implementation Notes

- Add view methods for onboarding and error states instead of pushing raw strings from services directly into UI.
- Keep service errors technical internally, but map them to user-friendly messages in the presenter.
- Store onboarding completion in `PlayerPrefs`.
- Add a reset/debug option later if needed, but do not expose it as a main user flow.

### Acceptance Criteria

- First launch shows onboarding before practice starts.
- Returning users can go directly to the category screen.
- User sees a friendly state if no microphone exists.
- User sees a friendly state if the model is missing.
- `START SPEAKING` is not available while the speech model is still preparing.
- Empty or silent recordings return to a retryable state.
- Existing category, lesson, settings, and result UI still work.

### Suggested Files

- `Assets/_FluentEcho/Runtime/Views/FluentEchoView.cs`
- `Assets/_FluentEcho/Runtime/Presentation/FluentEchoPresenter.cs`
- `Assets/_FluentEcho/Runtime/Bootstrap/FluentEchoBootstrap.cs`
- `Assets/_FluentEcho/Runtime/Services/WhisperSpeechRecognitionService.cs`
- `Assets/_FluentEcho/Editor/FluentEchoPrototypeBuilder.cs`
- `Assets/_FluentEcho/Demo/Scenes/FluentEchoPrototype.unity`

## Sprint 2 - Honest Local Pronunciation Estimate

### Objective

Make scoring more honest, more useful, and easier to explain in a portfolio.
The app should distinguish between recognized text and pronunciation quality estimate.

### Current Problem

Whisper is speech-to-text.
It can tell us which words were likely spoken, but it does not reliably score phonemes, stress, or accent quality.

The current heuristic scorer is useful, but the UI must not present it as true phoneme-level pronunciation assessment.

### New Scoring Language

Rename user-facing labels:

- `Pronunciation Score` -> `Pronunciation Estimate`
- `Score` -> `Practice Score`
- `Precision` -> `Recognition precision`
- `Tempo` -> `Rhythm`
- `Word detail` -> `Word focus`
- `Confidence` should be shown as a band: `High`, `Medium`, or `Low`

### Scoring Breakdown

The result panel should show:

- Practice Score
  - Overall local estimate.

- Word Match
  - How many target words were recognized.

- Recognition Confidence
  - High/Medium/Low based on match quality, fuzzy matches, extra words, missing words, and empty input.

- Rhythm
  - Attempt duration compared with expected phrase length.

- Focus Next
  - One clear coaching instruction.

### Confidence Heuristic

Use a transparent local confidence estimate:

- High:
  - all required words matched;
  - few or no extra words;
  - transcript is not empty;
  - duration is reasonable.

- Medium:
  - most required words matched;
  - fuzzy or alternative matches are present;
  - transcript has some extra words.

- Low:
  - many required words are missing;
  - transcript is empty or mostly noise;
  - duration is too short or too long.

### Data Model Direction

Add or evolve a scoring result model with fields like:

- `PracticeScore`
- `WordMatchScore`
- `ClarityEstimate`
- `RhythmScore`
- `ConfidenceBand`
- `MatchedWords`
- `MissingWords`
- `ExtraWords`
- `FeedbackText`
- `PhonemeDetails`

For now, `PhonemeDetails` can be empty or marked unavailable.
This keeps the architecture ready for a future real scorer without pretending it exists today.

### Service Architecture

Keep this boundary:

- `ISpeechRecognitionService`
  - Converts audio to transcript.

- `IPronunciationScoringService`
  - Converts transcript, target phrase, match result, and timing into feedback.

Future optional services:

- `IPhonemeAlignmentService`
  - Aligns expected phonemes with spoken audio.

- `IWordConfidenceProvider`
  - Provides word-level confidence if a future local model supports it.

### UI Result Panel

The result panel should read like a coach, not a debug console:

- Header: `Great work`
- Score block: `Practice Score: 87/100`
- Confidence: `Confidence: High`
- Word match: `Matched words: 4/4`
- Focus: `Keep the same rhythm on the next mission.`
- CTA buttons: `TRY AGAIN`, `NEXT MISSION`, `CLOSE`

When the user misses words:

- Header: `Almost there`
- Focus: `Repeat "window" slowly once, then say the full line again.`

When no speech is detected:

- Header: `I did not catch that`
- Focus: `Move closer to the microphone and try again.`

### Acceptance Criteria

- UI no longer implies true phoneme scoring.
- Result panel clearly separates transcript, word match, confidence, rhythm, and feedback.
- Silent input gets a useful retry message.
- Partial matches get one clear next step.
- Full matches get positive feedback and a next mission option.
- The architecture has a documented path for future phoneme scoring.
- Existing tests still pass.

### Suggested Files

- `Assets/_FluentEcho/Runtime/Services/WhisperSettingsSO.cs`
- `Assets/_FluentEcho/Runtime/Presentation/FluentEchoPresenter.cs`
- `Assets/_FluentEcho/Runtime/Domain/SpeechAnswerMatcher.cs`
- `Assets/_FluentEcho/Runtime/Views/FluentEchoView.cs`
- `Assets/_FluentEcho/Tests/Editor`

## Sprint 3 - Portfolio Case Study Polish

### Objective

Prepare the project to be shown confidently in a portfolio.
This sprint is not about adding large systems; it is about presentation, clarity, and proof of engineering decisions.

### Scope

- Update architecture docs with the onboarding and honest scoring pipeline.
- Add a short portfolio case-study section:
  - problem;
  - constraints;
  - local privacy approach;
  - architecture;
  - limitations;
  - future phoneme scoring path.
- Record or prepare a video-ready flow:
  - onboarding;
  - category selection;
  - recording;
  - transcript;
  - result feedback;
  - next mission.
- Add a clear note that paid APIs are intentionally not used.

### Acceptance Criteria

- A reviewer can understand the app in under one minute.
- A technical reviewer can understand the architecture in under five minutes.
- Limitations are framed professionally, not hidden.
- The demo flow is stable and repeatable.

### Suggested Files

- `Assets/_FluentEcho/Docs/MB_ARCHITECTURE.md`
- `Assets/_FluentEcho/Docs/MB_NEXT_STEPS.md`
- `Assets/_FluentEcho/Docs/PROTOTYPE_OVERVIEW.md`
- Portfolio README or external case study later.

## Recommended Implementation Order

1. Sprint 1: onboarding and friendly error states.
2. Sprint 2: honest local pronunciation estimate.
3. Sprint 3: portfolio case-study polish.

Do not start with paid APIs, custom ML training, or complex 3D systems.
The strongest next move is a reliable, honest, repeatable learning loop.

## Definition of Done for the Whole Roadmap

- The app can be launched by a new user without explanation.
- The user understands privacy and microphone requirements.
- Recording states never get stuck.
- Feedback is useful and honest.
- The result panel looks like a learning product, not a debug output.
- Architecture remains modular and documented.
- The project is ready to present as a local AI/Unity speech-practice vertical slice.
