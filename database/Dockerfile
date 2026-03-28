FROM postgres:17-alpine

COPY Scripts/CreateTables.sql /docker-entrypoint-initdb.d/01-create-tables.sql

EXPOSE 5432
