# Fixture authoring scripts

`python tools/author_fixtures/g4.py` regenerates all 60 fixtures under `fixtures/` (g1 to g4 build on each other; g1 uses the Python oracle for S01 to S16 and S42, the rest is authored from `docs/EMULATIONS.md`). Run it from the repository root, then `python tools/validate_schemas.py` and `python tools/oracle_diff.py`. The fixtures are the data; these scripts are only the architect's authoring aid.
