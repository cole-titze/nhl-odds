# nhl-odds

The nhl project. This repo collects nhl data from the nhl api, cleans it, and then runs machine learning models on the data to predict outcomes. There is also a full-stack web app to access the information.

# Install and Run

## Prerequisites

- [Install docker desktop](https://www.docker.com/products/docker-desktop/)
- [Install dotnet](https://dotnet.microsoft.com/en-us/download)

## Setup Database

```
sudo docker pull mcr.microsoft.com/azure-sql-edge:latest
sudo docker run --restart=always --cap-add SYS_PTRACE -e 'ACCEPT_EULA=1' -e 'MSSQL_SA_PASSWORD=<YOUR PASSWORD>' -p 1433:1433 --name azuresqledge -d mcr.microsoft.com/azure-sql-edge
```

- Connect to sql server and run database scripts (connecting to 'localhost' from mssql extension works well)

1. CreateDatabase.sql
2. CreateTables.sql

### Run Data Collection

- Build

```
dotnet build
```

- Run

```
cd ../Entry
dotnet run
```

### Run Data Models
