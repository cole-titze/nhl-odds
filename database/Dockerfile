FROM postgres:18-alpine
RUN apk upgrade --no-cache

COPY Scripts/CreateTables.sql /docker-entrypoint-initdb.d/01-create-tables.sql

EXPOSE 5432
