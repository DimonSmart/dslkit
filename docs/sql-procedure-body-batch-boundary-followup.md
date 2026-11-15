# Follow-up proposal: explicit procedure-body boundaries per SQL batch

**Status:** design proposal, not part of stage 3. Do not implement without an explicit compatibility decision.

## Problem

The SQL grammar currently permits `CREATE/ALTER PROCEDURE` and `CREATE TRIGGER`
as ordinary statements in `Script → StatementList`. At the same time, their
unwrapped bodies use another `StatementList`. The LR parser therefore cannot
distinguish the next statement of a procedure from the next outer statement.
The stage-3 report shows the exact 172 S/R collisions and the failed attempts
to remove or rename them without changing the accepted SQL.

## Required decision

Choose the semantics of a procedure / trigger body without `BEGIN/END`:

1. **SQL Server batch-scoped mode:** the definition consumes statements through
   the end of the current preprocessed batch. `GO` remains a preprocessor
   boundary, not an LR terminal. A subsequent definition or ordinary statement
   must be placed in a different batch.
2. **Compatibility mode:** keep the current permissive parse behavior of
   multiple definitions and trailing statements in one batch. Document the
   ambiguous boundary and retain existing reductions until a deterministic
   compatibility rule has been specified.

Do not silently change the default from the current permissive mode. If strict
batch scoping is introduced, prefer an explicitly selected mode and test both
modes against real existing scripts.

## Design work

- Identify every consumer of `CreateProcBodyBlock` (procedures and triggers)
  and every grammar entry point which accepts a `Script`.
- Design a **grammar-level** batch-entry alternative or equivalent contextual
  parse path for the strict mode, while preserving the existing preprocessing of
  `GO`. Avoid changing the LR algorithm, lexer and default grammar implicitly.
- Define parse-tree ownership of all body statements and separators, including
  initial and trailing `;`. Document how the format visitor will see them.
- Account for nested `IF/ELSE`, `WHILE`, `TRY/CATCH`, `BEGIN/END` and
  native/CLR procedure variants. In strict mode, close only at actual batch
  boundaries, not at arbitrary semicolons or `END` tokens.
- Decide whether mixed-mode documents are supported and how a caller opts in.
  Do not add complex intermediary serialized formats.

## Verification requirements

Run both old and new modes on a corpus of real SQL scripts. Add positive and
negative tests for a procedure followed by a second SQL statement in the same
batch, the same script split by `GO`, multiple definitions per batch, and
unwrapped bodies with several separators. Assert parse-tree ownership, not
just parse success. Check every formatter termination/layout mode and full
`Parse → Format → Parse → Format` stability.

Generate complete conflict diagnostics and compare **multisets** keyed by
kind, lookahead, existing/incoming productions and resolution. Do not compare
LR-state numbers, add `Resolve.Shift`/`Resolve.Reduce` merely to suppress the
report, or claim success from a drop in the total while new unresolved conflict
records appear.

## Acceptance

The chosen strict mode has an explicit and testable definition of body end,
preserves well-formed SQL Server batch semantics, does not introduce new
unresolved conflict families, and has no regressions in its declared scope.
Legacy mode must retain its published behavior unless a separate breaking
change is approved. Report remaining conflicts honestly.
