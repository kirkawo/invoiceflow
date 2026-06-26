FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy csproj files for layer caching
COPY InvoiceFlow.sln ./
COPY src/InvoiceFlow.Domain/InvoiceFlow.Domain.csproj src/InvoiceFlow.Domain/
COPY src/InvoiceFlow.Application/InvoiceFlow.Application.csproj src/InvoiceFlow.Application/
COPY src/InvoiceFlow.Infrastructure/InvoiceFlow.Infrastructure.csproj src/InvoiceFlow.Infrastructure/
COPY src/InvoiceFlow.Pdf/InvoiceFlow.Pdf.csproj src/InvoiceFlow.Pdf/
COPY src/InvoiceFlow.Web/InvoiceFlow.Web.csproj src/InvoiceFlow.Web/
RUN dotnet restore src/InvoiceFlow.Web/InvoiceFlow.Web.csproj

# build and publish
COPY . .
RUN dotnet publish src/InvoiceFlow.Web/InvoiceFlow.Web.csproj --configuration Release --output /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "InvoiceFlow.Web.dll"]
