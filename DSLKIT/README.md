# DSLKIT

DSLKIT is a .NET parser toolkit for building and running LR-family parsers.
The implementation in this repository was originally aligned with the tutorial by Stephen Jackson and then extended with tests and visualizers.

## Documentation

- Documentation index: [docs/README.md](docs/README.md)
- Cleaned and corrected tutorial: [docs/lalr1-tutorial-cleaned.md](docs/lalr1-tutorial-cleaned.md)

## Implementation Status

The following tutorial stages are implemented in code and covered by tests:

- [x] Grammar and production model
- [x] Lexer and token stream
- [x] Item set construction
- [x] Translation (transition) table generation
- [x] Extended grammar generation
- [x] FIRST set calculation
- [x] FOLLOW set calculation
- [x] Action/Goto table generation
- [x] Shift-reduce syntax parsing
- [x] Parse tree / AST helpers
- [x] Text and Graphviz visualizers

Key test suites:

- `DSLKIT.Test/ParserTests/SetBuilderTests.cs`
- `DSLKIT.Test/ParserTests/ActionAndGotoTableBuilderTests.cs`
- `DSLKIT.Test/ParserTests/FirstsCalculatorTests.cs`
- `DSLKIT.Test/ParserTests/FollowCalculatorTests.cs`
- `DSLKIT.Test/ParserTests/SyntaxParserTests.cs`

## Build And Test

```powershell
dotnet build DSLKIT.sln
dotnet test DSLKIT.sln
```

## Solution Layout

- `DSLKIT/` - core lexer/parser implementation
- `DSLKIT.Test/` - unit tests
- `DSLKIT.Visualizers/` - table/state visualizers and exporters
- `DSLKIT.Visualizers.Tests/` - visualizer tests
