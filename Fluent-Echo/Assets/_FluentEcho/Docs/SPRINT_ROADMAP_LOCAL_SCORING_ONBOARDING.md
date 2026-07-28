# Fluent Echo Sprint Roadmap

This roadmap is the working plan for turning the current prototype into a polished, honest, local speech-practice app without paid APIs.

## Product Goal

Ship a portfolio-ready Unity vertical slice that feels stable, readable, and intentional:

`open app -> understand privacy -> choose a practice path -> record -> get feedback -> retry or continue`

The app should stay honest about what local Whisper can do. It can produce transcripts and confidence signals, but it should not pretend to do phoneme-level pronunciation scoring until a real scorer exists.

## Constraints

- No paid APIs.
- No cloud speech processing.
- Keep audio local.
- Keep the current architecture split: Data, Domain, Services, Presentation, Views, Bootstrap.
- Prefer inspector-owned scene wiring over runtime recreation.
- Do not hide limitations from the user or the portfolio case study.

## Sprint Status

- Sprint 0 is complete.
- Scene-owned binding is now the default path in bootstrap.
- Runtime fallback creation has been removed for lesson, microphone, and Whisper profile controls.
- Notice overlay is now scene-owned instead of being created from code.
- Settings panel and notice UI are now assigned through the scene instead of being reconstructed at runtime.
- Progress, Whisper profile, and pronunciation labels are now also bound directly from scene-owned references.
- First-launch onboarding is now a two-step flow that explains privacy first and practice paths second.
- Sprint 3 lesson catalog and progress UX is now the active focus: category screens now surface cleared lessons in a separate progress label.
- Sprint 4 portfolio case-study polish is now complete: the docs mirror the current result breakdown and demo flow.
- Sprint 5 phoneme-aware scoring roadmap and architecture cleanup is now the next active focus.
- Sprint 6 final portfolio polish and demo packaging will come after the scoring roadmap is fully documented.

## Sprint 0 - Scene-Owned UI and Layout Lock-Down

### Objective

Make the scene itself the source of truth for the visual layout.
The user should be able to move panels, anchors, fonts, colors, and spacing in the scene without the bootstrap code silently rebuilding the UI into a different shape.

### Scope

- Keep microphone, Whisper profile, lesson selector, settings, and result panel as scene-owned objects.
- Remove any remaining runtime positioning that re-centers or reflows the main UI after Play mode starts.
- Ensure the top-level panels stay where they are placed in Edit mode.
- Keep the lesson dropdown, category screen, and settings panel visually separated.
- Keep the result panel as its own overlay instead of a tiny inline text block.
- Keep all current icons and button styles, but let the scene own their positions and sizes.

### Acceptance Criteria

- Moving a panel in Edit mode keeps that position in Play mode unless the scene explicitly changes it.
- Stopping Play mode does not snap back the result panel or settings panel because of hidden code-driven layout reset.
- The lesson dropdown still works after a reload.
- The settings panel can be opened and closed without breaking the lesson flow.

### Suggested Files

- `Fluent-Echo/Assets/_FluentEcho/Runtime/Bootstrap/FluentEchoBootstrap.cs`
- `Fluent-Echo/Assets/_FluentEcho/Runtime/Views/FluentEchoView.cs`
- `Fluent-Echo/Assets/_FluentEcho/Runtime/Presentation/FluentEchoPresenter.cs`
- `Fluent-Echo/Assets/_FluentEcho/Demo/Scenes/FluentEchoPrototype.unity`

### Verify After Sprint

- Open the scene in Edit mode and confirm the UI hierarchy matches the intended Play mode layout.
- Run Play mode and confirm the layout is not rebuilt into a different shape.
- Confirm the buttons and dropdowns remain clickable.

## Sprint 1 - Polished Onboarding and Error States

### Objective

Make the first launch and failure states understandable for a new user.
The user should always know what is happening, what to press next, and how to fix microphone or model problems.

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

Use product-facing messages instead of technical logs:

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

### Acceptance Criteria

- First launch shows onboarding before practice starts.
- Returning users can go directly to the category screen.
- The user sees a friendly state if no microphone exists.
- The user sees a friendly state if the model is missing.
- `START SPEAKING` is not available while the speech model is still preparing.
- Empty or silent recordings return to a retryable state.
- Existing category, lesson, settings, and result UI still work.

### Suggested Files

- `Fluent-Echo/Assets/_FluentEcho/Runtime/Views/FluentEchoView.cs`
- `Fluent-Echo/Assets/_FluentEcho/Runtime/Presentation/FluentEchoPresenter.cs`
- `Fluent-Echo/Assets/_FluentEcho/Runtime/Bootstrap/FluentEchoBootstrap.cs`
- `Fluent-Echo/Assets/_FluentEcho/Runtime/Services/WhisperSpeechRecognitionService.cs`
- `Fluent-Echo/Assets/_FluentEcho/Editor/FluentEchoPrototypeBuilder.cs`
- `Fluent-Echo/Assets/_FluentEcho/Demo/Scenes/FluentEchoPrototype.unity`

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
- `Precision` -> `Recognition Precision`
- `Tempo` -> `Rhythm`
- `Word detail` -> `Word Focus`
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

- `Fluent-Echo/Assets/_FluentEcho/Runtime/Services/WhisperSettingsSO.cs`
- `Fluent-Echo/Assets/_FluentEcho/Runtime/Presentation/FluentEchoPresenter.cs`
- `Fluent-Echo/Assets/_FluentEcho/Runtime/Domain/SpeechAnswerMatcher.cs`
- `Fluent-Echo/Assets/_FluentEcho/Runtime/Views/FluentEchoView.cs`
- `Fluent-Echo/Assets/_FluentEcho/Tests/Editor`

## Sprint 3 - Lesson Catalog, Progress, and Navigation

### Objective

Expand the content so the app feels like a real learning product instead of a single demo lesson.

### Scope

- Build a category-first starting flow.
- Keep three practice paths:
  - Words
  - Short Sentences
  - Challenge Sentences
- Populate each category with 8-10 exercises.
- Keep the `Next` / `Prev` navigation smooth and predictable.
- Persist the selected category, lesson index, and best result per exercise.
- Make lesson switching fast and deterministic.

### Content Targets

- Words:
  - short, concrete nouns and common vocabulary;
  - focused on clarity and pronunciation confidence.

- Short Sentences:
  - 4-6 word prompts;
  - controlled rhythm;
  - a good fit for early scoring feedback.

- Challenge Sentences:
  - longer, more natural phrases;
  - rhythm and continuity become more important;
  - useful for a stronger portfolio demo.

### Acceptance Criteria

- The start screen shows three distinct practice categories.
- Each category contains multiple unique lessons.
- `Next` and `Prev` switch lessons without slow rebuilds.
- Progress survives restarts.
- Lesson content no longer collapses into one repeated placeholder experience.

### Suggested Files

- `Fluent-Echo/Assets/_FluentEcho/Runtime/Data`
- `Fluent-Echo/Assets/_FluentEcho/Runtime/Presentation/FluentEchoPresenter.cs`
- `Fluent-Echo/Assets/_FluentEcho/Runtime/Services/LessonProgressRepository.cs`
- `Fluent-Echo/Assets/_FluentEcho/Editor/FluentEchoPrototypeBuilder.cs`
- `Fluent-Echo/Assets/_FluentEcho/Demo/Data`

### Verify After Sprint

- Confirm the category screen shows three paths.
- Confirm each path has its own lesson list.
- Confirm progress is stored and restored correctly.
- Confirm lesson switching does not block the mic or scoring flow.

## Sprint 4 - Portfolio Case Study Polish

### Objective

Prepare the project to be shown confidently in a portfolio.
This sprint is about presentation, clarity, and proof of engineering decisions.

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

- `Fluent-Echo/Assets/_FluentEcho/Docs/MB_ARCHITECTURE.md`
- `Fluent-Echo/Assets/_FluentEcho/Docs/MB_NEXT_STEPS.md`
- `Fluent-Echo/Assets/_FluentEcho/Docs/PROTOTYPE_OVERVIEW.md`
- Portfolio README or external case study later.

## Sprint 5 - Phoneme-Aware Scoring Roadmap

### Objective

Define the next scoring stage clearly enough that the project can move from heuristic practice scoring to a more honest phoneme-aware pipeline without changing the current local-only promise.

### Scope

- Describe the difference between heuristic transcript-based scoring and future phoneme-aware scoring.
- Define the new service boundary for a future `IPhonemeAlignmentService`.
- Keep Whisper as the transcript engine and do not blend it with pronunciation assessment.
- Describe the data needed for future alignment: transcript, timing, target words, and per-word match quality.
- Keep the current heuristic scorer as the MVP fallback until a stronger scorer exists.
- Document how the UI should keep showing honest labels while the new scorer is being introduced.

### Acceptance Criteria

- The roadmap clearly explains what the current scorer does and does not do.
- The future phoneme-aware path is documented as an additive service, not a rewrite of Whisper.
- No paid API is required for the documented next step.
- The docs give a reviewer a believable technical path from practice estimate to phoneme-aware assessment.

### Suggested Files

- `Fluent-Echo/Assets/_FluentEcho/Docs/MB_ARCHITECTURE.md`
- `Fluent-Echo/Assets/_FluentEcho/Docs/MB_NEXT_STEPS.md`
- `Fluent-Echo/Assets/_FluentEcho/Docs/CASE_STUDY_BRIEF.md`
- `Fluent-Echo/Assets/_FluentEcho/Docs/PORTFOLIO_DEMO_SCRIPT.md`

### Verify After Sprint

- Confirm the docs still describe the current heuristic scorer honestly.
- Confirm the future scorer is documented as optional and local-first.
- Confirm the reviewer can understand the next technical step without guessing.

## Sprint 6 - Final Portfolio Polish And Demo Packaging

### Objective

Turn the stable local speech-practice prototype into a clean portfolio package that can be shown with confidence.

### Scope

- Polish the case-study narrative so it fits a one-minute explanation.
- Prepare a short demo script that shows the full loop without technical detours.
- Make the visual hierarchy feel production-ready in the main practice, settings, and result panels.
- Keep the demo repeatable for recording.
- Leave a clear handoff for a future 3D character or lip-sync layer if the project grows later.

### Acceptance Criteria

- A reviewer can understand the project story quickly.
- The live demo flow is repeatable and stable.
- The final package keeps the local-only and no-paid-API promise explicit.
- The remaining future work is framed as roadmap, not as missing basics.

### Suggested Files

- `Fluent-Echo/Assets/_FluentEcho/Docs/CASE_STUDY_BRIEF.md`
- `Fluent-Echo/Assets/_FluentEcho/Docs/PORTFOLIO_DEMO_SCRIPT.md`
- `Fluent-Echo/Assets/_FluentEcho/Docs/MB_ARCHITECTURE.md`
- `Fluent-Echo/Assets/_FluentEcho/Docs/MB_NEXT_STEPS.md`

### Verify After Sprint

- Confirm the case study stays short and credible.
- Confirm the demo script reads like a clean walkthrough.
- Confirm the docs still match the implemented local-only architecture.

## Recommended Implementation Order

1. Sprint 0: scene-owned UI and layout lock-down.
2. Sprint 1: onboarding and friendly error states.
3. Sprint 2: honest local pronunciation estimate.
4. Sprint 3: lesson catalog, progress, and navigation.
5. Sprint 4: portfolio case-study polish.
6. Sprint 5: phoneme-aware scoring roadmap.
7. Sprint 6: final portfolio polish and demo packaging.

## Definition of Done for the Whole Roadmap

- The app can be launched by a new user without explanation.
- The user understands privacy and microphone requirements.
- Recording states never get stuck.
- Feedback is useful and honest.
- The result panel looks like a learning product, not a debug output.
- Architecture remains modular and documented.
- The project is ready to present as a local AI/Unity speech-practice vertical slice.
