# Unity license setup for GitHub Actions

The `Unity Tests` workflow uses `game-ci/unity-test-runner@v4`. Configure the
repository secrets in **Settings -> Secrets and variables -> Actions** using
exactly one licensing method:

- Unity Personal: `UNITY_LICENSE` (the complete contents of the locally
  activated `Unity_lic.ulf` file), `UNITY_EMAIL`, and `UNITY_PASSWORD`.
- Unity Pro/Plus: `UNITY_SERIAL`, `UNITY_EMAIL`, and `UNITY_PASSWORD`.

Do not configure both `UNITY_LICENSE` and `UNITY_SERIAL`. The workflow validates
the configuration before starting the EditMode and PlayMode jobs, but it never
prints secret values.

For a Personal license, current GameCI guidance is to activate it locally in
Unity Hub and copy `Unity_lic.ulf` into `UNITY_LICENSE`. The current guide no
longer recommends the old GitHub activation-file request workflow, so this
repository intentionally does not include one.

Official references:

- https://game.ci/docs/github/activation/
- https://game.ci/docs/github/test-runner/
