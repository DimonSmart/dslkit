# Chapter 1: Parsing INI with DSLKIT

This chapter explains the core design of the INI grammar example:

- where tokens come from
- how the grammar is structured
- how parse results become a convenient document model

All code discussed here is in `Examples/DSLKIT.GrammarExamples.Ini/IniGrammarExample.cs`.

## 1. Input Model

The parser accepts classic INI-like input:

```ini
[database]
host=localhost
port=5432
enabled=true
```

The supported concepts are intentionally small:

- section headers: `[section_name]`
- properties: `key=value`
- value categories:
  - quoted string (`"text"` or `'text'`)
  - number (`5432`, `3.14`)
  - word (`localhost`, `true`, `cache_dir`)

## 2. Lexer Stage

The example defines a small terminal set:

- `Word`
- `Number`
- `QuotedString`
- `NewLine`
- single-line comments: `;...` and `#...`
- spaces (only `' '` and `'\t'`, via `CustomSpaceTerminal`)

During parsing, `Space` and `Comment` tokens are filtered out, while `NewLine` is kept because line structure matters for INI.

## 3. Grammar Stage

The grammar is line-oriented:

- `Document -> LineList`
- `LineList -> Empty | Line | LineList Line`
- `Line -> NewLine | SectionLine | PropertyLine`

Section and property rules:

- `SectionHeader -> "[" Word "]"`
- `Property -> Word "=" Value`

Value rules:

- `Value -> Number | QuotedString | Word`

This makes the grammar compact and easy to reason about in a learning context.

## 4. Parser Output

`ParseDocument(string source)` returns `IniParseOutput`, which includes:

- `ParseResult` (success/error from LR parser)
- `Document` (`IniDocument?`)
- `Diagnostics` (non-fatal recovery notes)

The document model is:

- `IniDocument` -> list of `IniSection`
- `IniSection` -> `Name` + list of `IniProperty`
- `IniProperty` -> `Key`, typed `Value`, and recovery marker

## 5. Typed Values

Values are normalized into `IniValue`:

- `IniValueKind.String`
- `IniValueKind.Number`
- `IniValueKind.Boolean`

Parsing order is:

1. quoted string
2. boolean
3. number
4. fallback string

This order keeps behavior predictable for common config files.

## 6. Why This Example Is Useful

This INI grammar is a clean "hello world" for DSLKIT because it demonstrates:

- terminal design with regexes
- grammar composition with `GrammarBuilder`
- running lexer + parser together
- converting parse tree to a practical DOM

With only a few productions, it still shows a full parser pipeline end to end.
