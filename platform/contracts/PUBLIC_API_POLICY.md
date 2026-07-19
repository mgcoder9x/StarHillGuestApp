# Bedrock Public API Policy

## Supported Surface

The compatibility contract covers these reusable assemblies:

- `Bedrock.Domain`
- `Bedrock.Messaging.Contracts`
- `Bedrock.Application`
- `Bedrock.Api`
- `Bedrock.Infrastructure`
- `Adapters.Messaging.RabbitMq`

Reference modules and `Bedrock.ReferenceHost` are examples and composition evidence, not supported platform API.

## Compatibility Rules

- Released packages use Semantic Versioning. Removing, renaming, narrowing accessibility, changing a parameter or
  return type, or changing generic constraints requires a major version.
- Additive public API requires an intentional shipped-baseline update and a design record. It is not accepted as an
  incidental side effect of making implementation types public.
- Deprecations use `[Obsolete]` with a replacement and removal version. A supported member remains functional for at
  least one minor release before removal.
- Integration-event compatibility is independent from CLR package versioning and continues to use event type plus
  schema version.

## Gate

`PublicApiCompatibilityTests` compares compiled public and inheritable protected members with
`contracts/public-api/*.txt`. The descriptor includes type kind/modifiers, inheritance/interfaces, nested
nullability, generic constraints, parameter names/defaults/modifiers, accessor visibility, method/field modifiers,
constant values and deprecation metadata. To intentionally accept a reviewed API change:

```powershell
$env:BEDROCK_UPDATE_PUBLIC_API = '1'
dotnet test tests/Bedrock.ArchitectureTests/Bedrock.ArchitectureTests.csproj `
  --filter FullyQualifiedName~PublicApiCompatibilityTests
Remove-Item Env:BEDROCK_UPDATE_PUBLIC_API
```

Review the baseline diff exactly like source code. Do not update snapshots merely to make CI green.
