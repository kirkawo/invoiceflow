FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY InvoiceFlow.sln ./
COPY src/InvoiceFlow.Domain/InvoiceFlow.Domain.csproj src/InvoiceFlow.Domain/
COPY src/InvoiceFlow.Application/InvoiceFlow.Application.csproj src/InvoiceFlow.Application/
COPY src/InvoiceFlow.Infrastructure/InvoiceFlow.Infrastructure.csproj src/InvoiceFlow.Infrastructure/
COPY src/InvoiceFlow.Pdf/InvoiceFlow.Pdf.csproj src/InvoiceFlow.Pdf/
COPY src/InvoiceFlow.Api/InvoiceFlow.Api.csproj src/InvoiceFlow.Api/
RUN dotnet restore src/InvoiceFlow.Api/InvoiceFlow.Api.csproj

COPY . .
RUN dotnet publish src/InvoiceFlow.Api/InvoiceFlow.Api.csproj --configuration Release --output /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
RUN mkdir -p /app/DataProtection-Keys && chown $APP_UID:$APP_UID /app/DataProtection-Keys
USER $APP_UID
ENTRYPOINT ["dotnet", "InvoiceFlow.Api.dll"]
