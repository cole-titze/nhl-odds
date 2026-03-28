FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY nhl-odds.sln .
COPY Entry/Entry.csproj Entry/
COPY DataGetter/DataGetter.csproj DataGetter/
COPY DataCleaner/DataCleaner.csproj DataCleaner/
COPY DatabaseAccess/DatabaseAccess.csproj DatabaseAccess/
COPY Entities/Entities.csproj Entities/
COPY Services/Services.csproj Services/
COPY BookmakerOddsGetter/BookmakerOddsGetter.csproj BookmakerOddsGetter/
RUN dotnet restore Entry/Entry.csproj

COPY . .
RUN dotnet publish Entry/Entry.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Entry.dll"]
