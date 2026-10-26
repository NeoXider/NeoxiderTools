# Play tests — existing interaction Core reference

2026-10-25, source integration only. The final same-author Interactor receipt
`d70c5a3c9925273c9f495a281b4d268605e69a5f23fe4b09ccd7e59df25aac21`
identifies direct public InteractionQueryMath/InteractionRayHit use in the moved
SharedQuery fixture. The current Play test assembly explicitly referenced only
the parent interaction assembly; Core is a separate existing assembly.

Root added exactly `Neo.Tools.InteractableObject.Core` immediately after that
parent in `Assets/Neoxider/Tests/Play/Neo.Tests.Play.asmdef`. Read the complete
existing Play/Core definitions first. Core's own references remain empty; no new
production API, friend grant, dependency cycle, helper move or production assembly
change is introduced. All existing Play references/constraints/settings remain.

This discharges the receipt's shared-reference source dependency. It is not a
compiler/discovery pass. Independent final SharedQuery/configuration/source-map
review and actual both-version/mode native execution remain required. No compiler,
test runner, Unity or product checker was launched for this integration.
