# syntax=docker/dockerfile:1.7
ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src
COPY Whodat.Api/Whodat.Api.csproj Whodat.Api/
RUN dotnet restore Whodat.Api/Whodat.Api.csproj
COPY Whodat.Api/ Whodat.Api/
# `appsettings.json` is gitignored so contributors don't leak local tweaks.
# Fall back to the committed example when building from a clean clone.
RUN test -f Whodat.Api/appsettings.json || cp Whodat.Api/appsettings.json.example Whodat.Api/appsettings.json
RUN dotnet publish Whodat.Api/Whodat.Api.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
WORKDIR /app

RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

COPY --from=build /app ./

RUN mkdir -p /data /app/logs && chown -R app:app /data /app
USER app

ENV ASPNETCORE_URLS=http://+:8080
ENV Whodat__DbPath=/data/whodat.db
EXPOSE 8080

ENTRYPOINT ["dotnet", "Whodat.Api.dll"]
