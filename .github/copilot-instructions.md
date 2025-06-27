# DSLKIT Copilot Instructions

## Project Overview

DSLKIT is a parser generator library for creating Domain Specific Languages (DSLs) in C#. It provides a comprehensive toolkit for building lexers, parsers, and grammars.

## Core Commands

### Build
- `dotnet build DSLKIT/DSLKIT.sln` - Build the entire solution
- `dotnet build DSLKIT/DSLKIT/DSLKIT.csproj` - Build main library only
- `dotnet build DSLKIT/DSLKIT.Test/DSLKIT.Test.csproj` - Build test project only

### Test
- `dotnet test DSLKIT/DSLKIT.sln` - Run all tests
- `dotnet test DSLKIT/DSLKIT.Test/DSLKIT.Test.csproj` - Run tests with coverage
- `dotnet test --filter "ClassName.MethodName"` - Run specific test
- `dotnet test --logger trx --collect:"XPlat Code Coverage"` - Generate coverage reports

### Package
- `dotnet pack DSLKIT/DSLKIT/DSLKIT.csproj` - Create NuGet package

## Architecture

### Core Components

#### Lexer System (`DSLKIT/Lexer/`)
- **Lexer**: Main tokenization engine
- **ISourceStream**: Input stream abstraction with `StringSourceStream` implementation
- **LexerSettings**: Configuration for lexer behavior
- **ParenthesesCheckedStream**: Stream with parentheses validation

#### Parser System (`DSLKIT/Parser/`)
- **Grammar**: Core grammar representation with productions and rules
- **ActionAndGotoTable**: LR parser action/goto tables
- **FirstsCalculator** / **FollowCalculator**: FIRST/FOLLOW set computation
- **Production** / **Rule**: Grammar rule definitions
- **ItemSetsBuilder**: LR item set construction

#### Terminal System (`DSLKIT/Terminals/`)
- **ITerminal**: Base interface for all terminals
- **KeywordTerminal**: Keyword matching
- **IdentifierTerminal**: Identifier patterns
- **IntegerTerminal**: Numeric literals
- **StringTerminal**: String literals
- **RegExpTerminal**: Regular expression based terminals
- **CommentTerminal**: Single/multi-line comment handling
- **SpaceTerminal**: Whitespace handling

#### NonTerminal System (`DSLKIT/NonTerminals/`)
- **INonTerminal**: Base interface for non-terminals
- **NonTerminal**: Standard non-terminal implementation

#### Token System (`DSLKIT/Tokens/`)
- **IToken**: Base token interface
- **ErrorToken**: Error reporting tokens
- **EofToken**: End-of-file tokens
- Various typed tokens (Integer, Comment, etc.)

### Data Flow
1. **Input** → `ISourceStream` (typically `StringSourceStream`)
2. **Lexer** → Tokenizes input using configured `ITerminal` implementations
3. **Parser** → Processes tokens using `Grammar` with `ActionAndGotoTable`
4. **Output** → Parsed result or syntax errors

## Style Guidelines

### Naming Conventions
- **Interfaces**: PascalCase with `I` prefix (`ITerminal`, `IGrammar`)
- **Classes**: PascalCase (`Lexer`, `Grammar`, `Production`)
- **Methods/Properties**: PascalCase (`TryMatch`, `GetTokens`)
- **Private fields**: camelCase with underscore prefix (`_lexerSettings`, `_eofTerminal`)
- **Namespaces**: Follow folder structure (`DSLKIT.Lexer`, `DSLKIT.Parser`)

### Code Organization
- One class per file with filename matching class name
- Place interfaces in same namespace as implementations
- Use readonly collections for immutable data (`IReadOnlyCollection<T>`)
- Implement proper `ToString()` methods for debugging

### Error Handling
- Use `TryMatch` pattern for optional operations returning bool + out parameter
- Create `ErrorToken` instances for lexer errors with descriptive messages
- Validate inputs and provide clear error messages
- Use `IToken` implementations to represent different error states

### Testing Patterns
- Test classes end with `Tests` suffix
- Use xUnit framework with `[Fact]` attributes
- Leverage `FluentAssertions` for readable assertions
- Use `Snapshooter.Xunit` for snapshot testing
- Create test data in separate classes (see `LexerTestData`)
- Use `Dump()` extension method for debugging test output

### Dependencies
- Target `.NET 8.0`
- Test dependencies: xUnit, FluentAssertions, Snapshooter.Xunit, MSTest, Coverlet
- Minimal external dependencies in main library

## Development Status

Current implementation status:
- ✅ Lexer with comprehensive terminal types
- ✅ Parentheses checking streams  
- ✅ Grammar construction with C#
- ✅ FIRST/FOLLOW set generation
- ✅ Comprehensive unit test coverage
- 🚧 Parser implementation (in progress)

## Key Interfaces

- `ITerm`: Base for terminals and non-terminals
- `ITerminal`: Tokenization rules with `TryMatch` method
- `INonTerminal`: Grammar non-terminal symbols
- `IToken`: Lexer output tokens with position/length
- `IGrammar`: Complete grammar definition
- `ISourceStream`: Input stream abstraction

## Testing Focus Areas

When writing tests:
- Lexer behavior with various terminal combinations
- Grammar construction and validation
- FIRST/FOLLOW set calculation accuracy
- Error handling and reporting
- Token position tracking
- Parentheses validation in streams
