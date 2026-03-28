FROM docker:27-cli

RUN apk add --no-cache bash postgresql16-client docker-cli-compose

WORKDIR /app
COPY crontab /etc/crontabs/root
COPY *.sh ./
RUN chmod +x *.sh

ENTRYPOINT ["crond", "-f", "-l", "2"]
