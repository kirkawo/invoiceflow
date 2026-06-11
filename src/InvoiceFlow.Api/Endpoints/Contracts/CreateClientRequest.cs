namespace InvoiceFlow.Api.Endpoints.Contracts;

public record CreateClientRequest(
    string Name,
    string Email,
    string? CompanyName);
