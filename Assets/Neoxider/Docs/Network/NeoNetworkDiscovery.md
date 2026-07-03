# NeoNetworkDiscovery

**Purpose:** Mirror LAN discovery helper for lobby-oriented projects.

`NeoNetworkDiscovery` lives under `Scripts/Network/Lobby/NeoNetworkDiscovery.cs`. Use it when a local network lobby needs host discovery without making game rules depend on networking.

## Usage Notes

- Keep discovery as an optional scene/network layer.
- Route gameplay state through explicit network components after a connection is established.
- For the broader lobby flow, see [Lobby](./Lobby.md).

## See Also

- [NeoLobbyManager](./NeoLobbyManager.md)
- [NeoLobbyPlayer](./NeoLobbyPlayer.md)
- [Network README](./README.md)
