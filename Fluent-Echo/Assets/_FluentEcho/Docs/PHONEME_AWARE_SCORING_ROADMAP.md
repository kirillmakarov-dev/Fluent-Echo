# Fluent Echo Phoneme-Aware Scoring Roadmap

This document describes the next scoring step for Fluent Echo after the current heuristic practice estimate.
It keeps the current local-only promise, avoids paid APIs, and treats Whisper as the transcript engine rather than the pronunciation scorer.

## Purpose

Move from a transparent transcript-based estimate to a more honest phoneme-aware scoring path without pretending Whisper can do that job by itself.

## Current State

The project currently has:

- local Whisper transcription;
- heuristic pronunciation scoring;
- transcript match, confidence, rhythm, and match-quality breakdowns;
- persisted progress with best and last match-quality summaries;
- result copy that explicitly says the app is not performing true phoneme scoring yet.

This is already useful for MVP feedback and portfolio demonstration.
The next step is to add a separate scoring layer, not to rewrite Whisper.

## Guiding Principles

- Keep audio local.
- Do not use paid APIs.
- Keep `ISpeechRecognitionService` responsible for transcript generation only.
- Keep `IPronunciationScoringService` responsible for heuristic estimate logic until a better scorer exists.
- Add future phoneme-aware logic as an optional service boundary.
- Use a `NoOpPhonemeAlignmentService` as the explicit default until a real scorer is wired.
- Allow the bootstrap inspector to switch into a preview alignment mode for demos and testing.
- Keep the UI honest about what is estimated and what is actually measured.
- Keep roadmap text out of post-attempt result details unless a real alignment signal exists.
- Keep the preview block compact, labeled as `Alignment preview`, and clearly distinct from true phoneme scoring.

## Proposed Service Boundary

Add a future scoring contract such as:

- `IPhonemeAlignmentService`
  - aligns the spoken audio or transcript-derived phoneme sequence with the expected phrase;
  - returns alignment confidence and mismatch categories;
  - stays separate from transcript generation.

Optional follow-up helpers:

- `IWordConfidenceProvider`
  - exposes word-level confidence if a local model can provide it;
- `IPronunciationEvidenceCollector`
  - collects transcript, timing, target words, and any alignment features in one place.

## Data Inputs

A phoneme-aware scorer should receive:

- the target phrase or lesson text;
- the Whisper transcript;
- recording duration;
- recognized word order;
- matched / missing / extra words;
- approximate / fuzzy match information;
- optional alignment artifacts when available.

The system should still work when phoneme artifacts are unavailable.
That means the heuristic scorer remains the fallback.

## Scoring Outputs

The future scorer should produce a result object that can describe:

- overall score;
- word-level confidence;
- phoneme or segment alignment details;
- missing or weak sounds;
- timing quality;
- a short coaching summary;
- a user-facing confidence band.

The UI should continue to separate:

- what we heard;
- what matched exactly;
- what matched approximately;
- what was missed;
- what to try next.

## UI Expectations

The result panel should not claim phoneme precision until the scorer actually has it.

Until then:

- the result panel keeps showing `Practice Score`;
- the breakdown keeps showing transcript, confidence, rhythm, and match quality;
- `PhonemeDetails` stays hidden or marked unavailable;
- the coaching copy stays honest about the current limitation.

When a future phoneme-aware scorer is introduced:

- it can feed the same coach-style result panel;
- the label can shift from estimate wording to a more specific pronunciation assessment wording;
- the old heuristic scorer can remain as a debug or fallback path.

## Roadmap Stages

### Stage 1 - Alignment Research

- Define the exact input contract for a phoneme-aware scorer.
- Decide whether the alignment uses transcript-level, audio-level, or hybrid features.
- Identify what can be extracted locally without paid APIs.

### Stage 2 - Scoring Contract

- Add the future service interface.
- Add a result model that can carry phoneme or segment-level details.
- Keep the existing heuristic scorer untouched as the default.
- Keep the explicit no-op fallback available so the build stays predictable before the future scorer ships.
- Keep the preview alignment mode separate from true phoneme scoring so the demo story stays honest.

### Stage 3 - Incremental Integration

- Wire the future scorer behind a feature flag or profile choice.
- Keep the current result panel layout stable.
- Keep the result panel clean by only showing alignment evidence when it is real, not as a permanent placeholder.
- Compare heuristic and phoneme-aware feedback on the same lesson flow.

### Stage 4 - UI Honest Mode

- Update copy so the app clearly states which scorer is active.
- Show phoneme details only when they exist.
- Keep the fallback heuristic wording available.

### Stage 5 - Verification

- Add tests for empty input, partial matches, approximate matches, and alignment failures.
- Add tests that the heuristic fallback still works.
- Add tests that the UI copy does not overclaim phoneme precision.

## Acceptance Criteria

- The project has a documented local-only path toward phoneme-aware scoring.
- Whisper remains transcript-only.
- The heuristic scorer remains usable until the new scorer exists.
- The UI never claims capabilities that are not implemented.
- The roadmap gives a reviewer a believable engineering path from estimate to assessment.

## Out Of Scope For Now

- Training a new speech model.
- Integrating paid pronunciation APIs.
- Replacing the current heuristic scorer before a new scorer is actually ready.
- Adding complex 3D character work before the scoring architecture is clear.

## Next Implementation Order

1. Keep the current heuristic pipeline stable.
2. Define the phoneme-aware scoring service boundary.
3. Add a result model that can carry alignment details.
4. Integrate the new scorer behind a local-only fallback strategy.
5. Update the UI copy only when the new scorer is truly available.
