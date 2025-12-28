# syntax=docker/dockerfile:1

ARG DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:9.0
ARG DOTNET_ASP_IMAGE=mcr.microsoft.com/dotnet/aspnet:9.0

FROM ${DOTNET_SDK_IMAGE} AS build
WORKDIR /src

COPY backend.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM ${DOTNET_ASP_IMAGE} AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "backend.dll"]
