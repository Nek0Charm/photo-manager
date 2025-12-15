# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY backend/*.csproj ./
COPY backend/ ./
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish ./
RUN mkdir -p /data
ENV ASPNETCORE_URLS=http://0.0.0.0:5151
EXPOSE 5151
ENTRYPOINT ["dotnet", "backend.dll"]
