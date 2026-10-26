# Native dependency metadata migration

Status: **COMPLETE for six metadata conversions and root copyback; full Neo verification pending.** This is asset maintenance, not a test/build pass for the library.

The first frozen Unity 6.6 import rejected six existing Demigiant PluginImporter v1 records. Root and the independent reviewer rejected a manual schema-number change because importer platform semantics must be preserved. Original inputs came from snapshot `final-20261023T230131Z`, manifest SHA-256 `0e433bf9848fe86066661c9209ebc955ed75a285e73d1e0a21f458a925e93260`.

After all product/helper writers stopped, root issued a separate metadata-only authorization. The original Unity operator used a new minimal ordinary Unity 6000.3.14f1 project containing only the six original DLL/meta pairs and the reviewed helper, with UGUI 2.0 and native UI/IMGUI dependencies. It contained no Neo runtime, tests, samples or alternative test driver. Unique company/product were assigned before first launch. Main and frozen inputs were untouched during native execution.

Owned PID 27964 ran 2026-10-23 23:42:56–23:43:26 UTC and exited 0, without timeout or cleanup failure. Before any mutation, the helper captured all six original native GUIDs, binary/meta hashes, actual SDK settings and complete native importer serialization. Only exact metadata ForceReserializeAssets and addressed SaveAndReimport were called. All six disk schemas became 3; every recorded setting/full native JSON, GUID and DLL hash remained equal. Raw per-platform getters are preserved alongside Any flags; they do not certify player builds.

| DLL below `Assets/Plugins/Demigiant/` | Retained GUID | Native metadata SHA-256 |
| --- | --- | --- |
| DemiLib/Core/DemiLib.dll | `54be29b67d0d29a478da2c6e5c62f091` | `774520c9d10ec2d2d1ba89a80e11311cf2e191327d5b05808913e008174e2129` |
| DemiLib/Core/Editor/DemiEditor.dll | `202f9ddaf2c1a8a429504f7f3cd7b84f` | `9daa0842adafd8a08f263d3966f99b5b52cb7ab80ff4cc8f8af71aaa50d45b84` |
| DOTween/DOTween.dll | `a811bde74b26b53498b4f6d872b09b6d` | `29ec19d47fbd4c3d63a7056d74d2320e81230ee70e34e3455727b7802a4446c5` |
| DOTween/Editor/DOTweenEditor.dll | `45d5034162d6cf04dbe46da84fc7d074` | `2e1af14ad8df685e4f70a7be09e53405f8a87fd90172c7f983dcb7aea181524f` |
| DOTweenPro/DOTweenPro.dll | `aa0b1eebb5db27a419fa4564bbe5c9c5` | `1cd2fc0ffa4032c1cd28feec99d2b97fa1df4a693b3df5070eea688cf365876e` |
| DOTweenPro/Editor/DOTweenProEditor.dll | `a6402d4311c862b4eb1325590d6466af` | `a3ac4f96838ffa4479d4c81bc482be972111a2e48e4e4bfe0e18b5368bd673a5` |

Evidence is retained under `Local/Audits/assembly-2026-10-23/unity/maintenance/a9e61ad7038f41c3bc6fc350db23cb51/`: native receipt SHA `3a85e65c5736e8deb86abc3b28e2e02b8b1b9c713dca7b8adea58c302778d727`, Editor log SHA `6dc316ade94a8f7a61f83d36bd6cedc879db1ba36b0442ea08b83f128cd0499a`, job SHA `343ba84b88fb79ae0f5e888f1701c8d51d460ed9258442e1170b080c6f92f166`. The full log also discloses account-token and native shutdown diagnostics; the helper's scoped console-error list is empty.

After the owned Editor and children exited, root independently checked all six before/after objects, GUIDs, DLL hashes, native output/meta hashes and original main hashes, then copied only the six `.meta` files at 23:47:12 UTC. Exact before/after hashes are in `Local/Audits/assembly-2026-10-23/native-plugin-metadata-copyback.json`. No DLL, helper, project setting, cache or generated settings asset was copied into the product. Final package checks and genuine full Unity 6.3/6.6 compile/discovery/runtime acceptance require the new source snapshot.
