using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Domain;

namespace InvoiceFlow.Application.Invoices;

public class PublicInvoiceService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IClientRepository _clientRepository;

    public PublicInvoiceService(IInvoiceRepository invoiceRepository, IClientRepository clientRepository)
    {
        _invoiceRepository = invoiceRepository;
        _clientRepository = clientRepository;
    }

    public async Task<PublicInvoiceDto?> GetPublicInvoiceAsync(string publicId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            return null;

        var invoice = await _invoiceRepository.GetByPublicIdAsync(publicId, cancellationToken);
        if (invoice is null)
            return null;

        if (invoice.Status is InvoiceStatus.Draft or InvoiceStatus.Cancelled)
            return null;

        if (invoice.LineItems.Count == 0)
            return null;

        var client = await _clientRepository.GetByIdUnfilteredAsync(invoice.ClientId, cancellationToken);

        return new PublicInvoiceDto
        {
            Number = invoice.Number,
            ClientName = client?.Name ?? "Unknown",
            ClientEmail = client?.Email,
            ClientCompany = client?.CompanyName,
            Status = invoice.Status,
            Currency = invoice.Currency,
            Total = invoice.Total,
            IssueDateUtc = invoice.IssueDateUtc,
            DueDateUtc = invoice.DueDateUtc,
            Notes = invoice.Notes,
            LineItems = invoice.LineItems
                .Select(li => new InvoiceLineItemDto
                {
                    Id = li.Id,
                    Description = li.Description,
                    Quantity = li.Quantity,
                    UnitPrice = li.UnitPrice,
                    Amount = li.Amount
                })
                .ToList()
                .AsReadOnly()
        };
    }

    public async Task<Invoice?> LoadInvoiceDomainByPublicIdAsync(string publicId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            return null;

        var invoice = await _invoiceRepository.GetByPublicIdAsync(publicId, cancellationToken);
        if (invoice is null)
            return null;

        if (invoice.Status is InvoiceStatus.Draft or InvoiceStatus.Cancelled)
            return null;

        if (invoice.LineItems.Count == 0)
            return null;

        return invoice;
    }
}
