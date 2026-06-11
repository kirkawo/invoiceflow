using InvoiceFlow.Api.Endpoints.Contracts;
using InvoiceFlow.Application.Clients;

namespace InvoiceFlow.Api.Endpoints;

public static class ClientEndpoints
{
    public static IEndpointRouteBuilder MapClientEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clients");

        group.MapPost("/", async (CreateClientRequest request, ClientService clientService) =>
        {
            try
            {
                var id = await clientService.CreateClientAsync(request.Name, request.Email, request.CompanyName);
                return Results.Created($"/api/clients/{id}", new { id });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapGet("/", async (ClientService clientService) =>
        {
            var clients = await clientService.GetClientsAsync();
            return Results.Ok(clients);
        });

        return app;
    }
}
