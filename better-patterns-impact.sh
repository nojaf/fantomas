#!/usr/bin/env bash
# Formats a copy of the Fantomas sources with the local build and fsharp_experimental_better_patterns
# on, into tmp/better-patterns/, and lists where the result differs from the sources.
# Build first: dotnet build src/Fantomas
set -euo pipefail

repo=$(cd "$(dirname "$0")" && pwd)
out=$repo/tmp/better-patterns
fantomas=$repo/artifacts/bin/Fantomas/debug/fantomas.dll

rm -rf "$out"
mkdir -p "$out"
rsync -a --exclude bin --exclude obj --exclude 'Fantomas.FCS*' --exclude cases \
    "$repo/src" "$repo/analyzers" "$repo/scripts" "$repo/build.fsx" "$out/"
cp "$repo/.editorconfig" "$repo/.fantomasignore" "$out/"
printf '\n[*.{fs,fsi,fsx}]\nfsharp_experimental_better_patterns = true\n' >>"$out/.editorconfig"

# A file Fantomas cannot format is left as it is, so it shows no changes below. Name those first.
(cd "$out" && dotnet "$fantomas" . 2>&1 | sed -n 's#^x \./\(.*\) could not be formatted: \(.*\)#not formatted: tmp/better-patterns/\1: \2#p') || true

# One line per change: the formatted file and the line the change starts on in it. A file without
# changes is removed, so what is left in tmp/better-patterns/ is what changed.
cd "$repo"
for file in $(cd "$out" && find src analyzers scripts build.fsx -name '*.fs' -o -name '*.fsi' -o -name '*.fsx' | sort); do
    if cmp -s "$file" "$out/$file"; then
        rm "$out/$file"
    else
        diff "$file" "$out/$file" | sed -n "s#^[0-9,]*[acd]\([0-9]*\).*#tmp/better-patterns/$file:\1#p" || true
    fi
done
find "$out" -type f ! -name '*.fs' ! -name '*.fsi' ! -name '*.fsx' ! -path "$out/.editorconfig" ! -path "$out/.fantomasignore" -delete
find "$out" -type d -empty -delete
