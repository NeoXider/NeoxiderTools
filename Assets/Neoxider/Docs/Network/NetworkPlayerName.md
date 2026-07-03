# NetworkPlayerName

**What it is:** `Neo.Network.NetworkPlayerName` — a player nickname replicated to everyone. Names are trimmed and capped by `Max Length` on the server; the change command is rate-limited. Without Mirror it works locally (`SetLocalName` just fires the event).

**Usage:** put on the player object next to `NeoNetworkPlayer`; bind `OnNameChanged(string)` to a TMP label; call `SetLocalName(value)` from an input field / save on the local player. `PlayerName` falls back to `Default Name` while unset.
