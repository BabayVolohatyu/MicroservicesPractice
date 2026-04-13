FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/FinancialTracker.Accounts.Domain/FinancialTracker.Accounts.Domain.csproj src/FinancialTracker.Accounts.Domain/
COPY src/FinancialTracker.Accounts.Application/FinancialTracker.Accounts.Application.csproj src/FinancialTracker.Accounts.Application/
COPY src/FinancialTracker.Accounts.Infrastructure/FinancialTracker.Accounts.Infrastructure.csproj src/FinancialTracker.Accounts.Infrastructure/
COPY src/FinancialTracker.Accounts.API/FinancialTracker.Accounts.API.csproj src/FinancialTracker.Accounts.API/
RUN dotnet restore src/FinancialTracker.Accounts.API/FinancialTracker.Accounts.API.csproj

COPY src/FinancialTracker.Accounts.Domain/ src/FinancialTracker.Accounts.Domain/
COPY src/FinancialTracker.Accounts.Application/ src/FinancialTracker.Accounts.Application/
COPY src/FinancialTracker.Accounts.Infrastructure/ src/FinancialTracker.Accounts.Infrastructure/
COPY src/FinancialTracker.Accounts.API/ src/FinancialTracker.Accounts.API/
RUN dotnet publish src/FinancialTracker.Accounts.API/FinancialTracker.Accounts.API.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FinancialTracker.Accounts.API.dll"]
