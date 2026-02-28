FROM mcr.microsoft.com/azure-sql-edge:latest

ENV ACCEPT_EULA=Y

COPY Scripts/CreateTables.sql /usr/src/app/CreateTables.sql
COPY entrypoint.sh /usr/src/app/entrypoint.sh

USER root
RUN chmod +x /usr/src/app/entrypoint.sh
USER mssql

EXPOSE 1433

ENTRYPOINT ["/usr/src/app/entrypoint.sh"]
