#!/bin/bash
set -e

# PostgreSQL Initialization Script
# This script runs when the PostgreSQL container starts for the first time

echo "Starting PostgreSQL initialization..."

# Create application user if it doesn't exist
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    -- Create application user if not exists
    DO
    \$\$
    BEGIN
        IF NOT EXISTS (SELECT FROM pg_user WHERE usename = 'neoservice_app') THEN
            CREATE USER neoservice_app WITH PASSWORD '$POSTGRES_PASSWORD';
        END IF;
    END
    \$\$;

    -- Grant permissions
    GRANT ALL PRIVILEGES ON DATABASE $POSTGRES_DB TO neoservice_app;
    
    -- Create extensions
    CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
    CREATE EXTENSION IF NOT EXISTS "pgcrypto";
    CREATE EXTENSION IF NOT EXISTS "pg_stat_statements";
    CREATE EXTENSION IF NOT EXISTS "btree_gist";
    
    -- Set default search path
    ALTER DATABASE $POSTGRES_DB SET search_path TO public, core, sgx, auth, compute, monitoring;
    
    -- Create schemas
    CREATE SCHEMA IF NOT EXISTS core;
    CREATE SCHEMA IF NOT EXISTS sgx;
    CREATE SCHEMA IF NOT EXISTS auth;
    CREATE SCHEMA IF NOT EXISTS keymanagement;
    CREATE SCHEMA IF NOT EXISTS oracle;
    CREATE SCHEMA IF NOT EXISTS voting;
    CREATE SCHEMA IF NOT EXISTS crosschain;
    CREATE SCHEMA IF NOT EXISTS monitoring;
    CREATE SCHEMA IF NOT EXISTS eventsourcing;
    CREATE SCHEMA IF NOT EXISTS compute;
    
    -- Grant schema permissions
    GRANT ALL ON SCHEMA core TO neoservice_app;
    GRANT ALL ON SCHEMA sgx TO neoservice_app;
    GRANT ALL ON SCHEMA auth TO neoservice_app;
    GRANT ALL ON SCHEMA keymanagement TO neoservice_app;
    GRANT ALL ON SCHEMA oracle TO neoservice_app;
    GRANT ALL ON SCHEMA voting TO neoservice_app;
    GRANT ALL ON SCHEMA crosschain TO neoservice_app;
    GRANT ALL ON SCHEMA monitoring TO neoservice_app;
    GRANT ALL ON SCHEMA eventsourcing TO neoservice_app;
    GRANT ALL ON SCHEMA compute TO neoservice_app;
    
    -- Set default privileges for future tables
    ALTER DEFAULT PRIVILEGES IN SCHEMA core GRANT ALL ON TABLES TO neoservice_app;
    ALTER DEFAULT PRIVILEGES IN SCHEMA sgx GRANT ALL ON TABLES TO neoservice_app;
    ALTER DEFAULT PRIVILEGES IN SCHEMA auth GRANT ALL ON TABLES TO neoservice_app;
    ALTER DEFAULT PRIVILEGES IN SCHEMA keymanagement GRANT ALL ON TABLES TO neoservice_app;
    ALTER DEFAULT PRIVILEGES IN SCHEMA oracle GRANT ALL ON TABLES TO neoservice_app;
    ALTER DEFAULT PRIVILEGES IN SCHEMA voting GRANT ALL ON TABLES TO neoservice_app;
    ALTER DEFAULT PRIVILEGES IN SCHEMA crosschain GRANT ALL ON TABLES TO neoservice_app;
    ALTER DEFAULT PRIVILEGES IN SCHEMA monitoring GRANT ALL ON TABLES TO neoservice_app;
    ALTER DEFAULT PRIVILEGES IN SCHEMA eventsourcing GRANT ALL ON TABLES TO neoservice_app;
    ALTER DEFAULT PRIVILEGES IN SCHEMA compute GRANT ALL ON TABLES TO neoservice_app;
    
    -- Set default privileges for sequences
    ALTER DEFAULT PRIVILEGES IN SCHEMA core GRANT ALL ON SEQUENCES TO neoservice_app;
    ALTER DEFAULT PRIVILEGES IN SCHEMA sgx GRANT ALL ON SEQUENCES TO neoservice_app;
    ALTER DEFAULT PRIVILEGES IN SCHEMA auth GRANT ALL ON SEQUENCES TO neoservice_app;
    ALTER DEFAULT PRIVILEGES IN SCHEMA keymanagement GRANT ALL ON SEQUENCES TO neoservice_app;
    ALTER DEFAULT PRIVILEGES IN SCHEMA oracle GRANT ALL ON SEQUENCES TO neoservice_app;
    ALTER DEFAULT PRIVILEGES IN SCHEMA voting GRANT ALL ON SEQUENCES TO neoservice_app;
    ALTER DEFAULT PRIVILEGES IN SCHEMA crosschain GRANT ALL ON SEQUENCES TO neoservice_app;
    ALTER DEFAULT PRIVILEGES IN SCHEMA monitoring GRANT ALL ON SEQUENCES TO neoservice_app;
    ALTER DEFAULT PRIVILEGES IN SCHEMA eventsourcing GRANT ALL ON SEQUENCES TO neoservice_app;
    ALTER DEFAULT PRIVILEGES IN SCHEMA compute GRANT ALL ON SEQUENCES TO neoservice_app;
EOSQL

echo "PostgreSQL initialization completed successfully"

# Run the main migration script if it exists
if [ -f "/docker-entrypoint-initdb.d/InitialCreate.sql" ]; then
    echo "Running initial migration script..."
    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" -f /docker-entrypoint-initdb.d/InitialCreate.sql
    echo "Initial migration completed"
fi

# Run the seed data script if it exists
if [ -f "/docker-entrypoint-initdb.d/SeedData.sql" ]; then
    echo "Running seed data script..."
    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" -f /docker-entrypoint-initdb.d/SeedData.sql
    echo "Seed data loaded"
fi

echo "All initialization scripts completed"