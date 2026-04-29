FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/WebApi/WebApi.csproj src/WebApi/
COPY src/DatabaseAccess/DatabaseAccess.csproj src/DatabaseAccess/
COPY src/Entities/Entities.csproj src/Entities/
RUN dotnet restore src/WebApi/WebApi.csproj

COPY src/WebApi/ src/WebApi/
COPY src/DatabaseAccess/ src/DatabaseAccess/
COPY src/Entities/ src/Entities/
RUN dotnet publish src/WebApi/WebApi.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine
RUN apk upgrade --no-cache && apk add --no-cache icu-libs tzdata
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_EnableDiagnostics=0
EXPOSE 8080
ENTRYPOINT ["dotnet", "WebApi.dll"]
