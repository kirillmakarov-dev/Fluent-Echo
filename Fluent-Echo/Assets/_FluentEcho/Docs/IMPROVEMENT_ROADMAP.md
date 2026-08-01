# Fluent Echo - Improvement Roadmap

## Текущее состояние

Прототип использует локальную английскую модель Whisper `ggml-tiny.en.bin`.
Она преобразует речь в текст, после чего `SpeechAnswerMatcher` проверяет
обязательные слова, их порядок, альтернативы и небольшие ошибки распознавания.

Это уже демонстрирует архитектуру speech-to-text упражнения, но пока не является
полноценной системой оценки английского произношения.

## Важное ограничение

Whisper отвечает на вопрос: «Какие слова, вероятнее всего, произнёс игрок?»

Whisper не даёт надёжную оценку:

- правильности отдельных звуков и фонем;
- ударения;
- интонации;
- беглости речи;
- качества произношения конкретного слова.

Поэтому улучшение проекта не должно ограничиваться заменой `tiny` на более
крупную Whisper-модель. Для pronunciation scoring потребуется отдельный сервис.

## Этап 1 - Надёжный микрофонный поток

- Добавить список доступных микрофонов и выбор устройства.
- Сохранять выбранное устройство между запусками.
- Показывать уровень входного сигнала и индикатор обнаружения голоса.
- Различать состояния `Loading`, `Warming Up`, `Listening`, `Analyzing`,
  `Success`, `Retry` и `Error`.
- Добавить понятные сообщения для отсутствующего устройства и запрещённого
  доступа к микрофону.
- Добавить минимальную и максимальную продолжительность записи.
- Запретить повторное нажатие кнопок во время незавершённого inference.
- Добавить таймаут и корректную отмену асинхронной операции.

## Этап 2 - Улучшение Whisper

- Выполнять прогрев модели при открытии сцены, чтобы первая попытка не занимала
  значительно больше времени.
- Вынести путь модели, язык, GPU, VAD и streaming parameters в отдельный
  `WhisperSettingsSO`.
- Добавить профили качества:
  - `Fast` - `tiny.en`;
  - `Balanced` - `base.en`;
  - `Accurate` - `small.en`.
- Измерять время загрузки, длительность inference и real-time factor.
- Сравнить CPU и GPU режимы на целевых компьютерах.
- Добавить fallback на CPU, если GPU initialization завершился ошибкой.
- Не хранить модель в обычной сцене или prefab; она должна загружаться через
  единый speech infrastructure layer.

Переобучать Whisper для следующего этапа не требуется. Сначала нужно сравнить
готовые английские модели и выбрать лучший баланс скорости и точности.

## Этап 3 - Расширение упражнений

- Создать каталог `SpeechExerciseSO`, а не одну тестовую фразу.
- Добавить категории, уровень сложности и учебную цель.
- Поддержать несколько правильных фраз и вариантов слов.
- Добавить эталонную аудиозапись носителя языка.
- Добавить изображения или 3D-ситуации, которые задают контекст фразы.
- Добавить последовательность упражнений и экран результатов урока.
- Сохранять прогресс, количество попыток и лучший результат.
- Добавить повторение проблемных слов.
- Поддержать задания:
  - повторение готовой фразы;
  - ответ на вопрос;
  - описание изображения;
  - диалог с виртуальным преподавателем.

## Этап 4 - Настоящая оценка произношения

Добавить отдельный `IPronunciationScoringService`, не смешивая его с
`ISpeechRecognitionService`.

Он должен возвращать:

- общий score попытки;
- score каждого слова;
- score отдельных фонем;
- пропущенные и добавленные слова;
- темп речи;
- длину пауз;
- рекомендации по исправлению.

Возможные реализации:

- локальная acoustic/phoneme model;
- forced alignment между эталонной фразой и записью;
- внешний pronunciation-assessment API;
- гибрид: локальный Whisper для текста и отдельный scorer для фонетики.

UI должен показывать отдельно:

- `What we heard` - результат Whisper;
- `Pronunciation score` - качество произношения;
- конкретную подсказку, например: `Focus on the /th/ sound`.

## Этап 5 - Архитектура и тестирование

- Добавить state machine вместо распределённых boolean-флагов.
- Ввести cancellation tokens для загрузки, записи и анализа.
- Покрыть Presenter тестами через mock speech и pronunciation services.
- Добавить тесты ошибок модели, микрофона и отмены операции.
- Добавить PlayMode-тест полного сценария упражнения.
- Собирать диагностический отчёт без сохранения пользовательской аудиозаписи.
- Проверить освобождение native Whisper resources при смене сцены.
- Добавить dependency composition root для выбора локальных и облачных сервисов.

## Этап 6 - Portfolio-версия

- Заменить capsule placeholder на говорящего 3D-персонажа.
- Добавить lip sync и facial expressions.
- Синхронизировать эталонное аудио с анимацией персонажа.
- Добавить одну визуально завершённую учебную сцену.
- Показать минимум три разных типа speech-упражнений.
- Добавить экран результатов с понятной расшифровкой score.
- Подготовить короткое видео:
  `prompt -> recording -> local transcript -> pronunciation feedback`.
- В case study объяснить разделение Whisper, scoring, domain logic и UI.

## Рекомендуемый порядок

1. Стабилизировать микрофон и добавить состояния загрузки.
2. Сделать прогрев и профили Whisper.
3. Добавить 5-10 упражнений через ScriptableObjects.
4. Добавить сохранение прогресса.
5. Подключить pronunciation scoring.
6. После стабилизации логики добавлять 3D-персонажа и portfolio-polish.

Не стоит начинать с обучения собственной speech model или сложного 3D. Сначала
нужно получить быстрый, измеримый и повторяемый цикл:
`record -> transcript -> score -> useful feedback`.

## Stage 7 - Optional External Integrations

The current prototype does not require cloud APIs.
That is still the right default for a portfolio-ready local-first version.

However, if Fluent Echo evolves into a stronger production-oriented application,
the following optional integrations are the most reasonable next step.

### A. Cloud Pronunciation Scoring

Goal:

- replace or complement the current heuristic pronunciation estimate with a more objective scoring layer.

Good fit:

- Azure AI Speech Pronunciation Assessment

Recommended role:

- keep local Whisper for transcript generation and private practice mode;
- add cloud scoring as a separate optional service;
- keep it behind `IPronunciationScoringService` so the presenter and UI do not need a rewrite.

### B. Cloud Speech Recognition Fallback

Goal:

- improve recognition quality when the local device, microphone, or lesson type is too difficult for the on-device path.

Good fits:

- Google Cloud Speech-to-Text with phrase adaptation
- Deepgram Speech-to-Text with keyword boosting

Recommended role:

- use only as an optional fallback or benchmark mode;
- keep the local-first experience as the default product identity.

### C. Dynamic Reference Audio

Goal:

- generate native-sounding lesson playback without manually recording every lesson.

Good fit:

- a cloud TTS service, or a local/offline TTS pipeline depending on the privacy target

Recommended role:

- keep the current asset-based `Reference Audio` flow for stable portfolio demos;
- add optional generated lesson audio only when the content catalog grows large enough to justify it.

### D. Product Rules For Any External API

If external APIs are added later, the project should preserve these rules:

- local Whisper stays the default transcription path;
- external STT is optional, not mandatory;
- external pronunciation scoring is separate from transcript recognition;
- the UI must clearly tell the user whether audio stays local or is sent to a cloud service;
- diagnostics and analytics must avoid storing raw learner audio by default.

This keeps the architecture clean and preserves the current trust model of the prototype.
