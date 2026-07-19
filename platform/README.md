# Bedrock Platform Base

Bedrock is a domain-agnostic .NET platform base for large modular systems. It owns reusable mechanisms, architecture
boundaries and executable quality gates. Product routes, business entities, roles, workflows and visual identity stay
outside the base.

## Architecture

```text
Host (composition root)
  -> Module.Api + Module.Infrastructure
  -> Bedrock.Api + Bedrock.Infrastructure

Module.Api -> Module.Application + Module.Contracts + Bedrock.Api
Module.Infrastructure -> Module.Application + Module.Domain + Bedrock.Infrastructure
Module.Application -> Module.Domain + Module.Contracts + Bedrock.Application
Module.Domain -> Bedrock.Domain
```

Architecture tests discover module and adapter projects from disk and inspect compiled assembly references. A new
module cannot silently bypass the dependency-direction gate.

## Runtime Capabilities

- Result/error contracts, validation, authorization, idempotency, transactions and structured logging pipeline.
- EF Core persistence conventions, soft delete, audit metadata, optimistic concurrency and keyed module UoW.
- Transactional outbox/inbox, bounded retries, lease ownership, DLQ quarantine and duplicate-effect fencing.
- Audited outbox replay with dry-run preview, single-use operation IDs and immutable message snapshots.
- JWT verification/signing seams, startup validation, Problem Details, HTTP hardening and versioned endpoints.
- OpenTelemetry traces, metrics and logs plus neutral SLO, Prometheus alerts and Grafana dashboard templates.

The reference host exposes replay under `/v1/operations/outbox/replay` only to authenticated principals carrying the
`platform.outbox.replay` permission. Applications may replace that policy at their composition boundary.

## Frontend Base

`web/` contains framework-neutral packages:

- `@bedrock/web-core`: runtime config, safe HTTP transport, Problem Details and cancellation.
- `@bedrock/design-tokens`: semantic tokens and WCAG contrast guards.
- `@bedrock/web-adapter`: async external-store lifecycle for framework wrappers.
- `@bedrock/ui-primitives`: accessible focus and ARIA mechanisms.
- `apps/a11y-harness`: Playwright + axe verification on desktop and mobile Chromium.

## Module Scaffolding

Create a neutral five-layer module with:

```powershell
powershell -File tools/new-module.ps1 -Name Billing -ModuleKey billing
```

The command generates Contracts, Domain, Application, Infrastructure, Api and UnitTests projects, registers them in
`Platform.slnx`, then can restore/build the result. Persistence and business policy remain explicit module decisions.

## Verification

```powershell
$env:DOTNET_ROOT = (Resolve-Path '..\.tmp-dotnet')
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 all
```

Browser accessibility is a separate executable gate:

```powershell
cd web
pnpm install --frozen-lockfile
pnpm --filter @bedrock/a11y-harness exec playwright install chromium
pnpm a11y
```

Docker-backed PostgreSQL/RabbitMQ tests skip on local machines without Docker and fail closed when `CI=true`. The
scheduled workflow additionally runs the ten-minute broker soak.

## Release

Supported public packages are the six assemblies listed in `contracts/PUBLIC_API_POLICY.md` and the four frontend
packages above. See `RELEASE.md` and `CHANGELOG.md`. A `v<semver>` tag builds packages, verifies the public API
baseline, generates an SPDX SBOM and attests the artifact set; it does not package the reference host or modules.
