# Bedrock Web Base

Domain-agnostic frontend mechanisms for large applications. This workspace intentionally does not contain a product
app, router, page, business store, role model, or authentication policy.

## Packages

- `@bedrock/web-core`: runtime configuration, HTTP transport, correlation/idempotency headers, cancellation and
  RFC Problem Details normalization. Authentication is a request hook, so cookie, bearer and BFF deployments remain
  application choices.
- `@bedrock/design-tokens`: semantic theme contract, CSS-variable generation and WCAG contrast validation. Product
  themes can replace values without changing component semantics.
- `@bedrock/web-adapter`: framework-neutral external-store state for cancellation, stale-result suppression and async
  resource binding. Framework packages can wrap it with hooks/composables without changing the mechanism.
- `@bedrock/ui-primitives`: accessible dialog focus restoration and ARIA relationship helpers without product styling.
- `apps/a11y-harness`: Playwright + axe browser proof on desktop and mobile Chromium.

## Dependency Direction

```text
product app -> framework adapter/components -> web-core + design-tokens
```

The base owns mechanisms and contracts. Applications own routes, screens, feature state, permissions, translations,
analytics policy and visual brand. There is deliberately no implicit retry in the HTTP client: callers must opt into
idempotency before retrying writes.

Run the frontend gate from this directory:

```powershell
pnpm install --frozen-lockfile
pnpm verify
```
