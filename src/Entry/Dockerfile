FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY nhl-odds.sln .
COPY src/Entry/Entry.csproj src/Entry/
COPY src/DataGetter/DataGetter.csproj src/DataGetter/
COPY src/DataCleaner/DataCleaner.csproj src/DataCleaner/
COPY src/DatabaseAccess/DatabaseAccess.csproj src/DatabaseAccess/
COPY src/Entities/Entities.csproj src/Entities/
COPY src/Services/Services.csproj src/Services/
COPY src/BookmakerOddsGetter/BookmakerOddsGetter.csproj src/BookmakerOddsGetter/
RUN dotnet restore src/Entry/Entry.csproj

COPY . .
RUN dotnet publish src/Entry/Entry.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine
RUN apk upgrade --no-cache && apk add --no-cache icu-libs tzdata
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Entry.dll"]
