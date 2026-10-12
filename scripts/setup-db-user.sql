-- SentinelOps Least-Privilege Database Role Setup
-- Execute this script as PostgreSQL administrator (superuser) to configure
-- the dedicated application role for SentinelOps.

-- 1. Create dedicated application user if it does not already exist
DO
$do$
BEGIN
   IF NOT EXISTS (
      SELECT FROM pg_catalog.pg_roles
      WHERE rolname = 'sentinelops_app') THEN
      CREATE ROLE sentinelops_app WITH LOGIN PASSWORD 'CHANGE_THIS_PASSWORD_IN_USER_SECRETS';
   END IF;
END
$do$;

-- 2. Grant connectivity on the target database
GRANT CONNECT ON DATABASE sentinelops TO sentinelops_app;

-- 3. Grant schema and object permissions
\c sentinelops

GRANT USAGE, CREATE ON SCHEMA public TO sentinelops_app;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO sentinelops_app;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO sentinelops_app;

-- 4. Ensure future tables created during EF Core migrations are accessible
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL PRIVILEGES ON TABLES TO sentinelops_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL PRIVILEGES ON SEQUENCES TO sentinelops_app;
