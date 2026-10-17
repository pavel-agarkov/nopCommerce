# Evidence: people

Window 2022-01-01..2026-10-16 (effective start 2021-12-29), 1970 non-merge commits. Ownership counts use 1937 commits = 1970 minus 15 mass commits minus 18 sweeping commits (both lists kept out of ownership; mass authors: Koshelev 8, Anokhin 5, Kulagin 1, Romanov 1; sweeping authors: Koshelev 6, Kulagin 6, Anokhin 4, Romanov 1, Finch 1). A commit counts once per capability it touches, via `capabilities.tsv` + `attribute.mjs`; lockfiles/media/min/bin/obj files dropped. Attribution is an inference from path heuristics.

**Identities (evidence):** 112 distinct people after merging by e-mail, then by obvious name variants (skoshelev = Sergey Koshelev, AlexeyA = Alexey Anokhin, RomanovM = Romanov Maksim, three Atiqur/Rickard/Fuad variants, etc.). The contract's "~100" holds approximately. 71 people have exactly 1 counted commit; 89 have 2 or fewer. 9 people have 20+ commits.

**Team baseline (evidence):** four long-running core contributors, each active in 20-21 of 21 quarters: Sergey Koshelev 720 commits (37.2%), Alexey Anokhin 373 (19.3%), DmitriyKulagin 304 (15.7%), Romanov Maksim 186 (9.6%). Together 81.7% of all counted commits. Everyone else is at most 60 commits (next: Atiqur Rahman Foyshal 60, 2025-01..2026-10; AndreiMaz 27, 2022-12..2026-10). The nopCommerce-staff reading of the core four comes from the contract (inference, e-mail domain/role not re-checked here); the tail is community. The team is larger than 3, so concentration is judged against these global shares, not against "few humans". Because the core four are 82% of everything, "core carries 80%+ of a capability" is the baseline too; the signal is a person's share vs their global share.

## People per capability

`n` = counted commits touching the capability; `core%` = share by the core four; `12m` = commits since 2025-10-17 (distinct people); topics are the most frequent subject words and are approximate.

| Capability | n | People ever | core% | 12m (people) | Last commit | Main people (share, period, topics) |
|---|---|---|---|---|---|---|
| foundations | 642 | 40 | 85 | 122 (9) | 2026-10-14 | Koshelev 41% (2022-2026; versions, refactoring); Anokhin 21% (admin/product validation, search, locales); Kulagin 16% (package/library updates); Romanov 7% (routing) |
| platform | 541 | 50 | 83 | 99 (13) | 2026-10-17 | Koshelev 34% (migrations, plugin removals); Anokhin 22% (product/setting locales); Kulagin 18% (library/packages); Romanov 9% (installation, translations) |
| ops | 423 | 31 | 83 | 91 (8) | 2026-10-13 | Koshelev 45% (refactoring, export/import, tests); Anokhin 18% (settings, search); Kulagin 15% (packages); Romanov 5% |
| catalog | 322 | 41 | 74 | 52 (11) | 2026-10-16 | Koshelev 33% (product edit, bulk, refactoring); Anokhin 21% (products, search, WCAG); Kulagin 11% (attributes, video, price); Romanov 9% (price, display filter; last 2025-03-20) |
| admin-ui | 288 | 27 | 85 | 58 (5) | 2026-09-29 | Koshelev 32%; Anokhin 26%; Kulagin 19% (editor, datatables, summernote); Romanov 8% |
| orders | 250 | 39 | 77 | 43 (7) | 2026-10-05 | Koshelev 34% (address, checkout, methods); Anokhin 22% (order reports, optimization); Kulagin 11% (wishlist, returns); Romanov 9% |
| identity | 238 | 26 | 80 | 40 (5) | 2026-10-15 | Koshelev 34%; Kulagin 19% (login, phone login); Anokhin 19% (password, validation); Romanov 7% (authentication) |
| frontend-ui | 221 | 14 | 89 | 45 (6) | 2026-10-06 | Anokhin 43% (password, themes, WCAG, RTL; 2022-2026); Koshelev 21% (last 2026-03); Kulagin 16%; Romanov 8% (last 2025-12) |
| messaging | 200 | 24 | 82 | 41 (7) | 2026-10-16 | Koshelev 35% (Omnisend plugin); Kulagin 19% (Brevo, tracking events; since 2022-11); Anokhin 18% (since 2023-10); Romanov 10% (Sendinblue to Brevo) |
| content | 156 | 19 | 83 | 35 (8) | 2026-10-15 | Koshelev 33%; Anokhin 28% (WCAG, forum, menu); Kulagin 16%; Romanov 6% |
| payments | 88 | 12 | 85 | 9 (3) | 2026-08-25 | Koshelev 40%; Romanov 28% (PayPal Commerce); Anokhin 13% (last 2025-09); Kulagin 5% |
| integrations | 74 | 10 | 88 | 36 (7) | 2026-10-02 | Koshelev 49% (RFQ/quotes, 24 of his 36 in last 12m); Anokhin 15% (request-quote UI, last 2025-11); Romanov 12% (Zettle); Kulagin 12% (PunchOut) |
| tax | 72 | 10 | 88 | 13 (4) | 2026-10-05 | Koshelev 47%; Anokhin 15% (last 2025-09); Romanov 14% (Avalara); Kulagin 11% |
| localization | 69 | 18 | 71 | 9 (3) | 2026-09-22 | Koshelev 38%; Anokhin 16% (currency, last 2025-09); Kulagin 9%; Romanov 9% (last 2025-08) |
| media | 68 | 12 | 81 | 15 (5) | 2026-09-07 | Koshelev 46% (image/region); Kulagin 24% (SkiaSharp, images); Anokhin 9% |
| shipping | 64 | 11 | 80 | 15 (5) | 2026-09-28 | Koshelev 48%; Anokhin 19% (pickup; last 2025-12); Kulagin 9%; Atiqur Foyshal 8% (2025-01..2026-01) |
| discounts | 48 | 12 | 77 | 8 (3) | 2026-10-16 | Koshelev 38% (last 2026-04); Anokhin 23% (last 2025-09); Kulagin 13% (price lists); Atiqur Foyshal 4% (price-list adjustment, latest 2026-10-16) |
| vendors | 36 | 7 | 92 | 9 (2) | 2026-08-25 | Koshelev 47%; Anokhin 25%; Kulagin 11% (last 2023-02); Romanov 8% (last 2024-04) |
| search | 29 | 5 | 93 | 6 (3) | 2026-07-27 | Kulagin 38% (filter levels, ChatGPT feed); Koshelev 28%; Anokhin 28% (last 2026-03) |
| ai (low crit.) | 11 | 3 | 82 | 8 (3) | 2026-07-07 | Koshelev 82% (since 2025-07); Nielsen 1; Chadha 1 |
| docs (low) | 19 | 7 | 11 | 2 (2) | 2026-10-17 | AndreiMaz 47% (README/license), Mari 26% (2022); last commit is the 10x skills add (not product work) |

Not shown: `unmapped (deleted plugins)` n=51, core 84% (Koshelev 19, Romanov 11, Anokhin 8): removal/replacement of ShipStation, EasyPost, AccessiBe, etc.

## Concentration

- **Baseline (evidence):** Koshelev 37.2% of all counted commits; in 17 of 19 listed capabilities he is at 32-49% (inference: this is simply the baseline, he is the most active person everywhere).
- **Above baseline** (person share clearly over their global share; evidence for the numbers, inference for the reading):
  - ai: Koshelev 82% of 11 commits, small n; new capability since 2025-07.
  - integrations: Koshelev 49%, and 24 of his 36 commits are in the last 12 months (RFQ/quotes work).
  - shipping 48%, tax 47%, vendors 47%, media 46%, ops 45% (n=423, the largest after the catch-alls), foundations 41%: Koshelev, 8-12 pts above his baseline.
  - frontend-ui: Anokhin 43% vs his 19.3% baseline, the largest relative deviation. Storefront WCAG/themes/RTL work is mostly his.
  - payments: Romanov 28% vs 9.6% baseline (PayPal Commerce). Next to Koshelev 40%, payments rests on two people; Anokhin last touched it 2025-09, Kulagin 4 commits.
  - search: Kulagin 38% vs 15.7% baseline (filter levels, ChatGPT feed), only 5 people ever.
  - media: Kulagin 24% vs 15.7%.
- **Smallest crowds (people ever):** ai 3, search 5, vendors 7, tax 10, integrations 10, shipping 11, payments 12, media 12, discounts 12. In all of them the core four make up 77-93% of commits. No capability has been touched by only one person ever.
- **Tail vs core:** community contributors matter by count in catalog (84 tail-author commits, 26%), platform (94), foundations (97), ops (71), orders (58). These are mostly 1-commit contributors (71 of 112 people have one commit); not a reliable owner pool.
- **Mechanical (false-signals 7/12):** foundations/platform are inflated by version, package and library bumps (Kulagin, Koshelev: "version", "packages", "latest" are top subject words). Those are dependency churn, not domain ownership.

## Recent activity and departures

- **Core four, last 12 months** (since 2025-10-17): Koshelev 187 (95 in the last 6 months), Kulagin 76 (51 in 6m), Anokhin 62 (16 in 6m, last commit 2026-08-11), Romanov 20 (11 in 6m, last 2026-10-12). Trend (inference): Kulagin and Koshelev are accelerating; Anokhin's pace fell and he has no commit in the ~2 months before the window end (cause unknown).
- **Last touches per core person in a capability** older than 12 months: Romanov in catalog (2025-03), vendors (2024-04), localization (2025-08), frontend-ui (2025-12); Anokhin in payments (2025-09), tax (2025-09), discounts (2025-09), localization (2025-09), integrations (2025-11); Kulagin in vendors (2023-02) and shipping/localization (2025-12). Evidence is the last dated commit; it says nothing about intent.
- **Non-core people with 10+ commits who have gone quiet >12 months:** Rickard von Haugwitz 26 (last 2023-12), Jaber Kibria 23 (2022-11), kirilchik 21 (2023-05), Jason Finch 13 (2025-04), Parshan Naderi 11 (2024-06), Fuad Hasan 10 (2025-02).
- **Non-core still active in the last 12 months:** Atiqur Rahman Foyshal 16 (since 2025-01, 60 total; shipping, discounts, media topics), AndreiMaz 5 (docs, 27 total since 2022-12), Jan Nielsen 5 (since 2024-12), plus ~19 one-or-two-commit contributors.

## Agent co-authorship

2 of 1937 counted commits carry a Claude Opus 4.8 co-author trailer (Andriy Kvasnytsya): one in catalog, one in foundations, 0.1% overall, 0 elsewhere. A broader text match also caught "Claude De-Tchambila", which is a human name, not an agent. Context only; no agent authors exist.

## Implications

- **Whom to ask (inference from commits):**
  - Catalog, orders, identity, ops, platform, foundations, admin-ui, tax, shipping, integrations (RFQ), AI: Sergey Koshelev (broadest and most active). Second voice: Anokhin for catalog/orders/admin (product validation, search), Kulagin for identity (login/phone login), packages, media.
  - Payments (PayPal Commerce): Romanov first, then Koshelev. Check also the Misc.PayPalCommerce plugin PRs by Romanov before changing flows.
  - Storefront UI, themes, accessibility, RTL: Alexey Anokhin.
  - Search, filter levels, ChatGPT feed: Kulagin.
  - Messaging (Omnisend/Brevo): Koshelev for Omnisend, Kulagin for Brevo/tracking events, Romanov for the Sendinblue-to-Brevo move.
  - Docs and contributor guidance: AndreiMaz.
- **Before changing a capability someone may already have tried:** search GitHub PR/issue history (`#NNNN` subjects) by these names; the discussion evidence file is the right place for the PR content.
- **Single-point risk (inference):** payments, tax, vendors, search and ai rely on 2-3 people each, the core four dominating them; ai on Koshelev alone in practice (9 of 11). Anokhin slowing and Romanov at ~10% of volume make payments and tax the first places where reduced availability would leave Koshelev alone.

## Unknowns

- Who is still on the nopCommerce staff, formal ownership, CODEOWNERS or review assignment: not in git history (CODEOWNERS not checked here; it would be an extra read).
- Whether Anokhin's slowdown (last commit 2026-08-11) is a departure, leave or a change of focus: unknown.
- Whether the core four really are all employees and how Atiqur Rahman Foyshal is engaged (frequent contributor since 2025-01): unknown; git only shows names.
- Squash/merge practice on GitHub PRs may hide co-authors and reviewers; reviewer roles are not covered here.
- Topic groups are subject-word frequencies, not reviewed per commit.

## Commands run

1. `grep '^@' .work/git-log.txt | awk ...` (author/email frequency).
2. `node .work/people.mjs` (parse git-log.txt, merge identities, attribute each commit via `attribute.mjs` `cap()`, write `.work/people-commits.json`).
3. `node .work/people2.mjs` (per-person totals, active quarters, first/last dates, core-vs-tail).
4. `node .work/people3.mjs` (per-capability shares, 12-month activity, topic words; output `.work/people-caps.json`).
5. Ad-hoc `node -e` checks: agent trailer commits, mass/sweeping authors, 12m/6m activity, departed contributors.
