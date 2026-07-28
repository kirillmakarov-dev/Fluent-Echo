# Fluent Echo Case Study Brief

## One-Minute Story

Fluent Echo is a private, local English speech-practice app built in Unity.
The user chooses a practice category, records a response, gets a local transcript from Whisper, and sees an honest pronunciation estimate with clear coaching feedback plus a visible note that true phoneme scoring is still on the roadmap.

The product story is intentionally simple:

- audio stays on the device;
- the app does not require paid APIs;
- the UI is explicit about what Whisper can and cannot do;
- the result flow feels like a learning app, not a debug console.

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
- progress persistence;
- a transparent pronunciation estimate pipeline;
- an explicit phoneme-roadmap note in the result flow;
- coach-style feedback and retry flow;
- separate settings, category, and result panels.

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
It uses transcript match, confidence, rhythm, and word-level signals.
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
4. Record a sample answer.
5. Show transcript, pronunciation estimate, and the phoneme-roadmap note.
6. Show retry or next mission.

## Future Direction

The next natural step is a stronger scoring pipeline:

- better confidence interpretation;
- more honest feedback wording;
- eventually phoneme-aware scoring or a dedicated scorer service.

For the current portfolio phase, the important win is a stable local loop:

`record -> transcript -> pronunciation estimate -> feedback`
