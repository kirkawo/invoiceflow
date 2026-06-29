FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY InvoiceFlow.sln ./
COPY src/InvoiceFlow.Domain/InvoiceFlow.Domain.csproj src/InvoiceFlow.Domain/
COPY src/InvoiceFlow.Application/InvoiceFlow.Application.csproj src/InvoiceFlow.Application/
COPY src/InvoiceFlow.Infrastructure/InvoiceFlow.Infrastructure.csproj src/InvoiceFlow.Infrastructure/
COPY src/InvoiceFlow.Pdf/InvoiceFlow.Pdf.csproj src/InvoiceFlow.Pdf/
COPY src/InvoiceFlow.Web/InvoiceFlow.Web.csproj src/InvoiceFlow.Web/
RUN dotnet restore src/InvoiceFlow.Web/InvoiceFlow.Web.csproj

COPY . .
RUN dotnet publish src/InvoiceFlow.Web/InvoiceFlow.Web.csproj --configuration Release --output /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
RUN mkdir -p /app/DataProtection-Keys && chown $APP_UID:$APP_UID /app/DataProtection-Keys
USER $APP_UID
ENTRYPOINT ["dotnet", "InvoiceFlow.Web.dll"]
