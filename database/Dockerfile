FROM mcr.microsoft.com/azure-sql-edge:latest

ENV ACCEPT_EULA=Y

USER root
RUN apt-get update && \
    apt-get install -y curl gnupg2 && \
    curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg && \
    curl -fsSL https://packages.microsoft.com/config/ubuntu/22.04/prod.list | tee /etc/apt/sources.list.d/mssql-release.list && \
    apt-get update && \
    ACCEPT_EULA=Y apt-get install -y mssql-tools18 unixodbc-dev && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

COPY Scripts/CreateTables.sql /usr/src/app/CreateTables.sql
COPY entrypoint.sh /usr/src/app/entrypoint.sh

RUN chmod +x /usr/src/app/entrypoint.sh
USER mssql

EXPOSE 1433

ENTRYPOINT ["/usr/src/app/entrypoint.sh"]
