# SQL Grammar And Formatter Notes

This folder documents `DSLKIT.GrammarExamples.MsSql` and the SQL formatter that uses the same parse tree.

## Formatting Flow

1. `ModernMsSqlFormatter.TryFormat(...)` calls `ModernMsSqlGrammarExample.ParseDocument(...)`.
2. Document parsing splits the input into SQL batches and control lines such as `GO` and SQLCMD commands.
3. Each batch is parsed with `ModernMsSqlGrammarExample.ParseBatch(...)`.
4. If any batch fails, formatter returns `SqlFormattingResult.Failure(...)` with parser error text.
5. If parsing succeeds, formatter runs `SqlFormattingVisitor` over each batch parse tree.
6. `SqlFormattingVisitor` emits formatted SQL through `IndentedSqlTextWriter`.

## Visitor Pattern Usage

- `ISqlParseTreeVisitor` defines a parse-tree visitor contract.
- `SqlParseTreeVisitorBase` contains dispatch logic for:
  - `NonTerminalNode`
  - `TerminalNode`
- `SqlFormattingVisitor` extends the base visitor and contains SQL formatting rules:
  - keyword casing
  - clause line breaks
  - spacing and punctuation rules
  - scoped indentation for selected non-terminals

This keeps formatting logic separated from grammar construction and parser internals.

## Class Hierarchy

- `ModernMsSqlFormatter` (facade)
  - uses `SqlFormattingOptions`
  - returns `SqlFormattingResult`
  - creates `SqlFormattingVisitor`
- `SqlFormattingVisitor : SqlParseTreeVisitorBase : ISqlParseTreeVisitor`
  - writes output via `IndentedSqlTextWriter`
- `IndentedSqlTextWriter`
  - appends text to `StringBuilder`
  - exposes `PushIndent()` returning `IDisposable`
  - nested `using` scopes increase/decrease indentation automatically

## Future Extension Points

- `SqlFormattingOptions` is the place for new options (line wrapping, alignment, heuristics).
- `SqlFormattingVisitor` is the place for grammar-aware formatting rules per node/token pattern.

## SQL Formatting Settings

- Full settings catalog and staging plan: [sql-formatting-settings.md](sql-formatting-settings.md)
- Current implementation in this repository covers Stage 1-8.
