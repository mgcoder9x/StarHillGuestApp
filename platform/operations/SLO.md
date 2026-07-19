# Bedrock Operational SLO Template

Products must set their own user-facing SLOs. These are the default platform mechanism objectives and alert starting
points for a single-region deployment; adjust them from measured traffic, not intuition.

## HTTP

- Availability objective: 99.9% successful platform requests per rolling 30 days, excluding explicit client 4xx.
- Latency objective: 99% below 500 ms at the API boundary; define stricter objectives per endpoint class.
- Alert on multi-window burn rate, not a single threshold: 14.4x burn over 5 minutes plus 6x over 1 hour for paging;
  3x over 6 hours for ticketing.

## Outbox

- Delivery objective: 99.9% of non-dead-lettered messages publish within 60 seconds.
- Warning: `bedrock.outbox.oldest_pending.age` above 120 seconds for 10 minutes.
- Critical: `bedrock.outbox.oldest_pending.age` above 600 seconds for 5 minutes or continuously increasing while
  `bedrock.outbox.published` is flat.
- Warning: `bedrock.outbox.dead_letter.depth` above zero. Critical when increasing after a deploy or when one event
  type dominates the set.
- Capacity: alert on the derivative and persistence of `bedrock.outbox.pending`; a static absolute threshold must be
  calibrated per module and traffic class.

## Recovery Objectives

- Database or broker outage RTO: 30 minutes for the reference deployment; product tiers may require less.
- RPO for committed domain state and outbox rows: zero. Broker delivery is at-least-once and consumer effects require
  Inbox/idempotency.
- Recovery drills run at least quarterly and after changes to persistence, messaging, migrations, key management or
  deployment topology.
