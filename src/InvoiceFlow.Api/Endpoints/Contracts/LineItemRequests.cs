namespace InvoiceFlow.Api.Endpoints.Contracts;

public record AddLineItemRequest(string Description, decimal Quantity, decimal UnitPrice);

public record UpdateLineItemRequest(string Description, decimal Quantity, decimal UnitPrice);
