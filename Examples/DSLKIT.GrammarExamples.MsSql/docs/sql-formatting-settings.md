# SQL Formatter Settings (T-SQL)

Formatter works in canonical mode only. Outside string literals/comments, extra source whitespace is normalized by formatter rules.

## Implementation Status

- Implemented now: Stage 1, Stage 2, Stage 3, Stage 4, Stage 5, Stage 6, Stage 7, Stage 8.

`[HEURISTIC]` marks options that use thresholds/conditions.

## Stage 1. Canonical Spaces And Tokens

- `keywordCase`: `Upper | Lower | Preserve`
- `spaces.afterComma`: `true | false`
- `spaces.aroundBinaryOperators`: `true | false`
- `spaces.insideParentheses`: `Never | Always | Smart`
- `spaces.beforeSemicolon`: `true | false`
- `statement.terminateWithSemicolon`: `Never | ExistingOnly | Always`
- `eof.newline`: `true | false`

## Stage 2. Query Skeleton

- `layout.indentSize`: `2 | 4 | ...`
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
  - `breakOn = AndOnly | AndOr`

## Stage 5. WHERE/HAVING Boolean Logic

- `predicates.multilineWhere`: `true | false`
- `predicates.logicalOperatorLineBreak`: `BeforeOperator | AfterOperator`
- `[HEURISTIC] predicates.inlineSimplePredicate`:
  - `maxConditions`
  - `maxLineLength`
  - `allowOnlyAnd`
- `[HEURISTIC] predicates.parenthesizeMixedAndOr`:
  - `mode = None | Minimal | AlwaysForOrGroups`

## Stage 6. CASE/Expressions/Subqueries/IN Lists

- `expressions.caseStyle`: `Multiline | CompactWhenShort`
- `[HEURISTIC] expressions.compactCaseThreshold`:
  - `maxWhenClauses`
  - `maxTokens`
  - `maxLineLength`
- `subqueries.indentStyle`: `Indented`
- `lists.inListItems`: `Inline | OnePerLine | WrapByWidth`
- `[HEURISTIC] lists.inlineInListThreshold`:
  - `maxItemsInline`
  - `maxLineLength`
- `[HEURISTIC] expressions.inlineShortExpression`:
  - `maxTokens`
  - `maxDepth`
  - `maxLineLength`
  - `forContexts`

## Stage 7. DML/DDL

- `dml.updateSetStyle`: `OnePerLine | WrapByWidth`
- `dml.insertColumnsStyle`: `OnePerLine | WrapByWidth`
- `ddl.createProcLayout`: `Expanded | Compact`

## Stage 8. Comments And Semantic Safety

- `comments.preserveAttachment`: `true` (default)
- `comments.formatting`: `Keep | ReflowSafeOnly`
- `preserve.stringLiterals`: `true` (always)

## Precedence Rules

1. Apply Stage 1 canonical spacing first.
2. Inline-vs-multiline heuristics choose node layout first, then internal style rules are applied.
3. `align.*` rules run last inside a formatted node.
