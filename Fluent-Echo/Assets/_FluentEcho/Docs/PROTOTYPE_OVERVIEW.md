# Fluent Echo Prototype

## Зачем нужен прототип

Fluent Echo - автономный portfolio vertical slice для практики английской речи.
Проект показывает локальное распознавание голоса в Unity, отделённое от UI и
логики проверки ответа. Сейчас используется 2D-интерфейс внутри 3D URP-проекта,
поэтому позднее можно добавить полноценного говорящего персонажа и окружение.

## Что уже работает

- Готовая сцена: `Assets/_FluentEcho/Demo/Scenes/FluentEchoPrototype.unity`.
- Упражнение: произнести `The dog is big`.
- Локальное распознавание речи через Whisper без отправки записи в облако.
- Автоматическая остановка после паузы и вывод распознанного текста.
- Проверка обязательных слов, их порядка, альтернатив `big|large` и небольших
  ошибок распознавания.
- Подсветка правильно распознанных слов, результат и повторная попытка.
- Детерминированный Demo Mode для проверки интерфейса без микрофона.
- ScriptableObject-конфигурация урока:
  `Assets/_FluentEcho/Demo/Data/FirstLesson.asset`.
- Editor-тесты для основной логики сопоставления текста.

## Основные части

- `Runtime/Data` - данные упражнений.
- `Runtime/Domain` - независимые от Unity UI правила проверки и состояние сессии.
- `Runtime/Services` - общий интерфейс распознавания, Whisper и mock-реализации.
- `Runtime/Presentation` - Presenter, связывающий упражнение, сервис и View.
- `Runtime/Views` - Unity UI и карточки слов.
- `Runtime/Bootstrap` - сборка зависимостей сцены.
- `Editor/FluentEchoPrototypeBuilder.cs` - полная пересборка demo-сцены.
- `Tests/Editor` - тесты `SpeechAnswerMatcher`.

Whisper-модель находится в
`Assets/StreamingAssets/Whisper/ggml-tiny.en.bin`.

## Как проверить

1. Если Unity предложит перечитать изменённую сцену с диска, выберите `Reload`.
2. При необходимости выполните `Tools > Fluent Echo > Rebuild Prototype`.
3. Откройте `FluentEchoPrototype` и нажмите Play.
4. Нажмите `RUN DEMO ANSWER`.
5. Через короткую паузу должны появиться фраза `the dog is big`, зелёные карточки
   всех слов и статус `ANSWER ACCEPTED`.
6. Нажмите `NEW ATTEMPT`, чтобы сбросить упражнение.
7. Выключите `Use deterministic demo engine`.
8. Нажмите `START SPEAKING`, произнесите `The dog is big` и сделайте паузу.
9. Whisper должен вывести текст, подсветить слова и завершить упражнение.

Если Windows или Unity запросит разрешение на микрофон, его нужно выдать вручную.
Первое включение Whisper может занять несколько секунд, пока модель загружается.

Доменные тесты можно запустить через
`Window > General > Test Runner > EditMode > Run All`. Ожидаемый результат:
пять пройденных тестов в `SpeechAnswerMatcherTests`.

## Текущее ограничение

Прототип проверяет текст, полученный от speech-to-text, а не качество отдельных
фонем. Профессиональную оценку произношения, ударения и уверенности следует
добавлять отдельным pronunciation-scoring сервисом, не меняя UI и Presenter.

Подробный план развития находится в `IMPROVEMENT_ROADMAP.md`.
