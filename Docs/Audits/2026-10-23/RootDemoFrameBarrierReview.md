# Root E28 review — real frame barriers and Play ownership

Date: 2026-10-25. Source-only review after personally accounting for the complete
current Demo receipt88043db8... against the previously fully read frozen baseline.
Read actual `Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoRpgDemoTests.cs:1–205`.
The two board-view native lifecycle candidates preserve their substantive oracles,
but their new deferred-destruction arrangement still has two bounded source gaps.

- **P2 / native frame admission:** lines167–168 and179–180 yield null twice from an
  Edit-assembly UnityTest. The installed Edit Test Framework advances the test
  enumerator through Editor updates after EnterPlayMode; these polls do not prove
  that two real `Time.frameCount` increments occurred. The earlier source3 cadence
  failures and the independently reviewed Coroutine correction establish this
  distinction. A stale cell/sprite after those polls cannot isolate a runtime leak.
  Require two observed native frame advances with a bounded realtime deadline;
  keep exact cell counts, every old-cell destruction, generated-sprite destruction,
  independent owner/art/white-texture/generator survival and all three rebuilds.
- **P2 / session ownership:** teardown23–39 exits any observed Play session. It
  does not record whether this fixture successfully acquired the transition.
  Reject an existing Play session before acquisition; set a private ownership flag
  only after its top-level EnterPlayMode succeeds. Exit only that acquired session,
  including when deferred cleanup faults. Do not replace a failed cleanup with a
  pass or allow it to prevent the top-level owned ExitPlayMode.

This finding concerns test setup/cleanup, not a demonstrated board-view runtime
defect. Resume the same original Demo author for only these fixture/receipt changes;
the NI39 nullable-Level runtime/Play source remains unchanged. New native outcomes
are UNEXECUTED. No compiler, test runner, Unity, checker or process was launched by
this root source review.
