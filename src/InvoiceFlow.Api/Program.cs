using InvoiceFlow.Application;
using InvoiceFlow.Infrastructure;
using InvoiceFlow.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapClientEndpoints();
app.MapInvoiceEndpoints();
app.MapClientInvoiceEndpoints();

app.Run();

public partial class Program { }
