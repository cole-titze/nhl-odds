FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files for restore
COPY nhl-odds.sln .
COPY WebApi/WebApi.csproj WebApi/
COPY Entry/Entry.csproj Entry/
COPY DataGetter/DataGetter.csproj DataGetter/
COPY DataCleaner/DataCleaner.csproj DataCleaner/
COPY DatabaseAccess/DatabaseAccess.csproj DatabaseAccess/
COPY Entities/Entities.csproj Entities/
COPY Services/Services.csproj Services/
COPY BookmakerOddsGetter/BookmakerOddsGetter.csproj BookmakerOddsGetter/
RUN dotnet restore WebApi/WebApi.csproj && dotnet restore Entry/Entry.csproj

# Copy everything and build
COPY . .
RUN dotnet publish WebApi/WebApi.csproj -c Release -o /app/publish
RUN dotnet build Entry/Entry.csproj -c Release

# Runtime image with .NET SDK (needed for dotnet run) + Python
FROM mcr.microsoft.com/dotnet/sdk:9.0-noble
RUN apt-get update && apt-get install -y --no-install-recommends \
    python3 python3-pip python3-venv \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Python venv with game predictor dependencies
COPY game_predictor/requirements.txt /tmp/requirements.txt
RUN python3 -m venv /app/.venv \
    && /app/.venv/bin/pip install --no-cache-dir -r /tmp/requirements.txt \
    && rm /tmp/requirements.txt

# Copy game predictor source
COPY game_predictor/ /app/game_predictor/

# Copy published WebApi
COPY --from=build /app/publish .

# Copy full source for Entry (dotnet run needs it)
COPY --from=build /src /app/src

ENV ASPNETCORE_URLS=http://+:8080
ENV AdminSettings__PythonPath=/app/.venv/bin/python3
ENV AdminSettings__RepoRoot=/app/src
ENV DOTNET_EnableDiagnostics=0

EXPOSE 8080

ENTRYPOINT ["dotnet", "WebApi.dll"]
