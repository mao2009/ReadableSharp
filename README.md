# ReadableSharp

**Human-readable code, enforced at compile time.**

ReadableSharp is a Roslyn analyzer for C# that detects code which is technically valid but unnecessarily difficult for humans to understand.

The project focuses on **cognitive load**, not formatting preferences.

## Why

AI-assisted development makes it easy to generate large amounts of correct code quickly. Correct code is not automatically maintainable code.

ReadableSharp aims to reject structures that force a reader to keep too much state in their head at once:

- deeply nested expressions;
- overly complex boolean conditions;
- long or nested LINQ pipelines;
- nested conditional operators;
- nested lambdas;
- methods with excessive cognitive complexity;
- compound expressions that should be named or extracted.

## Non-goals

ReadableSharp is not intended to become another general-purpose style checker.

It should avoid rules whose value is mainly subjective, such as:

- arbitrary naming preferences;
- whitespace or formatting rules;
- blanket bans on short variable names;
- line-length rules without structural meaning;
- stylistic rules already handled well by formatters or StyleCop.

The central question is:

> How much context must a human keep in their head to understand this code?

## MVP rules

| ID | Rule |
| --- | --- |
| RSHARP1001 | Expression nesting too deep |
| RSHARP1002 | Boolean condition too complex |
| RSHARP1003 | LINQ chain too complex |
| RSHARP1004 | Nested conditional operator |
| RSHARP1005 | Lambda nesting too deep |
| RSHARP1006 | Method cognitive complexity too high |
| RSHARP1007 | Complex expression should be named or extracted |

Each diagnostic should explain **why** the code is hard to follow and suggest a structural remediation that works for both humans and coding agents.

## Design principles

1. Prefer structural, measurable rules over taste.
2. Diagnostics must be deterministic.
3. Suggested fixes should reduce cognitive load rather than merely silence the analyzer.
4. Avoid encouraging pointless extraction into badly named helper methods.
5. Configuration should tune thresholds, not redefine readability from scratch.
6. Rules should be suitable for CI enforcement.

## Example

Instead of allowing a reader-hostile chain like:

```csharp
return items.Where(x => x.IsActive)
    .Select(x => new
    {
        x.Id,
        Value = values.TryGetValue(x.Id, out var value)
            ? value?.Items?.Where(y => y.Enabled)
                .OrderBy(y => y.Sort)
                .FirstOrDefault()?.Name ?? ""
            : ""
    })
    .Where(x => x.Value.Length > 0)
    .ToDictionary(x => x.Id, x => x.Value);
```

ReadableSharp should encourage intermediate concepts to be named:

```csharp
var activeItems = items.Where(x => x.IsActive);
var results = new Dictionary<int, string>();

foreach (var item in activeItems)
{
    var name = GetEnabledItemName(item.Id);

    if (!string.IsNullOrEmpty(name))
    {
        results[item.Id] = name;
    }
}

return results;
```

## Initial structure

```text
ReadableSharp
├─ src/
│  └─ ReadableSharp.Analyzers
├─ tests/
│  └─ ReadableSharp.Tests
├─ samples/
├─ README.md
└─ ReadableSharp.sln
```

## Status

Early development. The first milestone is the seven MVP readability diagnostics listed above.

## License

MIT
