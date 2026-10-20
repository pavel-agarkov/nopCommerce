#!/usr/bin/env bash
# Checks that no existing `virtual` member signature of CheckoutController changed.
#
# Usage: cc-virtual-signatures.sh <base-sha>
#
# Runs the research V1 rule (cc-virtual-members.yml) against CheckoutController.cs
# at <base-sha> and against the working-tree file, reduces every match to its
# declaration up to the body (`{` or `=>`) with attributes dropped and whitespace
# normalized, and fails if any old declaration is missing from the new set.
# New declarations are allowed.
#
# Requires: git, ast-grep, jq.

set -euo pipefail

if [ "$#" -ne 1 ]; then
    echo "usage: $0 <base-sha>" >&2
    exit 2
fi

base_sha="$1"
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
rule_file="$script_dir/cc-virtual-members.yml"
repo_root="$(git -C "$script_dir" rev-parse --show-toplevel)"
cc_path="src/Presentation/Nop.Web/Controllers/CheckoutController.cs"

for tool in git ast-grep jq; do
    if ! command -v "$tool" >/dev/null 2>&1; then
        echo "error: '$tool' is required but was not found on PATH" >&2
        exit 2
    fi
done

work_dir="$(mktemp -d)"
trap 'rm -rf "$work_dir"' EXIT

# keep the .cs extension so ast-grep picks the C# parser for the old copy
mkdir -p "$work_dir/old"
git -C "$repo_root" show "$base_sha:$cc_path" > "$work_dir/old/CheckoutController.cs"

# prints one normalized declaration per line, sorted and unique
extract_signatures() {
    ast-grep scan --rule "$rule_file" --json "$1" \
        | jq -r '.[].text
            | gsub("\\s+"; " ")
            | sub("^ "; "")
            | sub("^(\\[[^\\]]*\\] ?)+"; "")
            | sub(" ?(\\{|=>).*$"; "")
            | sub(" $"; "")' \
        | tr -d '\r' \
        | LC_ALL=C sort -u
}

extract_signatures "$work_dir/old/CheckoutController.cs" > "$work_dir/old.txt"
extract_signatures "$repo_root/$cc_path" > "$work_dir/new.txt"

old_count="$(wc -l < "$work_dir/old.txt" | tr -d ' ')"
new_count="$(wc -l < "$work_dir/new.txt" | tr -d ' ')"

if [ "$old_count" -eq 0 ]; then
    echo "error: no virtual members found at $base_sha; check the rule and path" >&2
    exit 2
fi

missing="$(LC_ALL=C comm -23 "$work_dir/old.txt" "$work_dir/new.txt")"

if [ -n "$missing" ]; then
    echo "FAIL: virtual declarations at $base_sha missing or changed in the working tree:"
    printf '%s\n' "$missing" | sed 's/^/  - /'
    exit 1
fi

added="$(LC_ALL=C comm -13 "$work_dir/old.txt" "$work_dir/new.txt")"
echo "OK: all $old_count virtual declarations at $base_sha are present unchanged ($new_count now)."
if [ -n "$added" ]; then
    echo "New declarations:"
    printf '%s\n' "$added" | sed 's/^/  + /'
fi
