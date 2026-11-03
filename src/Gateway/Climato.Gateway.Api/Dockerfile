FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Gateway/Climato.Gateway.Api/Climato.Gateway.Api.csproj src/Gateway/Climato.Gateway.Api/
COPY src/Gateway/Climato.Gateway.Application/Climato.Gateway.Application.csproj src/Gateway/Climato.Gateway.Application/
COPY src/Gateway/Climato.Gateway.Domain/Climato.Gateway.Domain.csproj src/Gateway/Climato.Gateway.Domain/
COPY src/Gateway/Climato.Gateway.Infrastructure/Climato.Gateway.Infrastructure.csproj src/Gateway/Climato.Gateway.Infrastructure/
RUN dotnet restore src/Gateway/Climato.Gateway.Api/Climato.Gateway.Api.csproj

COPY src/Gateway/ src/Gateway/
RUN dotnet publish src/Gateway/Climato.Gateway.Api/Climato.Gateway.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Climato.Gateway.Api.dll"]
