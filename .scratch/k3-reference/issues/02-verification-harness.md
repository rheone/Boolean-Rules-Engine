# 02: Verification harness for the reference

**What to build:** An automated check that keeps the documentation honest. It extracts the truth tables, evaluation tables and canonical forms from the Markdown reference and verifies them against an independent Strong K3 oracle over all T/F/U inputs (operand counts up to 4 for variadic operations), reports mismatches with file and line, checks that every operation has exactly one primary category, an arity, domains and a Kind, and validates relative links. Reuse the existing K3 test oracle where it fits; place the check where the repository already runs its gates (a test or a documented script) so CI can run it. Choose the smallest design and record the choice in the ticket Comments.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] A deliberately wrong table, canonical form or link in a fixture is detected and reported with file and line
- [ ] The harness is runnable locally and wired into the gates the repo already uses (or documented why not)
- [ ] It adds no new package dependency without a recorded reason
- [ ] It passes on the (initially empty) reference and on the skeleton from ticket 04

Source: the Phase 8 validation list in the brief. See also [spec](../spec.md).
