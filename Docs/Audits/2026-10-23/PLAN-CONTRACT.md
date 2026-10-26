# Phase 2: individual implementation plan

The user explicitly requests continuing the SAME audit worker/session after that worker completes its audit, then the coordinator will personally read every audit and every plan.

Read your completed audit, current owned source/docs/test changes, relevant cross-assembly boundaries and your manifest entry. Write ONLY your dedicated `*.plan.md` path from the manifest. Do not mutate code, tests, public docs, audit reports, package configuration or Unity state during this stage; integrated tests may run after all source-changing audits finish; other modules may still be auditing while you plan. Do not create a replacement agent/session or launch workers.

A concrete plan must contain:

1. **Current outcome:** what your audit actually fixed and documented; distinguish source review/static checks from executed Unity verification. Do not propose already-completed fixes as pending work.
2. **Prioritized remaining defects and verification debt:** each has evidence/source/reproduction or an explicit unconfirmed risk, priority, owner, smallest correction and meaningful expected observable result. Cross-assembly changes identify the other assembly owner and order. Do not manufacture bugs or present a speculative improvement as a confirmed defect.
3. **At least one substantial improvement:** explain developer/user problem; existing APIs/candidates to reuse; focused design and public C# API or authoring changes; exact affected files/assemblies; implementation steps; compatibility/serialized field/asset and sample-layout considerations; documentation for C# and scene/NoCode users; risk/effort; acceptance scenarios and the minimum useful tests. Pure proposals remain proposals, not claimed implementations.
4. **Implementation order:** safe fixes first, regression checks, public API work, component/editor adapters only where needed, documentation, release/package validation. Keep module independence and centralized Unity-version compatibility gates.
5. **Acceptance and stop conditions:** exact public behavior to observe, relevant Unity 6.0-6.4 and 6.5+ compile branch checks, EditMode/PlayMode scope, what remains unverified or needs a product decision. Do not demand approval for routine reversible fixes already authorized.

Keep it reviewable and specific: roughly 500-1000 words for a large module, shorter for genuinely tiny contracts. Russian is appropriate for this internal plan; all text must be real UTF-8. No git commits/pushes or agent signatures. Return the exact plan path and a concise summary. The coordinator will read every file in full and record decisions.
