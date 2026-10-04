FROM docker:29-cli

RUN apk upgrade --no-cache && apk add --no-cache bash postgresql16-client docker-cli-compose tzdata

# The scheduler only runs containers, never builds them; drop buildx (and its CVEs)
RUN rm -f /usr/local/libexec/docker/cli-plugins/docker-buildx

WORKDIR /app
COPY crontab /etc/crontabs/root
COPY *.sh ./
RUN chmod +x *.sh

ENTRYPOINT ["/app/entrypoint.sh"]
