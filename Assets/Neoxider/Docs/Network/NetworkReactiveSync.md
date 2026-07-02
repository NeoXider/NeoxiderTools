# NetworkReactiveSync

**Что это:** `Neo.Network.NetworkReactiveSync` — NoCode-репликация `ReactivePropertyFloat/Int/Bool` (инспекторный аналог `NetworkReactivePropertyBridge`, который требует ручного SyncVar-кода). Значение реплицируется, и все локальные байндинги (`TextMoney`, UI, UnityEvents) срабатывают у всех клиентов.

**Как использовать:** на объект с `NetworkIdentity`; указать компонент (например, `Money`) и имя reactive-поля (`CurrentMoney`), тип значения и направление (`ServerToClients` / `OwnerToServer`). `Sync Interval` ≥ 0.1 с. Без Mirror компонент бездействует — реактивка работает локально как обычно.
