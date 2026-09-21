FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG SERVICE_PROJECT
WORKDIR /src

COPY . .
RUN dotnet publish "${SERVICE_PROJECT}" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG SERVICE_DLL
ENV SERVICE_DLL=${SERVICE_DLL}
WORKDIR /app

# libgssapi-krb5-2 — Npgsql при старте пробует загрузить GSSAPI, и без библиотеки нативный
# загрузчик пишет в stderr строку мимо Serilog, ломая правило «каждая строка лога — JSON».
# curl — нужен healthcheck'у из docker-compose.yml, в базовом образе HTTP-клиента нет.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Порт не задаём: базовый образ уже выставляет ASPNETCORE_HTTP_PORTS=8080.
EXPOSE 8080

ENTRYPOINT dotnet ${SERVICE_DLL}
