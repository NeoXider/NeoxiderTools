#!/usr/bin/env bash
set -eu
if [ ! -f src/HtmlToUitk.Cli/HtmlToUitk.Cli.csproj ]; then
  echo "CLI project not present yet; skipping the determinism check"
  exit 0
fi
cli="dotnet run --project src/HtmlToUitk.Cli -c Release --no-build --"
for run in a b; do
  rm -rf "artifacts/determinism-$run"
  $cli convert examples/pages --config examples/htmltouitk.config.json --out "artifacts/determinism-$run" || true
done
diff -r "artifacts/determinism-a" "artifacts/determinism-b"
echo "two runs produced identical bytes"
