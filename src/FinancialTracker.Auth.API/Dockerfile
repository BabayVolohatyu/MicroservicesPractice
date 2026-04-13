FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/FinancialTracker.Auth.Domain/FinancialTracker.Auth.Domain.csproj src/FinancialTracker.Auth.Domain/
COPY src/FinancialTracker.Auth.Application/FinancialTracker.Auth.Application.csproj src/FinancialTracker.Auth.Application/
COPY src/FinancialTracker.Auth.Infrastructure/FinancialTracker.Auth.Infrastructure.csproj src/FinancialTracker.Auth.Infrastructure/
COPY src/FinancialTracker.Auth.API/FinancialTracker.Auth.API.csproj src/FinancialTracker.Auth.API/
RUN dotnet restore src/FinancialTracker.Auth.API/FinancialTracker.Auth.API.csproj

COPY src/FinancialTracker.Auth.Domain/ src/FinancialTracker.Auth.Domain/
COPY src/FinancialTracker.Auth.Application/ src/FinancialTracker.Auth.Application/
COPY src/FinancialTracker.Auth.Infrastructure/ src/FinancialTracker.Auth.Infrastructure/
COPY src/FinancialTracker.Auth.API/ src/FinancialTracker.Auth.API/
RUN dotnet publish src/FinancialTracker.Auth.API/FinancialTracker.Auth.API.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FinancialTracker.Auth.API.dll"]
