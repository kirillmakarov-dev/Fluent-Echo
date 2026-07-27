# Fluent Echo Next Steps

This brief captures the practical path from the current prototype to a portfolio-ready app.

## 1. Finish the lesson loop

- Polish lesson navigation and make the current lesson state obvious in the UI.
- Keep `Previous` / `Next` behavior consistent when recording, preparing, retrying, or switching modes.
- Add a clear end-of-lesson state and a clean way to replay or continue.

## 2. Expand the exercise catalog

- Add more lesson types beyond the current sentence-repetition prompts.
- Keep 2-3 short exercises per content group so the catalog feels like a real learning flow.
- Add richer lesson metadata if the scene needs categories, difficulty, or teaching goals.

## 3. Replace transcript matching with scoring

- Introduce a dedicated pronunciation scoring layer.
- Keep transcript recognition and pronunciation scoring separate.
- Expose a score summary, per-word feedback, and targeted guidance in the UI.

## 4. Harden cancellation and state transitions

- Make prepare, listen, analyze, and retry flows explicitly cancelable.
- Prevent stale async work from writing status back into the UI after the user has already moved on.
- Add tests for rapid mode switching, lesson switching, and interrupted recordings.

## 5. Improve feedback and learning value

- Show a better breakdown of what the system heard and what the learner should fix.
- Add a result history or progress screen per lesson.
- Store more than the best transcript if the app needs session review.

## 6. Build the portfolio layer

- Replace temporary visuals with a stronger character and a more intentional scene.
- Add lip sync or light facial animation once the core flow is stable.
- Prepare a short demo capture that explains the product in under one minute.

## 7. Finalize release quality

- Add playmode and editor coverage for the core flow.
- Verify microphone permissions and model loading on the target machine.
- Document the final build process, model assets, and update path.

## Recommended Order

1. Finish the lesson loop.
2. Harden cancellation and state transitions.
3. Expand the exercise catalog.
4. Add pronunciation scoring.
5. Polish visuals and presentation.
6. Wrap with tests and release documentation.

