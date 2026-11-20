#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 2 || $# -gt 3 ]]; then
  printf 'Usage: %s <NombrePascal> <nombre-kebab> [--dry-run]\n' "$0" >&2
  printf '  <NombrePascal>  identifier ASCII valido, p. ej. AcmeLedger\n' >&2
  printf '  <nombre-kebab>  minusculas con guiones, p. ej. acme-ledger\n' >&2
  exit 2
fi
dry_run=0
if [[ $# -eq 3 ]]; then
  if [[ "$3" != "--dry-run" ]]; then
    printf 'Usage: %s <NombrePascal> <nombre-kebab> [--dry-run]\n' "$0" >&2
    exit 2
  fi
  dry_run=1
fi

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
python3 - "$repo_root" "$1" "$2" "$dry_run" "$0" <<'PY'
import os
import re
import sys
from pathlib import Path

root = Path(sys.argv[1]).resolve()
new_pascal = sys.argv[2]
new_kebab = sys.argv[3]
dry_run = sys.argv[4] == "1"
script = Path(sys.argv[5]).resolve()

if not re.fullmatch(r"[A-Za-z][A-Za-z0-9]*", new_pascal):
    raise SystemExit("NombrePascal debe ser un identificador ASCII (letras y digitos).")
if not re.fullmatch(r"[a-z0-9]+(-[a-z0-9]+)*", new_kebab):
    raise SystemExit("nombre-kebab debe ser minusculas separadas por guiones.")

excluded = {
    ".git", ".next", ".turbo", ".vercel", "node_modules", "bin", "obj",
    "reports", "test-results", "playwright-report", "coverage", "dist", "build",
}
paths = []
for current, directories, files in os.walk(root, topdown=True, followlinks=False):
    current_path = Path(current)
    directories[:] = sorted(
        name for name in directories
        if name not in excluded and not (current_path / name).is_symlink()
    )
    paths.extend(current_path / name for name in directories)
    paths.extend(
        current_path / name for name in files
        if not (current_path / name).is_symlink()
        and (not name.startswith(".env") or name == ".env.example")
    )

# Old names are built from fragments so the completeness grep over the
# template does not match this tool itself.
old_kebab = "conta" + "sync"
old_pascal = "Conta" + "Sync"

def replace_name(value: str) -> str:
    return (
        value
        .replace(f"{old_kebab}-api", f"{new_kebab}-api")
        .replace(f"{old_kebab}-web", f"{new_kebab}-web")
        .replace(old_kebab, new_kebab)
        .replace(old_pascal, new_pascal)
    )

moves = []
for source in paths:
    relative = source.relative_to(root)
    destination = root.joinpath(*(replace_name(part) for part in relative.parts))
    if destination != source:
        moves.append((source, destination))

moving_sources = {source for source, _ in moves}
destinations = [destination for _, destination in moves]
if len(set(destinations)) != len(destinations):
    raise SystemExit("Rename aborted: multiple paths would have the same destination.")
for destination in destinations:
    if destination.exists() and destination not in moving_sources:
        raise SystemExit(f"Rename aborted: destination already exists: {destination.relative_to(root)}")

content_updates = []
for source in paths:
    if not source.is_file() or source.resolve() == script:
        continue
    try:
        content = source.read_bytes().decode("utf-8")
    except UnicodeDecodeError:
        continue
    if "\0" in content:
        continue
    updated = replace_name(content)
    if updated != content:
        content_updates.append((source, updated))

if dry_run:
    for source, _ in content_updates:
        print(f"  content  {source.relative_to(root)}")
    for source, destination in sorted(moves, key=lambda pair: len(pair[0].parts), reverse=True):
        print(f"  rename   {source.relative_to(root)}  ->  {destination.relative_to(root)}")
    print(f"Dry run: {len(content_updates)} text files and {len(moves)} paths would change.")
    raise SystemExit(0)

for source, updated in content_updates:
    source.write_text(updated, encoding="utf-8")
for source, destination in sorted(moves, key=lambda pair: len(pair[0].parts), reverse=True):
    renamed_name = replace_name(source.name)
    if renamed_name != source.name:
        os.replace(source, source.with_name(renamed_name))

print(f"Updated {len(content_updates)} text files and renamed {len(moves)} paths.")
PY