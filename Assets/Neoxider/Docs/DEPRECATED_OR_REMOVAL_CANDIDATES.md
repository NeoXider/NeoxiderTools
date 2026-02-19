# Устаревшие компоненты и кандидаты на удаление

Файл ведётся для планирования удаления или снятия поддержки. Компоненты и типы, помеченные в коде атрибутом `[Obsolete]`, перечислены здесь с указанием замены.

## Таблица: старый → новый / статус

| Старый скрипт / компонент | Новый / замена | Статус | Примечание |
|---------------------------|----------------|--------|------------|
| TimeReward | CooldownReward | Obsolete | См. Bonus/TimeReward. |
| AiNavigation | Neo.NPC.NpcNavigation | Obsolete | См. Tools/Other. |
| HandLayoutType (enum) | CardLayoutType | Obsolete | В Cards/Config/HandLayoutType.cs; использовать enum CardLayoutType. |
| HandComponent.LegacyLayoutType (свойство) | HandComponent.LayoutType (CardLayoutType) | Obsolete | Устаревшее только свойство LegacyLayoutType; сам HandComponent актуален. |
| UIReady | SceneFlowController | Obsolete | Загрузка сцен, прогресс, Quit/Restart/Pause; см. Level/SceneFlowController.md. |

## Планируемое удаление

Кандидаты на полное удаление из кодовой базы решаются по мере релизов. На текущий момент удаление перечисленных выше типов не запланировано; они сохранены для обратной совместимости.
