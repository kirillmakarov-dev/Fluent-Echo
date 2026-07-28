# Fluent Echo Case Study Brief

## One-Minute Story

Fluent Echo is a private, local English speech-practice app built in Unity.
The user starts on a category screen, chooses Words, Short Sentences, or Challenge Sentences, records a response, gets a local transcript from Whisper, and sees an honest practice score with clear coaching feedback.
The result panel separates practice score, confidence, word match, rhythm, and a focused next step so the feedback reads like a coach, not a log.
On the final lesson, the panel keeps the same structure but only shows the actions that make sense for that lesson, so the flow stays clean and easy to present.
For internal demos, the inspector can optionally enable an `Alignment preview` block, but the public story stays honest: the app is still not claiming true phoneme scoring yet.

The product story is intentionally simple:

- audio stays on the device;
- the app does not require paid APIs;
- the UI is explicit about what Whisper can and cannot do;
- the result flow feels like a learning app, not a debug console.
- demo preview modes stay clearly separated from the real user-facing scoring claim.

## Problem

Most speech-practice demos are either:

- cloud-dependent;
- too technical for a reviewer;
- or they overclaim pronunciation accuracy.

This project solves that by keeping the whole feedback loop local and transparent.

## Constraints

- no paid speech APIs;
- no cloud transcription;
- inspector-driven scene layout;
- a portfolio-friendly visual hierarchy;
- a future path to better scoring without pretending it already exists.

## What It Demonstrates

- local microphone capture;
- local Whisper transcription;
- category-first lesson navigation;
- three distinct practice paths with multiple lessons each;
- progress persistence with best / last match-quality breakdowns;
- a transparent pronunciation estimate pipeline;
- a clear practice-score result panel with retry, close, and next-mission actions;
- a final-lesson result panel that hides unnecessary navigation and keeps the flow focused;
- coach-style feedback with visible word match, rhythm, confidence, and focus-next signals;
- separate settings, category, and result panels.
- an optional inspector-only alignment preview for demos, which is explicitly framed as preview, not phoneme assessment.

## Architecture in One Glance

`Microphone -> Whisper transcript -> answer matching -> pronunciation estimate -> user feedback -> next attempt`

The code is split into:

- Data: lesson and catalog ScriptableObjects;
- Domain: answer matching and session state;
- Services: speech and scoring infrastructure;
- Presentation: the orchestration layer;
- Views: the scene-owned UI.

## Limitations

The current scoring is a heuristic estimate.
It uses transcript match, confidence, rhythm, and word match signals.
It does not perform true phoneme-level pronunciation assessment yet.
The UI says that plainly instead of implying the feature already exists.

That limitation is shown honestly in the UI and in the roadmap.

## Why It Works for Portfolio

- the architecture is cleanly separated;
- the demo flow is repeatable;
- the app is privacy-first;
- the UI is polished enough to present;
- the roadmap is realistic and clearly staged.

## Video-Ready Flow

1. Open the app.
2. Show the privacy/onboarding message.
3. Pick a practice category.
4. Pick a lesson inside that category.
5. Record a sample answer.
6. Show transcript, practice score, confidence, word match, rhythm, and focus-next feedback.
7. Show retry, close, or next mission.

## Future Direction

The next natural step is a stronger scoring pipeline:

- better confidence interpretation;
- more honest feedback wording;
- eventually phoneme-aware scoring or a dedicated scorer service.

The current build already stays honest by presenting transcript, practice score, confidence, word match, rhythm, and focus-next feedback as separate pieces of information.
When preview mode is enabled for a demo, it stays clearly labeled as preview so the reviewer can see the architectural path without being misled.

For the current portfolio phase, the important win is a stable local loop:

`record -> transcript -> pronunciation estimate -> feedback`
