## Project Coding Standards

### Comments
- Avoid obvious comments that merely repeat what the code does.
- Only add comments when they add real value explaining "why", not "what".
- Write all comments in English.
- If you need to comment a variable, consider if a better variable name would eliminate the need.
- **Self-documenting code**: Good code documents itself through clear naming, simple structure, and obvious intent. Comments should explain "why" not "what". If you need to explain "what" the code does, consider refactoring for clarity instead.
- **XML Documentation**: Only document public APIs. For public APIs, standard XML comments documenting parameters and return values are acceptable even if they seem obvious - they serve as official API documentation. For internal/private methods, avoid obvious XML comments that repeat method names or parameter types. Focus on business value, edge cases, or non-obvious behavior.
- **Property XML Comments**: If a class uses XML comments for its properties, document all properties or remove the comments entirely. Prefer descriptive property names so additional comments are rarely needed.
- **Complex logic documentation**: For complex algorithms, regex patterns, and bit manipulations, add short comments that explain intent, constraints, and edge cases.
- **Examples of obvious comments to avoid**:
  ```csharp
  // BAD - obvious comments
  catch (JsonException)
  {
      // If parsing fails, return null
      return null;
  }

  // Increment counter
  counter++;

  // Check if user is null
  if (user == null) return;

  /// Modern implementation using new API instead of obsolete methods
  public void ProcessData() { }

  /// Updates user settings with new values
  public void UpdateUserSettings(UserSettings settings) { }

  // GOOD - valuable comments
  // Using exponential backoff to avoid overwhelming the API during retries
  await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));

  // WorkAround: Azure API sometimes returns stale data, force refresh after writes
  cache.InvalidateKey(userId);

  /// <summary>
  /// Validates credit card using Luhn algorithm. Returns false for test cards in development.
  /// </summary>
  public bool ValidateCreditCard(string number) { }
  ```

### Control Flow
- Minimize use of `if…else`; prefer guard clauses and early `return`.
- Avoid deep nesting of `if` statements; use early exits to keep methods flat.
- Create router methods that delegate to appropriate specialized services based on conditions.
- For simple guard clauses, it's acceptable to omit braces and keep the statement on a single line (e.g., `if (items.Count == 0) return;`).

### JSON Serialization
- Use `System.Text.Json` for all JSON (de)serialization.

### Optional Project-Specific Presets
- **MudBlazor**: MudDialog visibility property is `Visible`, not `IsVisible`.

### C# Conventions
- Use PascalCase for classes, methods, properties, and public fields.
- Use camelCase for parameters, local variables, and private fields.
- Prefix interfaces with `I` (e.g., `IUserService`).
- Use primary constructors when appropriate (C# 12+).
- Prefer `System.Threading.Lock` over `object` for synchronization.
- **Parameter naming**: Parameter names must match their semantic purpose in the method. Avoid generic names that don't reflect the actual data type or business meaning.
- **ID parameters MUST be descriptive**: Never use a bare `id`. Always use `<entity>Name + Id` that reflects domain meaning.
  - Bad: `GetUser(Guid id)`
  - Good: `GetUser(Guid userId)`
  - Bad: `DeleteOrder(Guid id, Guid id2)`
  - Good: `DeleteOrder(Guid orderId, Guid performedByUserId)`
  - Bad: `Load(Guid id)`
  - Good: `Load(Guid invoiceId)`
- When renaming parameters, update usages including `nameof(...)`, logging placeholders, and validation messages.
- Return `IReadOnlyCollection<T>` instead of `List<T>` when modification isn't required.
- **MCP Server Tools exception**: For MCP Server tool parameters exposed via `[McpServerTool]` attribute, use `snake_case` naming (e.g., `model_name`, `connection_name`) to follow MCP protocol conventions and ensure compatibility with MCP clients.

### Error Handling
- Return empty collections (`[]`) instead of `null` when no data is available.
- Log errors with structured logging using `ILogger`.
- **Fail-fast**: Do not hide bugs with default values or silent fallbacks for required data.
- For invalid input, throw clear exceptions:
  - `ArgumentNullException` for `null` values when a runtime null check is intentionally required.
  - `ArgumentException` for invalid values/formats,
  - `ArgumentOutOfRangeException` for out-of-range values,
  - `InvalidOperationException` for invalid object/application state.
- **Design by Contract**: Validate required invariants and configuration at startup; after initialization, treat required values as valid.
- If absence is intentional, model it explicitly with nullable types (`T?`) and handle it explicitly.
- Do not use placeholder fallbacks for required values (for example, empty strings, `ToString()`, or dummy names).

### Architecture
- Follow Clean Architecture principles with clear separation of concerns.
- Use dependency injection for service registration and resolution.
- Organize code into logical layers appropriate for the project type (for example: API, Services, Models, Shared).
- Follow DRY principle: extract duplicated logic into helper classes or services.
- Create specialized services for complex operations instead of embedding logic in multiple places.
- **Method overloads**: Don't create overloads that merely delegate to another signature — keep one universal version and replace calls accordingly.
- **Avoid parameter pass-through methods**: If a method only receives parameters to create an object and return it at the end, consider removing the method and creating the object directly in the calling code. Methods should perform meaningful logic, not just pass parameters to constructors.
- **Don't return input parameters**: Avoid returning the same parameters that were passed as input to the method. It's redundant and adds no value. If a method receives an ID and needs to return it unchanged, the caller already has that ID.
- **Avoid tuple returns**: Returning tuples is often a sign of poor design. Consider creating a dedicated class/record or refactoring the method to have a single responsibility. If you find yourself returning multiple unrelated values, the method is likely doing too much.
- **Factory methods exception**: Factory methods like `CreateFromDomainModel` or similar that combine multiple operations for code readability and provide meaningful naming are acceptable and improve code clarity.

### Modern C# Features
- Use target-typed expressions where appropriate (e.g., `return [];`).
- Leverage nullable reference types (`<Nullable>enable</Nullable>`) for null safety.
- Use pattern matching and modern C# syntax when it improves readability.
- **Nullability is part of the contract**:
  - If a value can be `null`, it must be explicitly declared as nullable (`string?`, `T?`) and handled in code.
  - If a parameter is non-nullable, treat it as required and do not add repetitive manual null guards.
  - Do not use silent null fallbacks for required values.
- Validate semantic correctness separately from nullability (e.g., empty/whitespace strings, invalid ranges, invalid formats).

### Async
- Suffix asynchronous methods with `Async`.
- Accept `CancellationToken` in async public methods and pass it through to dependencies.
- Avoid `.Result` / `.Wait()` in async code; use `await`.
- Avoid `async void` except for UI event handlers.
- Do not start fire-and-forget tasks without explicit handling/logging.

### Refactoring
- **No compatibility-by-default**: When refactoring, do not add backward-compatibility layers, wrappers, fallback paths, obsolete overloads, or duplicate APIs unless compatibility is explicitly requested.
- Prefer a clean replacement over "temporary" compatibility code that preserves old behavior just in case.
- If compatibility is required, make it explicit in naming and comments (what is preserved, for whom, and when it can be removed).

### Change Discipline
- Prefer the smallest correct change: modify or remove existing code before adding new code.
- Do not introduce new wrappers, helpers, abstractions, or layers if a direct edit solves the task clearly.
- Keep diffs minimal: avoid unrelated additions, duplication, or "future-proofing" code not required by the task.

### Principle of Least Surprise
- Design methods and interfaces so their behavior is predictable from the name, parameters, return type, and containing interface.
- Keep contracts focused and intention-revealing. Require only data relevant to the declared responsibility.
- Avoid hidden side effects, surprising dependencies, and unrelated concerns in the same contract.
- If non-obvious behavior is required, make it explicit in naming, types, or documentation.

### Validation Checklist
- Run tests and checks required by the repository after code changes.
