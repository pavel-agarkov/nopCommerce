# Evidence: discussion
Sources: `gh` read-only on nopSolutions/nopCommerce (PRs and issues created 2022-01-01..2026-10-17, complete: 599 PRs, 1697 issues, all pages under the limit so nothing truncated), git history (`.work/git-log.txt`, 1955 counted commits, 15 mass excluded), HEAD grep. API use: about 160 calls (core quota 5000 -> ~4880 left); two heavy `--json reviews,comments` queries returned 502 and were re-run per quarter. Raw data in `context/map/.work/` (`prs-all.json`, `issues-all.json`, `commits.json`, `cap-table.json`).

## How PRs and issues were attributed (rules, so numbers can be judged)
- PR -> capability: (a) merge/squash commit subject ending `(#PR)`: 117 PRs; (b) PR title `#ISSUE` matched to a commit subject starting `#ISSUE`: 48; (c) `gh api pulls/N/files` for the 119 merged/open PRs that matched nothing (first 100 files each), mapped with `attribute.mjs`: 119. Mass (15), sweeping (18) and commits touching more than 3 capabilities were NOT used for matching (rule 12 spillover). Result: 284 of 599 PRs attributed; 315 unattributed (296 closed-unmerged, 19 merged with more than 100 files or no file match). Closed PRs were not file-listed (no bulk file calls), so closed-unmerged counts per capability cover only the 34 that a team commit re-landed. `evidence/inference`.
- Issue -> capability: only through commits whose subject mentions `#N`. 767 of 1697 issues are attributed; 930 are not (all 110 open ones, all 88 `on hold / maybe wont`, issues closed without a commit). A PR is counted once per capability it touches, so the columns do not sum to the totals.
- Bot/automation comments: 0 found on any PR (no bot authors in reviews/comments), so rule 7 removed nothing. Author self-comments are excluded from "human review".

## Headline facts (evidence)
- 599 PRs: 238 merged, 330 closed unmerged, 31 open. 1697 issues: 1587 closed, 110 open (57 are `on hold / maybe wont`, 23 `discussion`, 12 `enterprise / highlight feature`, only **2 are labelled bug**).
- PR flow by year (created / merged / closed-unmerged / with human review): 2022 102/53/48/30; 2023 120/54/64/11; 2024 104/42/61/6; 2025 167/61/93/13; 2026 106/28/64/5. Human review of PRs fell from 29% to 5%.
- 211 reviews on all PRs; two reviewers (skoshelev 76, RomanovM 67) give 68% of them (maintainers). 454 human comments on 343 PRs. Median time to merge 4.6 days.
- 187 of 330 closed PRs were closed within 1 day, 136 have no comment or review at all. Inference: maintainers close community PRs and re-apply the change as their own commit (34 are visibly re-landed by `#issue` commit). Closed-unmerged is therefore NOT a rejection/friction signal in this repo; see Mechanical signals.
- Bugs: 368 bug-labelled issues, closed with median 1-5 days depending on capability; only 2 open bugs: #8306 (UPS weight with German store language, 2026-09-10) and #8375 (wishlist count in navbar, 2026-10-14). Median days-to-close is short because the core team files issues for its own fixes and closes them in the same day: demoted (rule 4).
- Issue creation per year: 2022 332, 2023 347, 2024 379, 2025 414, 2026 (to Oct) 225. Issue authors: AndreiMaz 237, skoshelev 168, RomanovM 139 are core staff, so the tracker is largely the team's own work log.

## Buzz per capability
PRs = attributed PRs (merged / closed-unmerged-but-re-landed / open). H.rev = PRs with at least one human non-author review. Issues/bugs = issues with a `#N` fix commit (rest unattributed). Reverts = revert commits touching the capability (a commit counts for each capability it touched; spillover flagged). Markers = `TODO|FIXME|HACK|XXX` at HEAD in .cs/.cshtml/.js/.ts. Plans = 0 for all (see Planning attention).

| Capability | PRs | merged | closed* | open | H.rev | Issues | bugs | issue comments | issues with 8+ comments | Reverts | Markers |
|---|---|---|---|---|---|---|---|---|---|---|---|
| platform (incl. data, Migrations, loc XML) | 67 | 47 | 11 | 9 | 19 | 143 | 35 | 290 | 4 | 4 | 0 |
| foundations | 64 | 48 | 10 | 6 | 14 | 159 | 49 | 303 | 4 | 4 | 0 |
| catalog | 64 | 51 | 6 | 7 | 10 | 112 | 52 | 183 | 1 | 3 | 0 |
| ops | 43 | 34 | 3 | 6 | 11 | 96 | 34 | 161 | 1 | 1 | 1 |
| orders | 45 | 33 | 10 | 2 | 5 | 95 | 47 | 190 | 2 | 4 | 0 |
| identity | 35 | 25 | 3 | 7 | 6 | 62 | 26 | 81 | 0 | 2 | 0 |
| messaging | 25 | 18 | 5 | 2 | 4 | 64 | 20 | 109 | 3 | 1 | 0 |
| admin-ui | 19 | 16 | 1 | 2 | 2 | 67 | 27 | 99 | 0 | 2 | 0 |
| frontend-ui | 14 | 8 | 3 | 3 | 1 | 91 | 48 | 133 | 1 | 2 | 0 |
| content | 13 | 10 | 2 | 1 | 0 | 39 | 18 | 57 | 0 | 2 | 0 |
| localization | 15 | 14 | 0 | 1 | 1 | 8 | 3 | 7 | 0 | 0 | 0 |
| shipping | 9 | 7 | 0 | 2 | 0 | 15 | 7 | 57 | 2 | 0 | 0 |
| media | 10 | 8 | 2 | 0 | 1 | 17 | 8 | 22 | 0 | 2 | 0 |
| discounts | 7 | 6 | 0 | 1 | 1 | 12 | 3 | 24 | 1 | 0 | 0 |
| payments | 7 | 6 | 0 | 1 | 0 | 22 | 11 | 27 | 0 | 0 | 0 |
| tax | 6 | 4 | 2 | 0 | 1 | 13 | 5 | 16 | 0 | 0 | 0 |
| integrations | 4 | 4 | 0 | 0 | 0 | 25 | 12 | 37 | 0 | 1 | 0 |
| search | 2 | 2 | 0 | 0 | 0 | 6 | 0 | 10 | 0 | 0 | 0 |
| vendors | 1 | 1 | 0 | 0 | 0 | 3 | 3 | 4 | 0 | 0 | 0 |
| ai | 1 | 1 | 0 | 0 | 0 | 2 | 0 | 1 | 0 | 0 | 0 |
| docs | 5 | 2 | 0 | 3 | 0 | 0 | 0 | 0 | 0 | 0 | n/a |
| unmapped (deleted plugins) | 7 | 3 | 0 | 4 | 2 | 16 | 3 | 19 | 0 | 2 | n/a |
| unattributed | 315 PRs (296 closed-unmerged, 19 merged) | | | | | 930 issues (110 open, 88 on hold, 32 bugs, 2 open bugs) | | 1713 | 14 | | |

\* closed = closed unmerged AND matched to a team commit, i.e. re-landed by the team (inference); the 296 closed PRs with no matching commit are in the unattributed row. Revert column totals exceed 14 because multi-capability reverts count once per capability (14 revert commits in the window; 1 cross-cutting).

Reading (inference): foundations/platform/catalog/orders/ops carry the most PR and issue volume, which follows their size and role as catch-all buckets (35-40% of changes by design). Per bug-issue density, catalog (52), foundations (49), frontend-ui (48) and orders (47) lead; frontend-ui has the highest bugs-per-PR ratio (48 bugs vs 14 PRs) because storefront defects are filed by staff and fixed directly. Payments (11 bugs / 22 issues) and shipping (7/15) are small in volume but money-bearing; shipping has the highest comment depth per issue (57 comments / 15 issues).

## Friction
Reverts (14 revert commits, window; evidence):
| Date | Revert | Capability | What happened |
|---|---|---|---|
| 2026-09-07 | `#196` Azure.Storage.Blobs bump 12.29.2 | media | dependency bump rolled back |
| 2026-09-03 | `#8239` currency parsing inconsistency | catalog, orders, integrations | original fix landed 2026-06-29, reverted 66 days later; no re-land found in subjects (unknown if redone differently) |
| 2026-07-20 | `#8260` revert some missing images | content, frontend-ui | asset restore, mechanical |
| 2026-05-08 | `#7859` .NET 10 update, Microsoft.Data.SqlClient reverted to 6.1.1 | platform | newer SqlClient did not work |
| 2026-01-29 | `#8083` "Continue Shopping" redirect | catalog, orders | landed 01-28, reverted and re-landed 01-29, follow-up 02-04: 4 commits in 7 days |
| 2025-09-15 | `#196` LigerShark.WebOptimizer.Core bump 3.0.477 | foundations | dependency bump rolled back |
| 2025-06-12 | `#7744` revert of `#7709` one-page checkout address prefill | orders, frontend-ui | `#7709` had landed twice on 2025-05-23, then reverted |
| 2023-11-15 | two reverts ("Optimise caching in GetByIds", "repply relevant changes") | platform, unmapped | caching optimisation withdrawn |
| 2023-08-10 | `#6811` rename `#region Utils` | 6 capabilities (spillover) | cosmetic rename, revert and re-land same day: mechanical |
| 2023-05-10 | `#6690` captcha data-action | identity, content, catalog | 4 commits in 7 days; captcha v3 on news/blog comments fixed after the revert |
| 2023-03-16 | `#6471` revert of `#6444` trim IP in admin search | identity | single revert |
| 2023-01-17 | revert minor version in develop | foundations | release housekeeping |
| 2022-12-20 | removal of `BaseNopModel.CustomProperties` | messaging, foundations, admin-ui | framework API change withdrawn |

- Revert rate: 14 / 1955 commits = 0.7%. Orders (checkout flow, `#7709`, `#8083`), catalog/orders currency (`#8239`) and captcha/identity (`#6690`) are the product-behaviour reverts; the rest are dependency, refactor or housekeeping.
- Second attempts (evidence): 18 issue numbers have 3 or more commits within 30 days. Top: `#7643` WCAG contrast ratios, 9 commits over 13 days (frontend-ui, integrations, payments: a mechanical UI sweep, rule 5); `#8083` Continue Shopping (4 commits, 7 days); `#6690` captcha (4 commits, 7 days); `#7909` RFQ zero quantity (3 in 2 days); `#6893` per-request short-term caching (3 in 8 days, foundations/platform/vendors); `#7156` and `#6868` WebOptimizer/js-css paths (foundations, 3 commits same day each). Capability involvement across those 18: foundations 11, platform 8, catalog 7, orders 6, identity 6, content 6.
- Long-open work (evidence): 110 open issues, median age 396 days, 56 older than a year; 57 are `on hold / maybe wont` (parked, e.g. #6262 from 2022-05, #6428 discount minimum purchase 2022-11). 31 open PRs, median age 311 days, 15 older than a year, 29 of 31 with no human review (21 with no human comment either), none authored by core staff (oldest #6346 product publish permissions, 2022-09; #6917, #6919 from 2023-11; #7163 shipping method restrictions by state, 2024-05). All community authored.
- Most-discussed issues (comments): #6712 UPS OAuth migration 29 (shipping, platform), #7697 Redis synchronized cache not clearing all servers 22 (foundations/ops), #6614 RTL languages in PDF invoices 18, #6636 plugin.json LimitedToStores 17, #7740 high thread count mixing sync/async 15 (a mass commit, excluded from attribution), #6752 reward points use global balances 14 (unattributed), #6167 Reply-To on order notification email 13 (messaging), #6487 coupon with one-time limit usable many times 12 (discounts). Pattern (inference): the deepest threads are about shipping carriers, distributed caching and discount/reward-point correctness, not about the biggest code areas.
- Heavily reviewed PRs (human reviews, evidence): #6110 move customer fields from GenericAttribute, 33 reviews, closed (platform); #6416 refactor customer address cache, 7 reviews, merged (ops, identity); #6911 indexes on foreign keys, 6 reviews, merged (platform); #6373 multiple pictures per attribute value, 6 reviews, merged (catalog, platform, ops); #6149 order import from Excel, 8 reviews, closed. Large open or closed PRs: #8118 state-driven checkout page (+3292 lines, open, identity/orders/foundations, 2026), #7829 replace AutoMapper with Mapster (open, foundations, messaging, admin-ui), #6130 configurable pictures on catalog pages (+11k, merged).
- Closed-unmerged PRs: 330 of 599 (55%) but see Mechanical signals; by capability only the re-landed ones are visible (platform 11, foundations 10, orders 10, catalog 6, messaging 5).

## Planning attention
- `evidence`: the repo holds no ADRs, RFCs, `docs/`, `context/changes/` or `context/archive/` (only `context/map/` created by this run). `.claude/prompts/` (10 files) and `.claude/skills/` are course tooling, `CLAUDE.md` and `.10x-cli.json` are the toolkit setup; none names a product capability, so plans per capability = 0 for all 20 capabilities.
- Planning in this project lives in the GitHub issue tracker: 1697 issues, labelled by type (refactoring 412, bug 368, discussion/investigation 207, feature 191, on hold 88, enterprise/highlight 42). 12 open and 42 total issues carry `enterprise / highlight feature`; they cannot be attributed to capabilities without reading titles, which this run did not do. `unknown`.
- Prose churn: `docs` bucket has 19 commits in the window (1% of changes) and 5 PRs (3 open; #7485 plugin architecture docs, #7491 MariaDB in README): not a hot area (rule 8).

## Debt markers
- `evidence`: `git grep -w -E 'TODO|FIXME|HACK|XXX'` over 477,732 lines of .cs/.cshtml/.js/.ts finds **1 marker** (`TODO: avoid using service` in `Nop.Services/Installation/InstallSampleData.Products.cs`, ops bucket); 0 FIXME/HACK/XXX. Density 0.002 per 1000 lines everywhere; per-capability normalisation is meaningless (all zero except ops 0.02).
- Interpretation (inference): the team keeps debt in the issue tracker (the `on hold / maybe wont` label: 88 issues, 57 still open; `refactoring / source code` label 412), not in code comments. Marker counts therefore cannot rank capabilities in this repo. Other debt vocabulary at HEAD: 45 lines with `workaround|temporary fix|temporarily|obsolete` in .cs and 3 files with `[Obsolete`, counts only, not attributed.

## Mechanical signals
- Closed-unmerged PR count (330) and the closed share of 55%: nopCommerce maintainers close community PRs after re-implementing the change or declining it quickly (187 closed within a day, 136 with no comments or reviews, 34 visibly re-landed as team commits). Reading it as friction would overrate community-heavy areas. Kept out of risk ranking.
- Time-to-close of bug issues (1-5 days median): staff open issues to track their own fixes and close with the commit. Not a responsiveness signal.
- `#7643` WCAG contrast (9 commits over 13 days across frontend-ui, integrations, payments) and `#6811` region rename (revert + re-land same day, 6 capabilities): sweeping or cosmetic, not capability fragility (rules 5, 12).
- Dependency-bump reverts (`#196` Azure.Storage.Blobs, WebOptimizer, SqlClient in `#7859`): library compatibility, not feature friction; 3 of 14 reverts.
- Spillover: `#8239` revert touches catalog, orders, integrations; `#8083` touches catalog, orders, foundations; the `#6811` revert touches 6 capabilities. Attributed above to every capability touched; the product intent was orders/catalog.
- Shared cause (rule 4): the heavy PR and issue counts of foundations, platform, ops, admin-ui, catalog and orders come from their being catch-all buckets or sharing the same commits (the same `#7709`, `#6893`, `#7156` commits are counted in 3 capabilities each). Count the common cause once when combining with the history evidence.
- Community PR volume: atiq-bs23 alone opened 66 of 599 PRs; PR counts partly reflect one contributor's activity (about 11%), not core-team focus.

## Unknowns
- 930 issues and 315 PRs could not be attributed to a capability (no fix commit referencing them, or no file list was pulled). Open-issue distribution per capability is unknown (all 110 are unattributed). Reading titles would answer it; the run did not.
- Whether `#8239` (currency parsing) was re-landed in another form after the 2026-09-03 revert: no subject says so.
- Who triages the `enterprise / highlight feature` label and what the roadmap is: no planning artifact in the repo.
- Production error tracker, support forum (nopcommerce.com forum, where most real user pain is reported) and Discussions are not visible from git or the issue tracker.
- Reviews posted outside GitHub (internal tools, pair work, direct pushes by the core team, who commit directly to develop without PRs) are invisible: only 147 of 1955 counted commits carry a `(#PR)` suffix.
- Per-PR file lists were capped at the first 100 files; closed-unmerged PRs were not file-listed.

## Commands run
- `gh pr list -R nopSolutions/nopCommerce --state all --search "created:<range>" --limit 400 --json number,title,createdAt,mergedAt,closedAt,state,author,additions,deletions,labels` (9 half-year ranges); same with `--json number,reviews,comments` per quarter (20 ranges).
- `gh issue list ... --state all --search "created:<range>" --limit 1000 --json number,title,labels,createdAt,closedAt,state,comments,author` (8 ranges).
- `gh api "repos/nopSolutions/nopCommerce/pulls/N/files?per_page=100" --jq` for 119 merged/open PRs not matched by commit subject; `gh api rate_limit`.
- `node .work/parse.mjs` (git-log -> commits.json via attribute.mjs), `a1.mjs` (PR suffix, reverts), `a2.mjs`-`a4.mjs` (PR/issue attribution and tables), `a5.mjs` (git grep markers and line counts per capability), ad hoc node one-liners for second attempts and distributions.
- `git grep -I -i -c -w -E 'todo|fixme|hack|xxx'`, `git grep -I -i -E 'workaround|temporary fix|temporarily|obsolete'`, `git grep -I -l '[Obsolete'`, `git ls-files`, `ls .claude context docs`.
