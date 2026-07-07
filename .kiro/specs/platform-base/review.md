
A. Critique report
design.md

# Sev	Location	Problem	Fix applied

D1	🔴	§3.3 dependency matrix	Modules.<M></m>.Contracts listed as "ref —" and M.Application not allowed to ref its own Contracts. But RoomCreatedIntegrationEvent : IntegrationEvent lives in Contracts and derives from a base in BuildingBlocks.Application → the spec as written cannot compile on day one	Matrix fixed: Contracts may ref BuildingBlocks.Application (only for the event base); M.Application refs own M.Contracts + other B.Contracts. Footnote tagged [Tinh chỉnh so với Blueprint]
D2	🔴	§7.2 dispatcher	SELECT … WHERE processed_at IS NULL LIMIT n with no claim → two host instances double-publish and race on error_count; no next_attempt_at → poison messages retried every tick forever; "dead-letter" mentioned but had no schema representation	Schema gains next_attempt_at, dead_lettered_at, partial pending index; algorithm rewritten with atomic claim (skip-locked/lease, provider wording kept in Infrastructure), exponential backoff, threshold → dead-letter. New CP15 + R8.5/8.6
D3	🔴	§5.2 seam	"Use case must not see IEventBusPublisher" was unenforceable — all interfaces sat in one namespace; CP11 had no mechanical check ("how do you detect a use case?")	Seam split into Messaging (use-case-facing) vs Messaging.Dispatch (worker/adapter-facing). CP11 is now a trivial NetArchTest rule: IUseCase* implementors must not depend on *.Messaging.Dispatch
D4	🔴	§5.2/§7.3 consumer	No EventType→CLR-type resolution existed — a consumer literally could not deserialize payloads. Unknown event type behavior undefined (crash loop)	Added IIntegrationEventTypeRegistry (built at Host from Contracts assemblies); unknown EventType → dead-letter, not crash. New R17.3
D5	🔴	§9.4 validator sample	RequiredPortsValidator resolved scoped ports from the root provider — with the design's own ValidateScopes=true this throws at boot even when correctly configured; also stopped at first missing port and hardcoded the list	Sample corrected: IServiceScopeFactory scope, aggregated error message, list composed via StartupValidationOptions.RequiredPorts contributed by each AddXxxCore() (adding a capability never edits the validator)
D6	🔴	§7.1 + §5.1	Transaction behavior wraps commands and the sample use case calls ExecuteInTransactionAsync itself → nested BEGIN, undefined semantics (would fail on Npgsql)	Reentrancy contract defined on IUnitOfWork (inner call joins ambient tx). New R7.4, tested in task 6.3
D7	🟡	§5.1 IUnitOfWork.Repository<T></t>()	Service-locator: constructors don't reveal which aggregates a use case touches; harder to fake one repo	Removed; IRepository<T></t> injected directly (same scoped DbContext). Deviation from Blueprint §5.1 documented with benefit/tradeoff
D8	🟡	§4.1 Entity.RaiseDomainEvent	Kernel collects domain events but nothing ever dispatched them — a dangling feature; F13 explicitly lists domain events as a missing pillar	Added minimal IDomainEventDispatcher/IDomainEventHandler<T></t> + dispatch-before-commit loop with max-depth in PlatformDbContext.SaveChangesAsync (§7.5). New R33, CP14
D9	🟡	§4.4 outbox placement	"Shared infra schema OR per-module" left open — but this is a correctness choice, not taste: same DbContext ⇒ same transaction is what makes CP6 hold by construction	Decision locked: per-module outbox/inbox via modelBuilder.AddOutboxInbox(); tradeoff (dispatcher per module) stated
D10	🟡	§5.6/§9.5 JWT	Key-ring signing lives in Infrastructure, JwtBearer verification in Api, Api can't ref Infrastructure (F14) — the design never said how they share key material (classic composition trap)	JwtKeyRingOptions as the meeting point (Api: IssuerSigningKeyResolver by kid; Infra: sign with active key). No project reference needed
D11	🟡	§6.1 "no-op defaults"	Blanket no-op defaults are dangerous: a no-op IEmailSender silently loses mail; a no-op IDistributedLock reintroduces races	Per-port default classification: NullAppCache (safe degrade) vs Throwing* (fail-loud) vs no-default+boot-block for security ports (IPasswordHasher, IHtmlSanitizer). New R16.4
D12	🟡	§8 behavior order	Validation before Authorization leaks validation detail to unauthorized callers and wastes work	Order now Logging → Authorization → Validation → Idempotency → Transaction, rationale recorded as blueprint deviation. Idempotency duplicate semantics defined (idempotency_conflict, no response-replay in v1)
D13	🟡	whole doc	No HTTP middleware order (F16/F21 both depend on ordering); no health checks despite Overview claiming "health" is preserved; F19 port only described as three loose method names; RefreshTokenRecord placed in core Infrastructure while §6.4 moves Identity to Modules (unresolved tension); SearchRequest/EmailMessage/FileBlob/CacheEntryOptions undefined; IRateLimitStore vs Api rate-limit middleware ambiguous; no Non-goals section; Result contract sketch not valid C# and struct-vs-class undecided; CP5 Validates: Requirements 4 mislinked; stale §12/§13 pointers in traceability table	All addressed: pipeline order table (§3.5); health standard (§9.6) + R34; IRefreshTokenStore full port + placement decision (mechanism in BuildingBlocks, table owned by consuming module's schema via AddRefreshTokens(schema)); contract DTOs defined; two-tier rate-limit clarification; Non-goals (§1.2); full Result/Result<T></t> member contract with default-state invariant + open decision; CP5 → "2, 4"; traceability pointers fixed
D14	🟢	§4.1 uint RowVersion	F8 renamed the interface but kept the token type shaped exactly like the Postgres 32-bit xid — a silent provider leak	Kept (pragmatic, matches target provider + Non-goals) but the tradeoff is now stated explicitly instead of hidden
requirements.md

# Sev	Location	Problem	Fix applied

R1	🔴	R8	No criteria for multi-instance dispatcher exclusivity, retry scheduling, or dead-letter threshold — the parts that actually go wrong in production	R8.4 tightened; R8.5 (backoff + dead-letter), R8.6 (atomic claim, WHILE-form) added
R2	🔴	R17	No serialization/type-resolution contract → consumer undeserializable (mirror of D4)	R17.3 added (registry + unknown-type → dead-letter)
R3	🟡	R7	Transaction reentrancy edge case unaddressed (mirror of D6)	R7.4 added
R4	🟡	R13.1	Required-port list given as "IUnitOfWork/IClock/…/..." — an ellipsis is untestable	Reworded: composed list via StartupValidationOptions.RequiredPorts, error message must enumerate all missing ports
R5	🟡	R32.4	Contract test "error code ↔ FE ErrorCode (reflection)" — there is no FE in this greenfield scope; criterion was unsatisfiable as written	Reworded to code-registry + event-schema snapshot tests; FE sync via exported artifact, out of base scope
R6	🟡	missing	No requirements for domain-event dispatch (F13 pillar) or health endpoints (claimed as "preserved" in design Overview but specified nowhere)	R33 (4 criteria) and R34 (3 criteria) appended without renumbering existing requirements; traceability table extended
R7	🟢	R9, R16, R24.3	Handler re-publish rule, fail-loud optional ports, dead-letter metric absent	R9.4, R16.4 added; R24.3 metric list extended
tasks.md / README.md

# Sev	Problem	Fix applied

T1	🟡	Task 3 referenced R5.1 (adapter isolation) in a wave where no adapter exists — dangling/premature	Removed; adapter rule tested in task 14 where adapters exist
T2	🟡	Wave 4 ran task 9 (startup validator) before Infrastructure existed, though the validator lives there	Task 10 now depends on task 6; waves rebuilt (10 waves, valid topological order)
T3	🟡	Per-task acceptance criteria absent (only a global note)	Every task now carries an explicit "Nghiệm thu" line (build 0 warning + specific green tests)
T4	🟡	No tasks for the new seams (domain events, claim/backoff, type registry, health, key-ring split sign/verify)	Tasks 3.3, 5.3/5.4, 6.3/6.4, 7.1–7.4, 9.2, 13 restructured; CP coverage map added to Notes (all CP1–CP15 covered)
T5	🟢	README said "20 task, 10 wave", "R1–R32", "CP1–CP13"; task-0 numbering; no reading-order vs authority-order distinction; no open-decision flag	All corrected (21 tasks / 10 waves / R34 / CP15); tasks renumbered 1–21 throughout
B. Changelog
File	Change	Why (critique ref)
requirements.md	+R33 (domain events), +R34 (health); +criteria 7.4, 8.5, 8.6, 9.4, 16.4, 17.3; 8.4/13.1/32.4 reworded; +4 glossary terms; traceability table extended (rows 7, 8, 9, 16, 17, 33, 34). Existing numbering untouched — zero renumber churn	R1–R7, D2, D4, D6
design.md	Non-goals §1.2; pending-rename + Result open decisions in header; matrix fix + machine-checkable rule 6; HTTP pipeline order §3.5; IEndpointModule discovery contract; refresh-token placement decision; UoW without Repository<T></t>() + reentrancy; namespace-split messaging + type registry; domain-event seam §5.3/§7.5; contract DTOs + fail-loud default table §5.5; IRefreshTokenStore + JwtKeyRingOptions resolution §5.7; DI duplicate-guard semantics + corrected scope-aware validator §6.2/§9.4; dispatcher claim/backoff/dead-letter §7.2 + schema §4.5; behavior reorder §8; health §9.6; CP5 relink, CP11 mechanized, +CP14/+CP15; dependency whitelist per project (incl. Scrutor justification) §17; all stale § pointers fixed	D1–D14
tasks.md	21 tasks renumbered 1–21, restructured to the new design; per-task acceptance; new subtasks (3.2/3.3, 5.3–5.5, 6.3/6.4, 7.1–7.4, 9.2, 13); Mermaid graph + waves JSON rebuilt (10 waves, dependency-valid); Notes updated with CP→task coverage map	T1–T4
README.md	Counts corrected (R1–R34, CP1–CP15, 21 tasks/10 waves); reading order vs authority order separated; open naming/Result decisions flagged; phase→task table remapped; 3-layer verification updated; Docker note remapped to tasks 7.4/8.3/14	T5
C. The 4 rewritten files
Applied directly to disk (avoiding a ~2,000-line duplication in chat — the files are the canonical output):

requirements.md — R1–R34, EARS, full F#/I# traceability
design.md — all required Kiro sections present; CP1–CP15 each with **Validates: Requirements X**; blueprint deviations tagged [Tinh chỉnh so với Blueprint] with rationale
tasks.md — 21 tasks, _Requirements:_/_Correctness Properties:_ on every task, Mermaid + waves JSON consistent
README.md — onboarding map, greenfield state, 3-layer verification
Verified: all Validates targets exist (spot-checked R1–R34 headings), no dangling CP/task/requirement refs, all required section headings present, greenfield wording preserved ("cần tạo", never "đã có").

D. Open questions / assumptions
Needs your decision (both flagged in design header + README §1; chốt before tasks 1–2):

Prefix BuildingBlocks.* vs Bedrock.* — I did not rename (you listed it as your decision). Files stay consistently BuildingBlocks.*. My recommendation: take the single-word prefix — shorter namespaces/paths (relevant on Windows), no plural namespace; since it's greenfield the rename is a zero-risk global find-replace now, and expensive later. "BuildingBlocks" being the idiomatic literature term is the only counter-argument.
Result/Result<T></t>: readonly struct vs sealed class — design proposes struct (zero-alloc, with a documented default(Result) = failure invariant); class is simpler with no default-state trap. Member contract is identical either way.
Dead-letter representation — I chose a dead_lettered_at column over a separate table (simpler, one fewer moving part); flag if you want a dedicated DLQ table for operational tooling.
Assumptions made explicit:

Scrutor + M.E.DependencyInjection/Logging.Abstractions are allowed in BuildingBlocks.Application as composition plumbing (not "technology") — whitelisted with tradeoff in design §17; reject this and AddBuildingBlocksCore moves to Infrastructure at the cost of Host ergonomics.
Refresh-token mechanism in BuildingBlocks, table ownership in the consuming module's schema — my resolution of the F19-vs-"Identity is a module" tension; the alternative (everything in Modules.Identity) is defensible but forfeits reuse of the race-safe store.
PostgreSQL remains the sole production target (now codified in Non-goals §1.2); uint RowVersion kept on that basis.
All version pins (FluentValidation 12.1.1, SDK 10.0.301, EF 10.x) are "verify at install time" — no compatibility claims asserted.
