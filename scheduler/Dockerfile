FROM docker:29-cli

RUN apk upgrade --no-cache && apk add --no-cache bash postgresql18-client docker-cli-compose tzdata

# The scheduler only runs containers, never builds them; drop buildx (and its CVEs). Also drop the
# base image's own compose plugin, which lags on Go fixes; Alpine's docker-cli-compose (installed
# above, kept current by apk upgrade) takes its place
RUN rm -f /usr/local/libexec/docker/cli-plugins/docker-buildx \
    /usr/local/libexec/docker/cli-plugins/docker-compose

WORKDIR /app
COPY crontab /etc/crontabs/root
COPY *.sh ./
RUN chmod +x *.sh

ENTRYPOINT ["/app/entrypoint.sh"]
