# HTML verification evidence

[HtmlCtaStencil-20261102.zip](HtmlCtaStencil-20261102.zip) preserves the exact graphical PlayMode comparison: depth-zero capture run **2 FAIL**, corrected depth/stencil run **2 PASS**, full affected fixture **16 PASS**. All have zero skips; exits are 2/0/0. Actual corrected format is `D32_SFloat_S8_UInt`, with default clearing retained. Original paint/contrast/glyph/pointer assertions, scheduling and product styling are unchanged. Both desktop/mobile PNGs per run, XML/logs, captures metadata, prelaunch manifests and exact fixture sources are included. The other 331 unchanged input files are deduplicated to the Initial archive below and verified by hash. Root checked CRCs and all 39 payload hashes. This establishes the sampled-panel fix and synthetic event scenarios, not physical input, ordinary Game View, video or browser parity. ZIP SHA-256: `b3f06885ec051369509e01b07b87dd0e05ca691f6acbe8b3e38feafe1c35a9f4`.

[HtmlUnityNativeInitial-20261102.zip](HtmlUnityNativeInitial-20261102.zip) preserves three actual Unity 6000.3.14f1 D3D11 Mono Editor batches: first EditMode **79 PASS / 9 FAIL** (88 executed, including Web 65/65); corrected/affected EditMode **301/301 PASS**; and exactly two CTA PlayMode tests, **2 FAIL**. All have zero skips. Failed receipts are retained, not relabelled. The 332 input files copied before the latter two runs have identical manifests and one shared archived source tree; both post-run comparisons were unchanged. The earlier 58-source identity qualification is explicit and points to the API archive below. Private EditorPrefs values are excluded; restoration metadata is included. Root read back all CRCs and 353 payload hashes; this does not establish physical input, ordinary Game View, browser parity, Player/IL2CPP or global audit closure. ZIP SHA-256: `b80cce9ca9dd8b42d3f378c42e0df9e130e68bb598161f4cd51b67bf1f2ae831`.

[WebContracts-20261101.zip](WebContracts-20261101.zip) preserves the two actual standalone runs and their source/dependency identities.

- Run1:57 passed /4 failed /0 skipped (61 executed).
- Run2:65 passed /0 failed /0 skipped (65 executed).
- Run1 sources are post-run exact-hash reconstructions or unchanged bytes matching the original freeze; run2 sources were copied before execution.
- The first run has no separately captured complete raw console file; this is explicit in its receipt.
- No Unity, Player, browser, native UI, input or IL2CPP acceptance is established by these runs.

ZIP SHA-256: `6a9516dbde68a7a9675b38a18df15b78efd5f5b6bc6acacac583e24f049201a2`. Each of 25 payload members has a size and SHA-256 in manifest.json. ZIP CRC and member hashes were read back and verified.

[Package-check receipts](package-checks.json) and their three console logs are stored alongside: run1 retained two stale active sample-path errors; runs2/3 passed with 5889 files, 71 assemblies, 3209 GUIDs and zero errors. These are static package checks, not Unity import or runtime tests.

[Final package receipt](package-check-native-final.json) and its [raw log](package-check-native-final.log) record the same zero-error static result after the native fixture/documentation updates. No failed earlier receipt was replaced.

[HtmlApiTypecheck-20261101.zip](HtmlApiTypecheck-20261101.zip) preserves API-only C#9/netstandard2.1 compiler preflights against actual installed 6000.3.14f1 and 6000.6.0f1 managed DLLs. Both built 33 Runtime + 23 Editor authored sources and two fixtures, with zero errors and one retained CS0649 warning per profile. Source, direct-reference and output hashes were checked after builds and independently reviewed; all 83 payload hashes/CRC match. Neo.Extensions/TestRunner were precompiled dependencies, 1701/1702 warnings suppressed and explicitly recorded. This is not Unity import, native fixture execution, Player, full-package compilation or a distinct Unity6.5 branch test. Never execute these Editor fixture DLLs in a standalone host. ZIP SHA-256: `09cc9986e1051b6383ca25114b29256c32121bfe0e43956649b839b99626599d`.
