# Extensions-Numeric-Time-Random: approved implementation request

Original actor: `neo-audit-1023-extra-01`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Own8 numeric/time/random runtime files. Existing corrected weighted/time helper retained. Check final docs consumer guidance; no second formatting or RNG kernel, no new immutable distribution requirement.

## Assigned review actions

No additional confirmed runtime defect was assigned after independent review. Preserve your coherent earlier patch. Finish canonical docs/navigation and factual API/scene/NoCode contracts in owned scope, keep at least one substantial improvement described as a proposal, and write an honest source-final receipt. Do not manufacture a runtime rewrite or duplicate tests.

## Relevant full native sections

Source: NativeReview-A.md

### Extensions-Numeric-Time-Random — source review complete

Original owner: `neo-audit-1023-extra-01`. Full artifact reads:

- `Docs/Audits/2026-10-23/Extensions-Numeric-Time-Random.md` — `a13c5ca5385d89b11b5553e444922d988126005980011f42ddf4880138d2fa1c`.
- `Docs/Audits/2026-10-23/Extensions-Numeric-Time-Random.plan.md` — `a06da07e4f60d4d11db150ff4432793f232b78dd06f7551e9a0187bb5aaaa989`.

All changed numeric/time/random hunks were inspected, including readonly value-option copying, signed rounding, decimal decomposition, sticky BigInteger digits, small-double/scientific fallback, finite weight totals, full-int snapping and tick-based timestamp arithmetic. Complete new tests:

- `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023NumericFormatTests.cs` — `38dced82a1fce33de7848fb153f2a16e3fa0ce91974212d64ed8593491ff37cc`.
- `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023NumericRandomAndPrimitiveTests.cs` — `e95356e79f3cce97397a58d6114f9265cb99d881a181f076dcc3e6f929b08df1`.
- `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023NumericTimeTests.cs` — `0e09b06fe7ee20d0ab05731200b62c9cfdc17a49d8c564237c55fdfa5236a9dd`.

**Completed fixes appear correct by source inspection** for the named counterexamples. NumberFormatOptions is a struct, so local rounding/separator changes do not mutate caller options. Boundary tests use independent known numeric/text/date outputs; the RNG fixture restores its prior random state and diagnostics flag. No new scene searches or autonomous updates exist. Weighted draws remain O(n), and selector draws allocate a weights array; the immutable distribution is a **future proposal**, with existing ChanceManager reuse and dependency direction correctly acknowledged.

**Confirmed consumer residual:** A-07; count delegation is already present. **Needs evidence:** Mono E16/subnormal formatting and extreme native Random.Range, seeded-boundary compatibility and new rounding/saturation policies. Further double-rounding edge cases near the decimal routing threshold merit targeted numerical evidence before any additional behavior change; no unexecuted arithmetic probe is claimed as a C# result. The pure formatter/timing code does not prove consumer integration by itself.

## Completion receipt

Write `Docs/Audits/2026-10-23/Extensions-Numeric-Time-Random.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.
