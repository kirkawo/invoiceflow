using InvoiceFlow.Domain;

namespace InvoiceFlow.Application.Abstractions;

public interface IInvoicePdfService
{
    byte[] GeneratePdf(Invoice invoice);
}
