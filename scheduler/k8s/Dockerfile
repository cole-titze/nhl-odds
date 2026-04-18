FROM alpine:3.21

RUN apk upgrade --no-cache && \
    apk add --no-cache bash curl postgresql16-client tzdata && \
    curl -LO "https://dl.k8s.io/release/$(curl -Ls https://dl.k8s.io/release/stable.txt)/bin/linux/arm64/kubectl" && \
    chmod +x kubectl && mv kubectl /usr/local/bin/

WORKDIR /app
COPY crontab /etc/crontabs/root
COPY entrypoint.sh poll-requests.sh ./
RUN chmod +x entrypoint.sh poll-requests.sh

ENTRYPOINT ["/app/entrypoint.sh"]
