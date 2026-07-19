# ModuleName module

This is a domain-neutral Bedrock module scaffold. Replace the marker contracts and use cases with module policy; the
scaffold intentionally contains no business behavior.

- `ModuleName.Contracts`: public module identity and cross-module contracts.
- `ModuleName.Domain`: domain layer.
- `ModuleName.Application`: ports and use-case layer.
- `ModuleName.Infrastructure`: persistence/adapter composition layer.
- `ModuleName.Api`: endpoint composition layer.
- `tests/Modules/ModuleName.UnitTests`: initial compile and marker guard.

The generated projects are added to `Platform.slnx` by `tools/new-module.ps1`. Add a DbContext and migrations only
when this module actually owns persistent state; the template does not force a database into every module.
