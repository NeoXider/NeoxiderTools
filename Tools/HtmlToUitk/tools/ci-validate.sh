#!/usr/bin/env bash
set -u
os="${1:-local}"
out="artifacts/diagnostics"
mkdir -p "$out"
if [ ! -f src/HtmlToUitk.Cli/HtmlToUitk.Cli.csproj ]; then
  echo "CLI project not present yet; skipping validation of examples and fixtures"
  exit 0
fi
cli="dotnet run --project src/HtmlToUitk.Cli -c Release --no-build --"
status=0
$cli validate examples/pages --config examples/htmltouitk.config.json --format json --out "$out/examples-$os.json"
code=$?
echo "examples exit code $code"
if [ "$code" -ge 2 ]; then status=1; fi
exit $status
