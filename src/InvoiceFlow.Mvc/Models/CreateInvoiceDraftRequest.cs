namespace InvoiceFlow.Mvc.Models;

public record CreateInvoiceDraftRequest(
    Guid ClientId,
    string? Number,
    DateTime IssueDateUtc,
    DateTime DueDateUtc,
    string Currency,
    string? Notes);
