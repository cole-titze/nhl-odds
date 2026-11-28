FROM postgres:19-alpine
# The image's gosu is a Go binary built with an old toolchain (Trivy flags its stdlib). Alpine's
# su-exec takes the same arguments and is plain C, so the entrypoint's `gosu postgres ...` uses it.
RUN apk upgrade --no-cache && apk add --no-cache su-exec \
    && rm /usr/local/bin/gosu && ln -s /sbin/su-exec /usr/bin/gosu

COPY Scripts/CreateTables.sql /docker-entrypoint-initdb.d/01-create-tables.sql

EXPOSE 5432
