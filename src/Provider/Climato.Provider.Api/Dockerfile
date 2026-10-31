FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Provider/Climato.Provider.Api/Climato.Provider.Api.csproj src/Provider/Climato.Provider.Api/
COPY src/Provider/Climato.Provider.Application/Climato.Provider.Application.csproj src/Provider/Climato.Provider.Application/
COPY src/Provider/Climato.Provider.Domain/Climato.Provider.Domain.csproj src/Provider/Climato.Provider.Domain/
RUN dotnet restore src/Provider/Climato.Provider.Api/Climato.Provider.Api.csproj

COPY src/Provider/ src/Provider/
RUN dotnet publish src/Provider/Climato.Provider.Api/Climato.Provider.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Climato.Provider.Api.dll"]
