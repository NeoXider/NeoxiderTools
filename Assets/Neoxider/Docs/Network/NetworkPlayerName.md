# NetworkPlayerName

**What it is:** `Neo.Network.NetworkPlayerName` — a player nickname replicated to everyone. Names are trimmed and capped by `Max Length` on the server; the change command is rate-limited. Without Mirror it works locally (`SetLocalName` just fires the event).

**How to use:**
1. Put it on the player object next to `NeoNetworkPlayer` (`Scripts/Network/Player/NetworkPlayerName.cs`) and tick **Is Networked**.
2. Bind `OnNameChanged(string)` to a TMP label.
3. Call `SetLocalName(value)` from an input field or your save system on the local player.

---

`PlayerName` falls back to `Default Name` while unset. The server ignores a rename command while its own
**Is Networked** flag is off (the component is local-only there), before the rename rate limit is spent.

```csharp
NetworkPlayerName nameTag = player.GetComponent<NetworkPlayerName>();
nameTag.OnNameChanged.AddListener(value => label.text = value);
nameTag.SetLocalName(PlayerPrefs.GetString("nickname", "Player"));
```
