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
# Two more comparisons (review finding F7) use the same reduction:
#   - the same declarations with their attributes kept, so a dropped or changed
#     [HttpPost], [IgnoreAntiforgeryToken], [ValidateCaptcha] or [FormValueRequired]
#     on an existing action fails (the DI harness cannot exercise these);
#   - the constructor declarations, which must match exactly (none added or removed).
#
# The new side is the working tree, not HEAD, so the check can run before a commit;
# to check a committed state, run it on a clean working tree.
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

ctor_rule='
id: cc-constructors
language: csharp
rule:
  kind: constructor_declaration
'

# prints one normalized declaration per line, sorted and unique
# $1: file, $2: "strip" to drop leading attributes or "keep" to keep them, $3: optional inline rule
extract_signatures() {
    local rule_args=(--rule "$rule_file")
    if [ -n "${3:-}" ]; then
        rule_args=(--inline-rules "$3")
    fi
    ast-grep scan "${rule_args[@]}" --json "$1" \
        | jq -r --arg mode "$2" '.[].text
            | gsub("\\s+"; " ")
            | sub("^ "; "")
            | (if $mode == "strip" then sub("^(\\[[^\\]]*\\] ?)+"; "") else . end)
            | sub(" ?(\\{|=>).*$"; "")
            | sub(" $"; "")' \
        | tr -d '\r' \
        | LC_ALL=C sort -u
}

extract_signatures "$work_dir/old/CheckoutController.cs" strip > "$work_dir/old.txt"
extract_signatures "$repo_root/$cc_path" strip > "$work_dir/new.txt"
extract_signatures "$work_dir/old/CheckoutController.cs" keep > "$work_dir/old-attr.txt"
extract_signatures "$repo_root/$cc_path" keep > "$work_dir/new-attr.txt"
extract_signatures "$work_dir/old/CheckoutController.cs" keep "$ctor_rule" > "$work_dir/old-ctor.txt"
extract_signatures "$repo_root/$cc_path" keep "$ctor_rule" > "$work_dir/new-ctor.txt"

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

missing_attr="$(LC_ALL=C comm -23 "$work_dir/old-attr.txt" "$work_dir/new-attr.txt")"

if [ -n "$missing_attr" ]; then
    echo "FAIL: attributes of virtual declarations at $base_sha missing or changed in the working tree:"
    printf '%s\n' "$missing_attr" | sed 's/^/  - /'
    exit 1
fi

if [ ! -s "$work_dir/old-ctor.txt" ]; then
    echo "error: no constructor found at $base_sha; check the rule and path" >&2
    exit 2
fi

if ! cmp -s "$work_dir/old-ctor.txt" "$work_dir/new-ctor.txt"; then
    echo "FAIL: constructor declarations changed since $base_sha:"
    diff "$work_dir/old-ctor.txt" "$work_dir/new-ctor.txt" | sed 's/^/  /' || true
    exit 1
fi

added="$(LC_ALL=C comm -13 "$work_dir/old.txt" "$work_dir/new.txt")"
echo "OK: all $old_count virtual declarations at $base_sha are present unchanged with their attributes ($new_count now); constructor unchanged."
if [ -n "$added" ]; then
    echo "New declarations:"
    printf '%s\n' "$added" | sed 's/^/  + /'
fi
