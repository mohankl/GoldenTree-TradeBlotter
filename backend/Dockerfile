# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first (better layer caching) — publish only the API project.
COPY TradeBlotter.Api/TradeBlotter.Api.csproj TradeBlotter.Api/
RUN dotnet restore TradeBlotter.Api/TradeBlotter.Api.csproj

COPY TradeBlotter.Api/ TradeBlotter.Api/
RUN dotnet publish TradeBlotter.Api/TradeBlotter.Api.csproj -c Release -o /app /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_ENVIRONMENT=Production
# Railway injects PORT; the app binds 0.0.0.0:$PORT (see Program.cs).
ENTRYPOINT ["dotnet", "TradeBlotter.Api.dll"]
