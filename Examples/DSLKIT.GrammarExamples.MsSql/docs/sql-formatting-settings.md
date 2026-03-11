# SQL Formatter Settings (T-SQL)

Formatter works in canonical mode only. Outside string literals/comments, extra source whitespace is normalized by formatter rules.

## Implementation Status

- Implemented now: Stage 1, Stage 2, Stage 3, Stage 4, Stage 5, Stage 6, Stage 7, Stage 8.

`[HEURISTIC]` marks options that use thresholds/conditions.

## Stage 1. Canonical Spaces And Tokens

- `keywordCase`: `Upper | Lower | Preserve`
- `spaces.afterComma`: `true | false`
- `spaces.aroundBinaryOperators`: `true | false`
- `spaces.insideParentheses`: `Never | Always`
- `spaces.beforeSemicolon`: `true | false`
- `statement.terminateWithSemicolon`: `Never | ExistingOnly | Always`
- `eof.newline`: `true | false`

## Stage 2. Query Skeleton

- `layout.indentSize`: `2 | 4 | ...`
- `layout.wrapColumn`: `80 | 100 | 120 | ...`
- `layout.newlineBeforeClause`: flags for `WITH/SELECT/FROM/WHERE/GROUP BY/HAVING/ORDER BY/OPTION`
- `layout.blankLineBetweenClauses`: `None | BetweenMajorClauses`

## Stage 3. Lists And Commas

- `lists.commaStyle`: `Trailing | Leading`
- `lists.selectItems`: `OnePerLine | WrapByWidth`
- `lists.groupByItems`: `OnePerLine | WrapByWidth`
- `lists.orderByItems`: `OnePerLine | WrapByWidth`
- `[HEURISTIC] lists.selectCompactThreshold`:
  - `maxItems`
  - `maxLineLength`
  - `allowExpressions`
- `align.selectAliases`: `true | false`

## Stage 4. FROM/JOIN/APPLY And ON Conditions

- `joins.newlinePerJoin`: `true | false`
- `joins.onNewLine`: `true | false`
- `[HEURISTIC] joins.multilineOnThreshold`:
  - `maxTokensSingleLine`
  - `breakOnAnd = true | false`
  - `breakOnOr = true | false`

## Stage 5. WHERE/HAVING Boolean Logic

- `predicates.multilineWhere`: `true | false`
- `predicates.logicalOperatorLineBreak`: `BeforeOperator | AfterOperator`
- `[HEURISTIC] predicates.inlineSimplePredicate`:
  - `maxConditions`
  - `maxLineLength`
  - `allowOnlyAnd`
- `predicates.mixedAndOrParentheses`:
  - `parenthesizeOrGroups = true | false`
  - `breakOrGroups = true | false`
  - Both options are semantics-preserving and operate only when top-level `AND` and `OR` are mixed in the same predicate.

## Stage 6. Short Queries / CASE / Inline Expressions / IN Lists

- `[HEURISTIC] shortQueries`:
  - `enabled`
  - `maxLineLength`
  - `maxSelectItems`
  - `maxPredicateConditions`
  - `applyToParenthesizedSubqueries`
  - `allowSingleJoin`
  - Current implementation is intentionally conservative: it only compacts simple single-query `SELECT ... [FROM ...] [WHERE ...]` shapes, and skips `GROUP BY`, `HAVING`, set operators, `ORDER BY`, comments, and nested subqueries inside the compacted query.
  - `applyToParenthesizedSubqueries` also applies to short subqueries inside `EXISTS (...)`, `IN (SELECT ...)`, scalar subqueries, and derived tables.

- `expressions.caseStyle`: `Multiline | CompactWhenShort`
- `[HEURISTIC] expressions.compactCaseThreshold`:
  - `maxWhenClauses`
  - `maxTokens`
  - `maxLineLength`
- `lists.inListItems`: `Inline | OnePerLine | WrapByWidth`
- `[HEURISTIC] lists.inlineInListThreshold`:
  - `maxItemsInline`
  - `maxLineLength`
- `[HEURISTIC] expressions.inlineShortExpression`:
  - `maxTokens`
  - `maxDepth`
  - `maxLineLength`
  - `forContexts`

## Stage 7. DML / DDL

- `dml.updateSetStyle`: `OnePerLine | WrapByWidth`
- `dml.insertColumnsStyle`: `OnePerLine | WrapByWidth`
- `dml.insertColumnsStartOnNewLine`: `true | false`
  - When enabled, the column list opening parenthesis starts on the next line after the `INSERT INTO ...` target.
- `dml.insertValuesStyle`: `OnePerLine | RowsPerLine | WrapByWidth`
  - `RowsPerLine` keeps each `(... )` row on a single line while splitting multi-row `VALUES` lists across lines.
  - Applies to single-row and multi-row `INSERT ... VALUES ...`; `OUTPUT` clauses keep their inline form and stay attached to the `INSERT` header.
- `ddl.createProcLayout`: `Expanded | Compact`

## Stage 8. Comments And Semantic Safety

- `comments.preserveAttachment`: `true` (default)
- `comments.formatting`: `Keep | ReflowSafeOnly`

## Precedence Rules

1. Apply Stage 1 canonical spacing first.
2. Inline-vs-multiline heuristics choose node layout first, then internal style rules are applied.
3. `align.*` rules run last inside a formatted node.
