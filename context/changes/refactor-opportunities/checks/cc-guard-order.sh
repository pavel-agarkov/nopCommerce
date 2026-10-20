#!/usr/bin/env bash
# Checks that ValidateCheckoutAccessAsync tests `CheckoutDisabled` before it loads the customer.
#
# Usage: cc-guard-order.sh [file]
#
# Runs cc-guard-order.yml against CheckoutController.cs (or the given file) and fails if any
# statement of the guard loads the customer before the `if (_orderSettings.CheckoutDisabled)`
# check. Also fails if the guard method or its CheckoutDisabled check cannot be found, so a
# rename does not make the rule pass vacuously.
#
# Requires: git, ast-grep, jq.

set -euo pipefail

if [ "$#" -gt 1 ]; then
    echo "usage: $0 [file]" >&2
    exit 2
fi

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
rule_file="$script_dir/cc-guard-order.yml"
repo_root="$(git -C "$script_dir" rev-parse --show-toplevel)"
cc_file="${1:-$repo_root/src/Presentation/Nop.Web/Controllers/CheckoutController.cs}"

for tool in git ast-grep jq; do
    if ! command -v "$tool" >/dev/null 2>&1; then
        echo "error: '$tool' is required but was not found on PATH" >&2
        exit 2
    fi
done

# the guard and its CheckoutDisabled check must exist, otherwise the rule below proves nothing
anchor_count="$(ast-grep scan --inline-rules '
id: cc-guard-anchor
language: csharp
rule:
  kind: if_statement
  has:
    field: condition
    pattern: _orderSettings.CheckoutDisabled
  inside:
    kind: method_declaration
    has:
      field: name
      regex: ^ValidateCheckoutAccessAsync$
    stopBy: end
' --json "$cc_file" | jq 'length')"

if [ "$anchor_count" -ne 1 ]; then
    echo "error: expected one CheckoutDisabled check inside ValidateCheckoutAccessAsync, found $anchor_count" >&2
    exit 2
fi

violations="$(ast-grep scan --rule "$rule_file" --json "$cc_file" \
    | jq -r '.[] | "\(.range.start.line + 1): \(.lines | gsub("\\s+"; " "))"')"

if [ -n "$violations" ]; then
    echo "FAIL: ValidateCheckoutAccessAsync loads the customer before the CheckoutDisabled check:"
    printf '%s\n' "$violations" | sed 's/^/  - line /'
    exit 1
fi

echo "OK: ValidateCheckoutAccessAsync checks CheckoutDisabled before loading the customer."
