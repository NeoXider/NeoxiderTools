# NetworkPlayerName

**Что это:** `Neo.Network.NetworkPlayerName` — ник игрока, реплицируемый всем. Имя тримится и ограничивается `Max Length` на сервере; команда смены — с рейт-лимитом. Без Mirror работает локально (`SetLocalName` просто вызывает событие).

**Как использовать:** на объект игрока рядом с `NeoNetworkPlayer`; `OnNameChanged(string)` → TMP-лейбл; `SetLocalName(value)` — из поля ввода/сейва на локальном игроке. `PlayerName` возвращает `Default Name`, пока имя не задано.
