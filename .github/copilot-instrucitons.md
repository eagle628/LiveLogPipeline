# C# Solution Guidelines

## Design & Architecture

- Follow the requirements defined in `DesignDoc.md` or `docs/` when provided.

## Language & Modern C\#

- Always target the latest available .NET and C# versions supported by the project (default to .NET 9+ / C# 13+).
- Actively utilize the latest C# syntax and modern language features (e.g., primary constructors, collection expressions, pattern matching, ref struct improvements).
- Nullable reference types: ENABLED (zero compiler warnings allowed).
- Use `async`/`await` for asynchronous operations.

## Constants & Fields Policy

- Use `const` only for values that will never change mathematically/conceptually (e.g., PI, E) or constants strictly internal/private to the assembly.
- For public values that might change in the future (e.g., URLs, limits, defaults) and may be accessed from other DLLs, ALWAYS use `public static readonly` instead of `const` to avoid assembly recompilation issues.

## Performance & Memory Management (Library Standard)

- Avoid unnecessary heap allocations in hot paths:
  - Prefer `ReadOnlySpan<T>` and `Span<T>` over array allocations or string slicing/splitting.
  - Avoid unnecessary LINQ queries inside frequently called loops.
  - Prefer `readonly record struct` or `readonly struct` for small, immutable value types.
- Ensure thread-safety where applicable using lightweight primitives (`Interlocked`, `Volatile`, lock-free designs) before resorting to heavy locks.

## Project Structure & Testing Standards

- Root Layout:
  - Solution files (`*.sln` or `*.slnx`) reside at the repository root.
  - Production source code resides under `src/<ProjectName>/`.
  - Test projects reside under `tests/<ProjectName>.Tests/` (do NOT nest test folders inside production project directories).
- Testing Standards:
  - Framework: xUnit.
  - Design tests using the Arrange-Act-Assert (AAA) pattern.
  - Always include edge cases:
    - Empty, null, and whitespace inputs
    - Boundary values and overflow conditions
    - Concurrent access scenarios for thread-safe components

## Documentation

- Use XML documentation comments (`/// <summary>`) for all public APIs and types.
