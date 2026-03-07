FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY WebApi/WebApi.csproj WebApi/
COPY DatabaseAccess/DatabaseAccess.csproj DatabaseAccess/
COPY Entities/Entities.csproj Entities/
RUN dotnet restore WebApi/WebApi.csproj

COPY WebApi/ WebApi/
COPY DatabaseAccess/ DatabaseAccess/
COPY Entities/ Entities/
RUN dotnet publish WebApi/WebApi.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "WebApi.dll"]
