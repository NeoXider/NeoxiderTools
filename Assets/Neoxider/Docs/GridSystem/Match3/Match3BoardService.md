# Match3BoardService

**Что это:** runtime-сервис для Match-3 на базе FieldGenerator: инициализация, проверка свапа, поиск и снос совпадений, обрушивание и добор. События по фазам и результату. Пространство имён `Neo.GridSystem.Match3`, файл `Scripts/GridSystem/Match3/Match3BoardService.cs`.

**Как использовать:** Add Component на объект с FieldGenerator; настроить минимальную длину совпадения и автогенерацию; вызывать свап и обновление из кода или UI. Подписаться на события для подсчёта очков и анимаций.

---

## См. также

- [FieldGenerator](../FieldGenerator.md)
