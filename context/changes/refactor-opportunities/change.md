---
change_id: refactor-opportunities
title: Rank checkout-flow refactor opportunities, target architecture and order
status: implementing
created: 2026-10-18
updated: 2026-10-19
archived_at: null
---

## Notes

We have an analysis of the module that documents technical debt and structural risks: @context/changes/checkout-flow-analysis/research.md . This change answers the question that the previous analysis deliberately left open:

WHICH of those issues are worth fixing, what the target architecture should look like, and in what order they should be addressed.

We investigate each documented issue in both the code and project history, then organize the findings as refactor opportunities.

The change proceeds in stages:

exploration → decision and planning → implementation

During the exploration phase, no refactoring takes place and no decisions are made.

Exploration output: the research.md file for this change, ending with a ranked list of options and their trade-offs.

I will review that report first. The decision about what to implement is made during the planning phase, and refactoring begins only after an agreed plan has been established.
