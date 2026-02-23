# Chapter 2: Recoverable Missing Equals (`key value`)

This chapter describes the recovery idea used in the INI example for a common typo:

- expected: `port=5432`
- typed by user: `port 5432`

## 1. Why Recovery Matters

In configuration files, this typo is frequent and easy to understand.
Failing hard on the first missing `=` can be noisy in editor scenarios.

The example demonstrates a lightweight strategy:

- accept the line
- keep parsing the rest of the document
- emit a diagnostic that tells what was recovered

## 2. Grammar Mechanism

The grammar includes two property productions:

```text
Property -> Word "=" Value
Property -> Word Value
```

The second rule is the recovery rule.
It allows parsing to continue when `=` is absent.

## 3. Detection In DOM Builder

After parsing, the code inspects `Property` node shape:

- 3 children (`Word`, `"="`, `Value`) -> normal property
- 2 children (`Word`, `Value`) -> recovered property

When recovered, the builder sets:

- `IniProperty.IsRecoveredFromMissingEquals = true`
- diagnostic message like: `Recovered missing '=' after key 'port'.`

## 4. Example

Input:

```ini
[database]
host=localhost
port 5432
enabled=true
```

Result:

- parse succeeds
- `database.port` is produced with numeric value `5432`
- one diagnostic is emitted for the missing equals sign

## 5. Tradeoff And Intent

This is not a universal error-recovery framework.
It is a focused, explicit grammar choice for one well-known user mistake.

That is exactly why it is useful in a tutorial:

- behavior is transparent
- implementation is small
- students can extend the same pattern for other recoverable cases
