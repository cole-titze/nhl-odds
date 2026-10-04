FROM alpine:3.21

# Fetch the index once (rather than --no-cache on each command, which warns
# about the missing cache) and remove it at the end
RUN apk update && \
    apk upgrade && \
    apk add bash curl postgresql16-client tzdata && \
    curl -LO "https://dl.k8s.io/release/$(curl -Ls https://dl.k8s.io/release/stable.txt)/bin/linux/arm64/kubectl" && \
    chmod +x kubectl && mv kubectl /usr/local/bin/ && \
    rm -rf /var/cache/apk/*

WORKDIR /app
COPY crontab /etc/crontabs/root
COPY entrypoint.sh poll-requests.sh ./
RUN chmod +x entrypoint.sh poll-requests.sh

ENTRYPOINT ["/app/entrypoint.sh"]
