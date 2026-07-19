# Bedrock Recovery Runbook

## Safety Rules

1. Preserve the database as source of truth. Do not delete pending or dead-lettered messages to make a graph green.
2. Use least-privilege operator authorization outside the base service. Every replay requires a unique operation id,
   authenticated actor, written reason, bounded selector and dry-run review.
3. Capture timestamps, deployment version, module tag, correlation ids and metric screenshots before mutation.

## HTTP Error Budget

1. Confirm the burn-rate alert across both the short and long window, then split 5xx by route template, deployment and
   dependency. Do not use raw URL paths that contain identifiers as metric labels.
2. Stop the active rollout when the error increase aligns with a new version. Check readiness, saturation and the
   downstream database/broker before scaling blindly.
3. Preserve correlation IDs and traces for representative failures, mitigate, then verify both error rate and latency
   return inside the SLO window.

## Broker Outage

1. Confirm broker readiness and consumer readiness separately. Inspect `bedrock.outbox.pending`,
   `bedrock.outbox.oldest_pending.age`, publisher failures and broker alarms.
2. Stop rollout changes. Do not replay pending rows; they are already eligible and will drain after recovery.
3. Restore broker quorum/network/DNS/credentials, then verify consumers return to `Consuming` and publisher confirms
   succeed.
4. Watch oldest-pending age decrease to zero and confirm no unexpected dead-letter growth.

## Dead-Letter Replay

1. Identify messages by id, event type and/or bounded occurrence window. Diagnose and fix the downstream or payload
   problem first.
2. Call `POST /v1/operations/outbox/replay/preview` (or `IOutboxReplayService.ExecuteAsync` with `DryRun=true`) with a
   unique `OperationId`, reason and conservative `MaxMessages`. The Host derives actor from authenticated `sub`; review
   every returned candidate and retained `LastError`.
3. Call `POST /v1/operations/outbox/replay` only after authorization. The service first claims the single-use operation id in
   `outbox_replay_operation`, then clears quarantine/retry ownership and writes immutable message snapshots to
   `outbox_replay_audit` in the same transaction.
4. Verify publish, Inbox deduplication and business effect. Review `GET /v1/operations/outbox/replay/{operationId}` and
   query the operation/item rows by operation id. If a
   commit result is ambiguous, inspect these rows before issuing any new operation id. Never bypass Inbox for a
   replayed integration event.

## Database Outage

1. Confirm database readiness, connection saturation, storage and replica state. Keep API instances unready while the
   dependency is unsafe.
2. Restore database service before broker replay activity. Outbox records committed before the outage remain the
   source of truth.
3. Verify migrations ledger and module schemas, then observe outbox drain and error-rate recovery.

## Migration Failure

1. Stop application rollout. Do not auto-migrate from multiple application instances.
2. Prefer roll-forward with a corrected migration bundle. Roll back only when the migration is explicitly reversible
   and no newer application wrote data under the new schema.
3. Validate schema, migration history and a clean host startup before resuming traffic.

## Signing-Key Rotation

1. Add the new verify/sign key, deploy it everywhere, then switch `ActiveKid`.
2. Retain old verify keys beyond the maximum token lifetime plus clock skew.
3. Remove old keys only after telemetry confirms no valid tokens reference them.

## Drill Evidence

Record scenario, start/end time, observed alert, RTO/RPO, commands, screenshots, replay operation ids, audit rows,
unexpected behavior and follow-up owner. A drill without evidence is not accepted as resilience proof.
