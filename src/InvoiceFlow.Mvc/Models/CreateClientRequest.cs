namespace InvoiceFlow.Mvc.Models;

public record CreateClientRequest(
    string Name,
    string Email,
    string? CompanyName);
