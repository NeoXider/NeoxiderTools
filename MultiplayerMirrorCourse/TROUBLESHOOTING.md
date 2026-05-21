# Быстрая диагностика Mirror course

Этот файл нужен для момента, когда "ничего не работает" и непонятно, в какой урок возвращаться.

---

## Сначала проверьте это

| Вопрос | Да/нет |
|--------|--------|
| Проверено не только в Host, но и отдельным Client? | |
| В сцене один активный `NetworkManager`? | |
| На `NetworkManager` нет `NetworkIdentity`? | |
| Player prefab назначен в `NetworkManager`? | |
| У player prefab есть `NetworkIdentity` на root? | |
| Dynamic prefab зарегистрирован как spawnable? | |
| Server log содержит `connectionId`? | |
| Client/server используют совместимую версию build/catalog/protocol? | |

---

## Симптомы

| Симптом | Вероятная причина | Урок |
|---------|-------------------|------|
| Client не подключается | Address, port, transport, firewall, wrong protocol. | 01, 16 |
| Работает только Host | Нет отдельного Client test, authority bug скрыт. | 01, 22 |
| Player не появляется | Не назначен Player Prefab или нет `NetworkIdentity`. | 01, 02 |
| Объект виден только серверу | Нет `NetworkServer.Spawn` или prefab не зарегистрирован. | 02 |
| `SyncVar` не приходит | State меняется не на server, object not spawned, `netId == 0`. | 04 |
| `Command called without authority` | Command вызван не с owned/local object. | 06 |
| UI меняет HP/валюту | UI стал источником правды вместо server state. | 04, 12 |
| Все игроки двигаются одним input | Нет `isLocalPlayer`/`isOwned` guard. | 03 |
| Remote movement дёргается | Send Rate/buffer/transport/loss не проверены. | 03, 10, 20 |
| Плохой игрок может спамить | Нет rate limit/cooldown/server validation. | 06, 09, 29 |
| WebGL не подключается | KCP-only path, нужен WebSocket/SimpleWeb/Multiplex. | 01, 16 |
| Dedicated ждёт кнопку | Запуск завязан на `NetworkManagerHUD`, нет bootstrap. | 14 |
| CI собирает не то | Wrong build target/subtarget/scenes/version. | 23 |
| После обновления Mirror сломался spawn/RPC | Не пройден upgrade smoke matrix. | 28 |
| Пуля RPG видна только серверу | Projectile создан обычным `Instantiate`, нет `NetworkServer.Spawn` или prefab не registered. | 02, 31 |
| XP начисляется только в Host | Reward вызывается локально, а не server-side death/reward flow. | 32, 33 |
| Клиент может поставить себе уровень или XP | Есть public Command для trusted state (`CmdAddXp`, `CmdSetLevel`) без server-only защиты. | 09, 32 |
| Reward за смерть выдался дважды | Death/reward flow не защищён server flag или выполняется на client и server. | 31, 33 |
| Перки не сохраняются после restart | Profile write не делает flush (`SaveProvider.Save()`/backend write). | 32 |
| Level-up reward пропущен при большом XP | Награды выдаются только за финальный уровень, нет цикла по crossed levels. | 32 |

---

## Минимальный лог, который нужен всегда

```text
[NET] start mode=server transport=KCP port=7777 scene=NetSandbox version=...
[NET] connect id=1 address=...
[NET] auth ok id=1 account=...
[NET] spawn player id=1 netId=...
[NET] command reject id=1 command=CmdTryBuy reason=not_enough_gold
[NET] disconnect id=1 reason=timeout duration=...
```

Не логируйте raw tokens, passwords, private keys и приватные персональные данные.

---

## Когда возвращаться назад

| Если ломается | Возвращайтесь |
|---------------|---------------|
| Подключение | `00_START_HERE`, `01`, `16` |
| Spawn | `02` |
| Movement | `03`, потом `10`, потом `20` |
| HP/score/state | `04`, потом `06` |
| Inventory/list data | `05`, `11` |
| UI | `12` |
| Release test | `15`, `22` |
| Dedicated/cloud | `14`, `23`, `25` |
| Security | `09`, `19`, `29` |
| RPG/Progression | `31`, `32`, `33` |
