FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Entry/Entry.csproj Entry/
COPY DataGetter/DataGetter.csproj DataGetter/
COPY DataCleaner/DataCleaner.csproj DataCleaner/
COPY DatabaseAccess/DatabaseAccess.csproj DatabaseAccess/
COPY Entities/Entities.csproj Entities/
COPY Services/Services.csproj Services/
RUN dotnet restore Entry/Entry.csproj

COPY Entry/ Entry/
COPY DataGetter/ DataGetter/
COPY DataCleaner/ DataCleaner/
COPY DatabaseAccess/ DatabaseAccess/
COPY Entities/ Entities/
COPY Services/ Services/
RUN dotnet publish Entry/Entry.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
RUN apt-get update && apt-get install -y python3 python3-pip python3-venv && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app .
COPY game_predictor/ /app/game_predictor/
COPY game_predictor/requirements.txt /app/game_predictor/requirements.txt
RUN python3 -m venv /app/.venv && /app/.venv/bin/pip install --no-cache-dir -r /app/game_predictor/requirements.txt

COPY entrypoint.sh /app/entrypoint.sh
RUN chmod +x /app/entrypoint.sh

ENTRYPOINT ["/app/entrypoint.sh"]
