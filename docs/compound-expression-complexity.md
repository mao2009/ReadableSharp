# Compound expression complexity

`RSHARP1007` is a synthesis rule for expressions that combine several different sources of cognitive load.

It intentionally does **not** report an expression when only one kind of complexity is present. Dedicated rules such as expression nesting or LINQ-chain complexity are better diagnostics in that case.

The default threshold is `8`.

Current contributors are deterministic structural signals:

- deep expression nesting;
- conditional flow;
- repeated null/coalesce flow;
- inline lambdas;
- multi-stage LINQ;
- many calls in one expression;
- mixing several operation kinds in one expression.

No LLM, dictionary, or subjective name-quality model is used.

## Configuration

```ini
[*.cs]
readablesharp_rsharp1007.max_complexity = 10
```
