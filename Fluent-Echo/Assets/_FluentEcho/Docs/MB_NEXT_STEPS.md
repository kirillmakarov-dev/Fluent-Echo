# Fluent Echo Next Steps Brief

This brief captures the practical path from the current prototype to a fuller portfolio-grade application.

## What is already in place

- Local microphone capture with device selection.
- Whisper transcription with warm-up and quality profiles.
- Lesson navigation across multiple exercises.
- A multi-category lesson catalog.
- Progress persistence.
- A heuristic pronunciation estimate MVP.
- A visible phoneme-roadmap note in the scoring flow.
- Editor-safe CPU fallback for Whisper.
- Separate scene-owned panels for settings and result feedback.

## Recommended next steps

### Current sprint roadmap

Use `SPRINT_ROADMAP_LOCAL_SCORING_ONBOARDING.md` as the current implementation plan.
It now breaks the work into:

- scene-owned UI and layout lock-down;
- onboarding and friendly error states;
- honest local pronunciation estimate;
- lesson catalog, progress, and navigation;
- portfolio case-study polish.

The plan intentionally avoids paid APIs and keeps the scoring pipeline local and transparent.

### 1. Keep the interaction loop stable

- Preserve scene-owned UI positions.
- Keep microphone selection visible and editable in the scene.
- Keep Whisper profile selection visible and editable in the scene.
- Keep settings separate from the lesson flow.
- Keep the result panel separate from the practice panel.
- Validate that switching lessons, mic devices, and Whisper profiles remains stable across repeated attempts.

### 2. Improve progress tracking

- Store best score, best transcript, and per-lesson attempt history.
- Show a clearer results summary when a lesson is cleared.
- Add lightweight progression rules for unlocking or sequencing lessons.

### 3. Replace heuristic scoring with true pronunciation assessment

- Keep the current heuristic scorer as a fallback or debug aid.
- Replace it with a phoneme-aware or alignment-based scoring path.
- Return separate feedback for timing, missing sounds, and difficult words.

### 4. Add tests around the architecture

- Cover presenter flows for success, retry, cancellation, and lesson switching.
- Cover scoring edge cases, especially empty input and partial matches.
- Cover persistence behavior so progress state does not regress.

### 5. Polish the portfolio presentation

- Replace placeholders with a real 3D character or a more polished presentation layer.
- Add lip sync and facial reaction states.
- Turn one lesson into a demo-quality showcase scene with strong visual hierarchy.

### 6. Keep the portfolio story readable

- Keep `MB_ARCHITECTURE.md` aligned with the current onboarding, honest scoring, and panel layout.
- Keep the score UI honest by naming the current heuristic estimate and the future phoneme roadmap separately.
- Keep `CASE_STUDY_BRIEF.md` as the short portfolio-facing summary.
- Keep `PORTFOLIO_DEMO_SCRIPT.md` as the live presentation walkthrough.
- Make sure the case-study story fits a one-minute explanation: privacy, local Whisper, transparent estimate, coach-style feedback.
- Keep the docs focused on what a reviewer needs to see first, not on internal implementation details.

## Implementation order

1. Stabilize the current interaction loop.
2. Improve progress.
3. Swap in real pronunciation scoring.
4. Finish portfolio polish and visual presentation.
5. Keep the docs and case-study narrative aligned with the current build.

## Working principle

Do not start with a custom speech model or complex 3D systems. Keep the loop simple first:

`record -> transcript -> score -> feedback -> next attempt`

That gives the project a reliable foundation before higher-fidelity features are added.
