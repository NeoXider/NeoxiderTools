# Модуль Network

## Назначение

Документация по сетевому слою: менеджер сети, owner filters, relay и синхронизация свойств.

## Компоненты и документация рядом

| Компонент / файл | Документация |
|------------------|--------------|
| `Scripts/Network/Core/NeoNetworkComponent.cs` | [NeoNetworkComponent](./NeoNetworkComponent.md) |
| `Scripts/Network/Core/NeoNetworkManager.cs` | [NeoNetworkManager](./NeoNetworkManager.md) |
| `Scripts/Network/Core/NetworkActionRelay.cs` | [NetworkActionRelay](./NetworkActionRelay.md) |
| `Scripts/Network/Core/NetworkContextActionRelay.cs` | [NetworkContextActionRelay](./NetworkContextActionRelay.md) |
| `Scripts/Network/Core/NetworkOwnerFilter.cs` | [NetworkOwnerFilter](./NetworkOwnerFilter.md) |
| `Scripts/Network/Core/NetworkPropertySync.cs` | [NetworkPropertySync](./NetworkPropertySync.md) |
| `Scripts/Network/Core/NetworkSingleton.cs` | [NetworkSingleton](./NetworkSingleton.md) |
| `Scripts/Network/Player/NeoNetworkPlayer.cs` | [NeoNetworkPlayer](./NeoNetworkPlayer.md) |
| `Scripts/Network/Spawner/NeoNetworkSpawner.cs` | [NeoNetworkSpawner](./NeoNetworkSpawner.md) |
| `Scripts/Network/Lobby/*.cs` | [Lobby](./Lobby.md), [NeoNetworkDiscovery](./NeoNetworkDiscovery.md), [NeoLobbyManager](./NeoLobbyManager.md), [NeoLobbyPlayer](./NeoLobbyPlayer.md) |

## Гайды

- [NoCode_Network_Spec](./NoCode_Network_Spec.md)
- [Multiplayer_Guide](./Multiplayer_Guide.md)

## Диагностика

Runtime `Log` и `Warning` в package-коде проходят через `NetworkDiagnostics` и выключены по умолчанию. Включайте `NetworkDiagnostics.RuntimeLogsEnabled` или `NetworkDiagnostics.RuntimeWarningsEnabled` только на время отладки; компонентные verbose-флаги (`Debug Lifecycle Log`, `Verbose Logging`) продолжают печатать явно включенную диагностику.

## Примечание

Некоторые страницы остаются RU-first и могут иметь дополнительный контент в корневом разделе.

- [English index](../../DocsEn/Network/README.md)
- [Russian module root](../../Docs/README.md)

