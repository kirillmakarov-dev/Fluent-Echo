# Fluent Echo Next Steps Brief

This brief captures the practical path from the current prototype to a fuller portfolio-grade application.

## What is already in place

- Local microphone capture with device selection.
- Whisper transcription with quality profiles.
- Lesson navigation across multiple exercises.
- Progress persistence.
- A heuristic pronunciation score MVP.
- Editor-safe CPU fallback for Whisper.

## Recommended next steps

### 1. Harden the transcription pipeline

- Add visible input-level feedback so the user knows the microphone is active.
- Keep refining cancellation and restart behavior.
- Validate that switching lessons, mic devices, and Whisper profiles remains stable across repeated attempts.

### 2. Expand the exercise catalog

- Add more lesson types beyond the sentence-repetition flow.
- Mix simple phrase prompts with response prompts and description prompts.
- Keep content in ScriptableObjects so the catalog remains data-driven.

### 3. Improve progress tracking

- Store best score, best transcript, and per-lesson attempt history.
- Show a clearer results summary when a lesson is cleared.
- Add lightweight progression rules for unlocking or sequencing lessons.

### 4. Replace heuristic scoring with true pronunciation assessment

- Keep the current heuristic scorer as a fallback or debug aid.
- Replace it with a phoneme-aware or alignment-based scoring path.
- Return separate feedback for timing, missing sounds, and difficult words.

### 5. Add tests around the architecture

- Cover presenter flows for success, retry, cancellation, and lesson switching.
- Cover scoring edge cases, especially empty input and partial matches.
- Cover persistence behavior so progress state does not regress.

### 6. Polish the portfolio presentation

- Replace placeholders with a real 3D character or a more polished presentation layer.
- Add lip sync and facial reaction states.
- Turn one lesson into a demo-quality showcase scene with strong visual hierarchy.

## Implementation order

1. Stabilize the current interaction loop.
2. Expand the catalog.
3. Improve progress.
4. Swap in real pronunciation scoring.
5. Finish portfolio polish and visual presentation.

## Working principle

Do not start with a custom speech model or complex 3D systems. Keep the loop simple first:

`record -> transcript -> score -> feedback -> next attempt`

That gives the project a reliable foundation before higher-fidelity features are added.
