# Recovery Drill Checklist

- [ ] Broker stop/restart: readiness turns unhealthy, outbox accumulates, recovery drains without message loss.
- [ ] Broker partition/pause: connections become unhealthy, unpause recovers and a post-recovery message is handled.
- [ ] Consumer cancellation/restart: in-flight work drains or is redelivered; Inbox prevents duplicate effect.
- [ ] Duplicate delivery: publish the same message identity twice; one Inbox row and one business effect remain.
- [ ] Database interruption: readiness fails closed; committed outbox rows survive; service recovers within RTO.
- [ ] Poison event: retry limit reaches dead-letter; dry-run lists the exact candidate; authorized replay creates an
      `outbox_replay_audit` row and does not duplicate the business effect.
- [ ] Migration failure: rollout stops, corrected bundle rolls forward, schema and migration ledger are verified.
- [ ] Signing-key rotation: new key signs, old key verifies during overlap, retired key removal follows token lifetime.

Attach timestamps, metric queries, logs, operation ids and observed RTO/RPO to the drill record.
