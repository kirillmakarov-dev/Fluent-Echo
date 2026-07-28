# Fluent Echo Portfolio Demo Script

Use this as the live walkthrough when recording or presenting the prototype.

## Goal

Show the app as a polished local speech-practice product in under three minutes.
The demo should communicate privacy, clarity, and a stable learning loop.

## Setup

- Open `FluentEchoPrototype.unity`.
- Make sure the scene has loaded fully.
- Confirm the microphone permission is available if you want a live recording.
- If you want a guaranteed safe run, switch to Demo Mode first.

## Walkthrough

### 1. Open With The Story

Say:

> Fluent Echo is a private local speech-practice app built in Unity. It listens on-device, uses local Whisper, and gives honest practice feedback without sending audio to the cloud.

### 2. Show The Categories

Point to the three practice paths:

- Words
- Short Sentences
- Challenge Sentences

Say:

> The app starts with three guided paths, so practice stays structured instead of feeling like a raw prompt list.

### 3. Show The Settings Panel

Open the settings panel and point to:

- microphone selection;
- Whisper profile selection;
- the fact that the panel is separate from the lesson flow.

Say:

> The important system settings are editable in the scene and stay visually separated from the exercise itself.

### 4. Run One Easy Demo

Use Demo Mode or start a real attempt.

Say:

> Here the app gets a transcript locally, then turns it into a transparent pronunciation estimate with clear confidence, word match, rhythm, and a focused next step.

Let the result panel appear.

### 5. Show The Result Panel

Highlight:

- practice score;
- confidence;
- word match;
- rhythm;
- focus next;
- `TRY AGAIN`, `NEXT MISSION`, and `CLOSE`.

Say:

> This is intentionally a learning flow, not a debug dump. It tells the user what happened and what to try next.

If you are on the final lesson, note that `NEXT MISSION` will not be shown, so the user can only close the result or try again.

If inspector preview mode is enabled for the demo build, point to the compact `Alignment preview` block and say:

> This is a demo-only preview of the future alignment boundary. It is intentionally labeled as preview, so the app stays honest about the fact that true phoneme scoring is not shipped yet.

### 6. Show Progress

Move to another lesson or category.

Say:

> Progress is persisted per lesson, and navigation across categories and exercises is designed to be deterministic.

### 7. Close With The Honest Limitation

Say:

> The current scorer is a heuristic estimate based on transcript quality, confidence, rhythm, and word match. The UI also says that true phoneme scoring is still a roadmap item, so the app stays honest about what it does today.

## Shot List

If you are recording video, capture these moments:

1. Landing screen / first launch notice.
2. Category screen.
3. Settings panel.
4. Start speaking or demo answer.
5. Result panel.
6. Next mission or category switch.

## What Not To Show

- Avoid scrolling through technical logs.
- Avoid focusing on temporary recovery scenes.
- Avoid talking about future scoring as if it is already implemented.

## One-Line Closing

> Fluent Echo is a privacy-first Unity speech practice prototype with a local transcription pipeline, a transparent pronunciation estimate, and a portfolio-ready presentation flow.
