# Imagen para publicar Alma Case en Render (u otro hosting con Docker)

# 1) Compilar
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY AlmaCase.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

# 2) Ejecutar
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_ENVIRONMENT=Production \
    TZ=America/Bogota
EXPOSE 8080
ENTRYPOINT ["dotnet", "AlmaCase.dll"]
