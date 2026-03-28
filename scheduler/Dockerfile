FROM docker:27-cli

RUN apk add --no-cache bash postgresql16-client supercronic

WORKDIR /app
COPY crontab crontab
COPY *.sh ./
RUN chmod +x *.sh

ENTRYPOINT ["supercronic", "crontab"]
