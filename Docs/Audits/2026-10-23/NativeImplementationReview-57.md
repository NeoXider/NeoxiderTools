# NI57 — operator guard closure and no-native probe boundary

Disposition: **SOURCE-CLOSED, no confirmed residual in this bounded delta.** Independent read-only review; A declared SOURCE-FINAL/writes stopped and root separately confirmed admission. No PowerShell, parser, probe, preflight, Unity, compiler, checker or product test was executed. Root review and explicit authorization remain required before either probe; native/product acceptance remains OPEN.

## Complete read coverage and immutable evidence

All utility paths below are under `Local/Audits/assembly-2026-10-23/unity/maintenance-source/asset-schema/`. Full bytes and every old/new guard changed line were read, not only the handoff claims.

| Artifact | Content lines | Actual SHA256 |
| --- | ---: | --- |
| `operator_diagnostic_guard.ps1` | 113 | `0b06bc13629f26590f5d64b84c6259660dabc99fcffd97eb04af840eba72cadf` |
| `operator_diagnostic_scope_probe.ps1` | 33 | `b210dbaf434b8d2cc496790385a8732644b2c983780a39c32c2de24807de5f26` |
| `operator_diagnostic_exit_probe.ps1` | 2 | `1f2c5bf690df2359777b4334cff2fac8a81d5fed3c1b44fd99e9c88568f4341b` |
| `OperatorGuardReviewPlan.md` | 30 | `7883067af04efb9cdf2ab331a563763ada86a0ea2323a41525eca6d1af3dd4bb` |
| `operator-guard-revision-handoff.json` | complete | `a2879f7f749b0287b247290a7b3536489c3e62b294a9b480f95dd54110c5da80` |
| Preserved `operator_diagnostic_guard.v1-952b0a29.ps1` | 78 | `952b0a29a3b0ae1b2a84a06d3c4f3ac25e1409805f65cee7d143ca1b4fa0839c` |
| Unchanged `launch_schema_maintenance.ps1` | 96 | `43a8cb30a5ebcbb2095e43cb8aa38280778e315070af90718fc69e24f40b33b1` |

The full historical `maintenance/ba515785480c4d2c9c791b2b91cc0e82/operator-guard-scope-failure.md`/`.json` were also read: SHA `5f6c7b9d452251259cc50e5211119dbf58f0c7c51607a1f41e00107dd47d07c1` / `82d7c862f645aff5e45f676481bd8971652658f38ef9235a1527701c924419cb`. They record the caller-dependent `$script:taskExpectedUnity` failure after the canonical claim, null native PID/exit, no immediate memory receipt/Library/Editor.log/native receipt, and misleading outer0. Those failed-attempt facts are preserved; this source review supplies no new native result.

## Bounded source findings

- **Lexical ownership supported:** guard31–58 creates the recorder with `GetNewClosure()` over the context object; guard62–93 independently closes the proxy over that same context and recorder. Neither block resolves the old caller-dependent `script:` variables/functions. Only installing the reviewed function uses `Function:script:Start-Process` (94); invoking the separate child script preserves the intended scope boundary.
- **Launch contract retained:** guard65–78 keeps all seven named parameters, exact Unity/project/stdout/stderr paths, Hidden and present PassThru, and now rejects a bound-parameter count other than7. The normal argument values still come from the unchanged pinned launcher63–78; the proxy does not independently parse them. The probe21–31 calls the same actual function and checks its captured one-string argument list and every other bound value against that producer-shaped call.
- **Fail-closed recorder retained:** guard38–57 still obtains one fresh `Win32_PerfFormattedData_PerfOS_Memory` row, requires parsable unsigned counters, positive limit and committed<=limit, and admits only available commit>=6GiB AND physical>=8GiB. Missing/query/invalid/low counters retain a failure receipt and throw. Receipt-write failures also stop control flow. The proxy invokes this captured recorder at80 before any native delegate; normal execution also retains the first check at111 before the original long preflight.
- **No-native separation supported:** guard96–106 pins both child-script hashes and exits before normal111–113. Scope probe5–10 requires an actual Function with the expected marker, a closure module, and captured `ProbeOnly=true` before its call; missing function/module/context fails instead of falling back to the real cmdlet. Proxy81–90 records only probe evidence and returns a marked no-process DTO before the sole module-qualified native delegate92. The recorder emits no success-pipeline objects; the child requires exactly one marked result. No probe path invokes the original launcher/preflight/job claim, Unity or native stdout redirection. Probe evidence lives in a separate GUID `operator-probe` directory; the exit-only path also creates that directory but performs no CIM/process/job operation.
- **Exit propagation supported:** the fixed exit child contains `exit17`; guard104/106 forwards that child result with explicit `exit $LASTEXITCODE`. Scope child33 explicitly exits0 only after its actual proxy/CIM/binding checks. Normal guard112–113 forwards the original launcher result: preflight/guard terminating errors remain unhandled failures, launcher error/null/receipt codes3/4/5 and actual native codes1/2 are not intentionally replaced by0. The future fresh outer-process observations remain necessary to establish these PowerShell execution semantics on the installed host.

No original launcher, seeds, authorization, existing canonical claim/logs, product source or execution state was changed by this review. Low memory is an expected blocking probe result, not permission to weaken thresholds. Required next evidence is separately authorized actual child-scope/CIM proof plus outer17/error propagation; neither would constitute native schema/product acceptance or authorize claim reuse, upgrade, copyback or retries. All reviewed file hashes were rechecked unchanged at completion.
