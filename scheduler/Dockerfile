FROM docker:27-cli

RUN apk add --no-cache bash postgresql16-client docker-cli-compose curl \
    && curl -fsSL https://github.com/aptible/supercronic/releases/download/v0.2.33/supercronic-linux-amd64 -o /usr/local/bin/supercronic \
    && chmod +x /usr/local/bin/supercronic

WORKDIR /app
COPY crontab crontab
COPY *.sh ./
RUN chmod +x *.sh

ENTRYPOINT ["/usr/local/bin/supercronic", "crontab"]
