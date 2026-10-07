# Cognitive complexity metric

`RSHARP1006` measures the amount of control-flow state a reader must keep in mind while reading a method.

It is intentionally not cyclomatic complexity.

## Scoring

ReadableSharp starts at zero and applies the following deterministic costs:

- `if`, loops, `switch`, `catch`, and conditional expressions: `1 + current nesting depth`
- additional `switch` sections after the first: `+1`
- a plain `else` branch: `+1`
- each logical operator sequence (`&&` or `||`): `+1`
- each local function or anonymous function encountered in the method body: `+1`

Nested control flow therefore costs more than equivalent flat control flow.

A top-level guard clause such as `if (!ready) return;` costs `1` and does not increase nesting for its exit statement. This makes a sequence of guard clauses cheaper than wrapping the rest of the method in nested branches.

The default maximum is `15`.

## Configuration

```ini
[*.cs]
readablesharp_rsharp1006.max_complexity = 12
```

The configured value changes only the threshold. It does not change the scoring model.
