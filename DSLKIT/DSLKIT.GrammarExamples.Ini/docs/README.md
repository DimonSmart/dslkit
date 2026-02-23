# INI Grammar Example Mini-Docs

This folder documents the `DSLKIT.GrammarExamples.Ini` example as a small, book-style guide.

## Contents

- [Chapter 1: Parsing INI with DSLKIT](chapter-01-ini-parsing-basics.md)
- [Chapter 2: Recoverable Missing Equals (`key value`)](chapter-02-recoverable-missing-equals.md)

## Goal Of This Example

The INI example is intentionally minimal and academic:

- tiny and readable grammar
- clear lexer -> parser -> DOM pipeline
- simple value typing (`string`, `number`, `bool`)
- one practical recovery rule for a common typo

Start with Chapter 1, then read Chapter 2 for the recovery behavior details.
