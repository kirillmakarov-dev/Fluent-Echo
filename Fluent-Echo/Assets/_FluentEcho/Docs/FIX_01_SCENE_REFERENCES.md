# Fix 01 - Scene References

## Симптом

При запуске `FluentEchoPrototype` появлялась ошибка:

`[FluentEchoBootstrap] Required references are missing.`

Интерфейс открывался, но упражнение не запускалось и оставалось в состоянии
`Loading speech engine...`.

## Причина

После `Tools > Fluent Echo > Rebuild Prototype` Unity не сохранял две внешние
ссылки, записанные через `SerializedObject`:

- `FluentEchoBootstrap.exercise`;
- `FluentEchoView.wordChipPrefab`.

## Исправление

- Текущая сцена снова подключена к `FirstLesson.asset` и `WordChip.prefab`.
- Runtime-компоненты получили явные методы конфигурации.
- Builder теперь назначает внешние зависимости напрямую, помечает компоненты
  dirty и сохраняет ассеты до создания сцены.
- Сообщение Bootstrap теперь перечисляет имена отсутствующих ссылок.

Проверка сериализации показала `0` отсутствующих обязательных ссылок.
Runtime, Editor и Tests assemblies собираются с `0 errors` и `0 warnings`.

## Повторная проверка

1. Остановить Play Mode.
2. Дождаться завершения компиляции Unity.
3. Если Unity сообщает, что сцена изменена на диске, выбрать `Reload`.
4. Запустить Play Mode снова.
5. Нажать `RUN DEMO ANSWER`.

Повторно выполнять `Rebuild Prototype` для этой проверки не требуется.
