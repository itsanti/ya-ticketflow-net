FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG SERVICE_PROJECT
WORKDIR /src

COPY . .
RUN dotnet publish "${SERVICE_PROJECT}" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG SERVICE_DLL
ENV SERVICE_DLL=${SERVICE_DLL}
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT dotnet ${SERVICE_DLL}
