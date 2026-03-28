FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY nhl-odds.sln .
COPY WebApi/WebApi.csproj WebApi/
COPY DatabaseAccess/DatabaseAccess.csproj DatabaseAccess/
COPY Entities/Entities.csproj Entities/

# Stub out projects referenced by the solution but not needed for WebApi
RUN mkdir -p Entry DataGetter DataCleaner Services BookmakerOddsGetter \
    && dotnet new classlib -n Entry -o Entry --force \
    && dotnet new classlib -n DataGetter -o DataGetter --force \
    && dotnet new classlib -n DataCleaner -o DataCleaner --force \
    && dotnet new classlib -n Services -o Services --force \
    && dotnet new classlib -n BookmakerOddsGetter -o BookmakerOddsGetter --force

RUN dotnet restore WebApi/WebApi.csproj

COPY WebApi/ WebApi/
COPY DatabaseAccess/ DatabaseAccess/
COPY Entities/ Entities/
RUN dotnet publish WebApi/WebApi.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_EnableDiagnostics=0
EXPOSE 8080
ENTRYPOINT ["dotnet", "WebApi.dll"]
