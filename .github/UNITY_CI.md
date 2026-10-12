# Unity license setup for GitHub Actions

The `Unity Tests` workflow uses `game-ci/unity-test-runner@v4`. Configure the
repository secrets in **Settings -> Secrets and variables -> Actions** using
exactly one licensing method:

- Unity Personal: `UNITY_LICENSE` (the complete contents of the locally
  activated `Unity_lic.ulf` file), `UNITY_EMAIL`, and `UNITY_PASSWORD`.
- Unity Pro/Plus: `UNITY_SERIAL`, `UNITY_EMAIL`, and `UNITY_PASSWORD`.

The independent `Package integrity` job runs without Unity or license secrets.
It checks UTF-8, JSON/assembly manifests, Unity GUIDs and metadata, merge
conflicts, sample paths, and centralized instance-ID compatibility. Run the
same check locally with `python scripts/check_package.py`. A passing package
check does not replace compilation or EditMode/PlayMode tests.

Независимая проверка `Package integrity` работает без Unity и секретов лицензии.
Локальный запуск: `python scripts/check_package.py`. Она проверяет кодировку,
манифесты, GUID и `.meta`, конфликты слияния, пути сэмплов и общий слой
совместимости идентификаторов. Компиляцию и тесты Unity нужно запускать отдельно.

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
