FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/FinancialTracker.Transactions.Domain/FinancialTracker.Transactions.Domain.csproj src/FinancialTracker.Transactions.Domain/
COPY src/FinancialTracker.Transactions.Application/FinancialTracker.Transactions.Application.csproj src/FinancialTracker.Transactions.Application/
COPY src/FinancialTracker.Transactions.Infrastructure/FinancialTracker.Transactions.Infrastructure.csproj src/FinancialTracker.Transactions.Infrastructure/
COPY src/FinancialTracker.Transactions.API/FinancialTracker.Transactions.API.csproj src/FinancialTracker.Transactions.API/
RUN dotnet restore src/FinancialTracker.Transactions.API/FinancialTracker.Transactions.API.csproj

COPY src/FinancialTracker.Transactions.Domain/ src/FinancialTracker.Transactions.Domain/
COPY src/FinancialTracker.Transactions.Application/ src/FinancialTracker.Transactions.Application/
COPY src/FinancialTracker.Transactions.Infrastructure/ src/FinancialTracker.Transactions.Infrastructure/
COPY src/FinancialTracker.Transactions.API/ src/FinancialTracker.Transactions.API/
RUN dotnet publish src/FinancialTracker.Transactions.API/FinancialTracker.Transactions.API.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FinancialTracker.Transactions.API.dll"]
