# Bedrock Platform Release Contract

The six supported .NET assemblies and four frontend packages are released independently from product modules and
hosts. Versioning is Semantic Versioning; the release version is supplied as `BEDROCK_PACKAGE_VERSION` and must match
the `v<version>` tag.

## Release Gate

1. Refresh locked NuGet/pnpm graphs and run the zero-warning build, full tests, architecture tests and browser axe
   harness.
2. Run `PublicApiCompatibilityTests` without `BEDROCK_UPDATE_PUBLIC_API`; any intentional API change must update the
   reviewed snapshot and record compatibility/deprecation rationale.
3. Pack only the supported assemblies with `dotnet pack --no-restore`; never ship hosts, modules or tests.
4. Pack frontend packages from `platform/web` with `pnpm -r pack`; publish is guarded by the release environment's
   `NPM_TOKEN` and uses npm provenance.
5. Generate SPDX SBOM and provenance for the package artifact set. A tag release fails closed if any package, API
   snapshot, lockfile, artifact or attestation step fails.

The release workflow is artifact-producing by default. Publishing is opt-in through repository environment secrets and
does not happen on branch or pull-request builds.
