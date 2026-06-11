using InvoiceFlow.Application;
using InvoiceFlow.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure();

var app = builder.Build();

app.UseHttpsRedirection();

app.Run();
