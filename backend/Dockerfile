From mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY TurneraJardin.Api.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish TurneraJardin.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "TurneraJardin.Api.dll"]