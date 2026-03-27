#!/bin/bash
set -e

# Start SQL Server in the background
/opt/mssql/bin/sqlservr &
SQL_PID=$!

# Wait for SQL Server to accept connections (up to 300 seconds)
echo "Waiting for SQL Server to start..."
for i in $(seq 1 300); do
    if /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -Q "SELECT 1" > /dev/null 2>&1; then
        echo "SQL Server is ready."
        break
    fi
    if [ "$i" -eq 300 ]; then
        echo "ERROR: SQL Server did not start within 300 seconds."
        exit 1
    fi
    sleep 1
done

# Create database and tables if they don't already exist
if /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$MSSQL_SA_PASSWORD" \
    -Q "SELECT name FROM sys.databases WHERE name = 'nhl'" -h -1 | grep -q 'nhl'; then
    echo "Database 'nhl' already exists. Skipping schema deployment."
else
    echo "Creating database 'nhl' and deploying schema..."
    /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$MSSQL_SA_PASSWORD" \
        -Q "CREATE DATABASE nhl"
    /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$MSSQL_SA_PASSWORD" \
        -d nhl -i /usr/src/app/CreateTables.sql
    echo "Schema deployment complete."
fi

# Wait on the SQL Server process to keep the container alive
wait $SQL_PID
