FROM docker:29-cli

RUN apk upgrade --no-cache && apk add --no-cache bash postgresql16-client docker-cli-compose tzdata

WORKDIR /app
COPY crontab /etc/crontabs/root
COPY *.sh ./
RUN chmod +x *.sh

ENTRYPOINT ["/app/entrypoint.sh"]
