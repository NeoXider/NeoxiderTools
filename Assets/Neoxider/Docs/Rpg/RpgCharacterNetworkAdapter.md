# RpgCharacterNetworkAdapter

Optional Mirror transport for `RpgCharacter`. It lives in the separate `Neo.Rpg.Network` assembly, so
projects can use the complete local RPG runtime without referencing `Neo.Network` or Mirror.

## Setup

1. Add `RpgCharacter` to the actor and configure resources, stats, effects, and persistence normally.
2. For a networked prefab, add `NetworkIdentity` and `RpgCharacterNetworkAdapter` on the same object.
3. Enable `RpgCharacter.isNetworked` and choose `AuthorityMode` / `AllowClientStateCommands` there.

The adapter routes remote mutations through one rate-limited command, validates the sender using the
character's authority policy, and replicates a serialized `RpgCharacterProfileData` snapshot. Late joiners
apply the same profile payload. Without an active Mirror session calls fall back to the local character API.

`RpgAttackNetworkAdapter` is the matching optional component for built-in attack input authority and
server-side spawning of projectiles carrying `NetworkIdentity`.

## Migration

Before this split, `RpgCharacter` inherited `NeoNetworkComponent`. Existing serialized values named
`isNetworked`, `_authorityMode`, and `_allowClientStateCommands` remain on `RpgCharacter`, but Unity cannot
automatically add the new adapter component. Add it to every networked prefab/scene object. Existing
UnityEvents targeting `RpgCharacter.Net*` stay valid because those methods are transport-neutral
compatibility wrappers. Purely local/offline characters keep their behavior without any adapter.
