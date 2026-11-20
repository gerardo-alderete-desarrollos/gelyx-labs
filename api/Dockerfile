FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore GelyxLabs.slnx
RUN dotnet publish src/GelyxLabs.Api/GelyxLabs.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build --chown=app:app /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=8080
EXPOSE 8080
USER app

ENTRYPOINT ["sh", "-c", "dotnet GelyxLabs.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
