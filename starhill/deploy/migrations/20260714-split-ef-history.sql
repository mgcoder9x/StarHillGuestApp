-- QR-AD-028: one-time, idempotent transition from the legacy shared EF ledger to schema-owned ledgers.
-- Run BEFORE deploying binaries/bundles configured with per-schema MigrationsHistoryTable.
-- The public ledger is intentionally retained for rollback; remove it only in a later, separately approved rollout.
BEGIN;

CREATE SCHEMA IF NOT EXISTS identity;
CREATE SCHEMA IF NOT EXISTS resort_config;
CREATE SCHEMA IF NOT EXISTS rooms;
CREATE SCHEMA IF NOT EXISTS guest_access;

CREATE TABLE IF NOT EXISTS identity."__EFMigrationsHistory" (
    migration_id varchar(150) NOT NULL PRIMARY KEY,
    product_version varchar(32) NOT NULL
);
CREATE TABLE IF NOT EXISTS resort_config."__EFMigrationsHistory" (
    migration_id varchar(150) NOT NULL PRIMARY KEY,
    product_version varchar(32) NOT NULL
);
CREATE TABLE IF NOT EXISTS rooms."__EFMigrationsHistory" (
    migration_id varchar(150) NOT NULL PRIMARY KEY,
    product_version varchar(32) NOT NULL
);
CREATE TABLE IF NOT EXISTS guest_access."__EFMigrationsHistory" (
    migration_id varchar(150) NOT NULL PRIMARY KEY,
    product_version varchar(32) NOT NULL
);

DO $transition$
BEGIN
    IF to_regclass('public."__EFMigrationsHistory"') IS NULL THEN
        RAISE NOTICE 'Legacy public EF history ledger is absent; nothing to copy.';
        RETURN;
    END IF;

    INSERT INTO identity."__EFMigrationsHistory" (migration_id, product_version)
    SELECT migration_id, product_version FROM public."__EFMigrationsHistory"
    WHERE migration_id IN ('20260713075245_InitialCreate', '20260713135945_AddOutboxTraceContext')
    ON CONFLICT (migration_id) DO NOTHING;

    INSERT INTO resort_config."__EFMigrationsHistory" (migration_id, product_version)
    SELECT migration_id, product_version FROM public."__EFMigrationsHistory"
    WHERE migration_id IN ('20260711112306_InitialCreate')
    ON CONFLICT (migration_id) DO NOTHING;

    INSERT INTO rooms."__EFMigrationsHistory" (migration_id, product_version)
    SELECT migration_id, product_version FROM public."__EFMigrationsHistory"
    WHERE migration_id IN ('20260711133837_InitialCreate')
    ON CONFLICT (migration_id) DO NOTHING;

    INSERT INTO guest_access."__EFMigrationsHistory" (migration_id, product_version)
    SELECT migration_id, product_version FROM public."__EFMigrationsHistory"
    WHERE migration_id IN ('20260714031018_InitialCreate')
    ON CONFLICT (migration_id) DO NOTHING;

    -- Guard: mọi migration trong ledger public phải đã được map vào ĐÚNG một schema ledger. Bọc union trong
    -- ngoặc để EXCEPT áp lên toàn bộ tập per-schema (tránh bẫy precedence EXCEPT/UNION trái-sang-phải).
    IF EXISTS (
        SELECT migration_id FROM public."__EFMigrationsHistory"
        WHERE migration_id LIKE '2026%'
        EXCEPT
        (
            SELECT migration_id FROM identity."__EFMigrationsHistory"
            UNION SELECT migration_id FROM resort_config."__EFMigrationsHistory"
            UNION SELECT migration_id FROM rooms."__EFMigrationsHistory"
            UNION SELECT migration_id FROM guest_access."__EFMigrationsHistory"
        )
    ) THEN
        RAISE EXCEPTION 'Legacy EF ledger contains an unmapped migration; update this transition script first.';
    END IF;
END
$transition$;

COMMIT;