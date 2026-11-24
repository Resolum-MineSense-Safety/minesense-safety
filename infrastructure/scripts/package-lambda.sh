#!/usr/bin/env bash
# Builds src/serverless/ReportPdfGenerator/artifacts/ReportPdfGenerator.zip for the dotnet8 arm64
# Lambda runtime (the zip Terraform uploads). Only needs a .NET SDK able to target net8.0.
set -euo pipefail
source "$(dirname "$0")/lib/common.sh"
require_tools dotnet

PROJECT="$REPO_ROOT/src/serverless/ReportPdfGenerator"
OUTPUT="$PROJECT/artifacts/ReportPdfGenerator.zip"
PUBLISH="$PROJECT/bin/lambda-publish"

rm -rf "$PUBLISH" "$OUTPUT"
mkdir -p "$PROJECT/artifacts"
dotnet publish "$PROJECT/ReportPdfGenerator.csproj" -c Release -r linux-arm64 --self-contained false -o "$PUBLISH" --nologo -v quiet

# Files at the root of the zip, with / as separator whatever the platform.
if command -v zip >/dev/null 2>&1; then
  (cd "$PUBLISH" && zip -qr -X "$OUTPUT" .)
elif command -v powershell.exe >/dev/null 2>&1; then
  powershell.exe -NoProfile -NonInteractive -Command \
    "Add-Type -AssemblyName System.IO.Compression.FileSystem; [IO.Compression.ZipFile]::CreateFromDirectory('$PUBLISH', '$OUTPUT')"
else
  die "Hace falta zip (o powershell.exe) para empaquetar la Lambda."
fi
[[ -s "$OUTPUT" ]] || die "No se generó $OUTPUT"
echo "Lambda empaquetada: $OUTPUT"
