#!/usr/bin/env bash
# Checks that the actions calling ValidateCheckoutAccessAsync are exactly the expected guarded actions.
#
# Usage: cc-guard-coverage.sh [controller-file test-file]
#
# The reflection test CheckoutGuardTests.EveryGuardedActionHasGuardTestCases verifies that every action in
# its _expectedGuardedActions list exists and has guard cases. This script adds the opposite check (review
# finding F8): every method of CheckoutController that calls ValidateCheckoutAccessAsync must be on that list,
# so a newly guarded action cannot skip the guard tests. The list is read from the test file, not duplicated.
#
# Requires: git, ast-grep, jq.

set -euo pipefail

if [ "$#" -ne 0 ] && [ "$#" -ne 2 ]; then
    echo "usage: $0 [controller-file test-file]" >&2
    exit 2
fi

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(git -C "$script_dir" rev-parse --show-toplevel)"
cc_file="${1:-$repo_root/src/Presentation/Nop.Web/Controllers/CheckoutController.cs}"
test_file="${2:-$repo_root/src/Tests/Nop.Tests/Nop.Web.Tests/Public/Controllers/Checkout/CheckoutGuardTests.cs}"

for tool in git ast-grep jq; do
    if ! command -v "$tool" >/dev/null 2>&1; then
        echo "error: '$tool' is required but was not found on PATH" >&2
        exit 2
    fi
done

work_dir="$(mktemp -d)"
trap 'rm -rf "$work_dir"' EXIT

# names of the methods that call the guard (the guard's own declaration does not call itself)
ast-grep scan --inline-rules '
id: cc-guard-callers
language: csharp
rule:
  kind: method_declaration
  has:
    field: name
    pattern: $NAME
  all:
    - has:
        pattern: ValidateCheckoutAccessAsync($$$)
        stopBy: end
' --json "$cc_file" \
    | jq -r '.[].metaVariables.single.NAME.text' \
    | tr -d '\r' | LC_ALL=C sort -u > "$work_dir/callers.txt"

# string literals of the _expectedGuardedActions initializer in the test
sed -n '/_expectedGuardedActions =/,/];/p' "$test_file" \
    | grep -o '"[A-Za-z0-9_]*"' | tr -d '"' \
    | LC_ALL=C sort -u > "$work_dir/expected.txt"

callers_count="$(wc -l < "$work_dir/callers.txt" | tr -d ' ')"
expected_count="$(wc -l < "$work_dir/expected.txt" | tr -d ' ')"

if [ "$callers_count" -eq 0 ] || [ "$expected_count" -eq 0 ]; then
    echo "error: found $callers_count guard callers and $expected_count expected actions; check the paths and patterns" >&2
    exit 2
fi

unlisted="$(LC_ALL=C comm -23 "$work_dir/callers.txt" "$work_dir/expected.txt")"
unguarded="$(LC_ALL=C comm -13 "$work_dir/callers.txt" "$work_dir/expected.txt")"

status=0
if [ -n "$unlisted" ]; then
    echo "FAIL: actions call ValidateCheckoutAccessAsync but are missing from _expectedGuardedActions:"
    printf '%s\n' "$unlisted" | sed 's/^/  - /'
    status=1
fi
if [ -n "$unguarded" ]; then
    echo "FAIL: actions in _expectedGuardedActions do not call ValidateCheckoutAccessAsync:"
    printf '%s\n' "$unguarded" | sed 's/^/  - /'
    status=1
fi

if [ "$status" -eq 0 ]; then
    echo "OK: the $callers_count actions calling ValidateCheckoutAccessAsync match _expectedGuardedActions."
fi
exit "$status"
