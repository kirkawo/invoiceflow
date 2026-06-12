using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Domain;

namespace InvoiceFlow.Application.Invoices;

public class InvoiceService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IClientRepository _clientRepository;
    private readonly ICurrentWorkspaceService _workspaceService;

    public InvoiceService(IInvoiceRepository invoiceRepository, IClientRepository clientRepository, ICurrentWorkspaceService workspaceService)
    {
        _invoiceRepository = invoiceRepository;
        _clientRepository = clientRepository;
        _workspaceService = workspaceService;
    }

    public async Task<Guid> CreateInvoiceDraftAsync(
        Guid clientId,
        string number,
        DateTime issueDateUtc,
        DateTime dueDateUtc,
        string currency,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        if (clientId == Guid.Empty)
            throw new ArgumentException("Client ID cannot be empty.", nameof(clientId));

        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        var clientExists = await _clientRepository.GetByIdAsync(clientId, cancellationToken);
        if (clientExists is null)
            throw new InvalidOperationException($"Client with ID '{clientId}' not found.");

        var invoice = new Invoice(_workspaceService.WorkspaceId, clientId, number, issueDateUtc, dueDateUtc, currency, notes);
        await _invoiceRepository.AddAsync(invoice, cancellationToken);
        return invoice.Id;
    }

    public async Task<InvoiceDto?> GetInvoiceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(id, cancellationToken);
        return invoice is not null ? MapToDto(invoice) : null;
    }

    public async Task<IReadOnlyList<InvoiceDto>> GetClientInvoicesAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        var invoices = await _invoiceRepository.ListByClientAsync(clientId, cancellationToken);
        return invoices.Select(MapToDto).ToList().AsReadOnly();
    }

    private static InvoiceDto MapToDto(Invoice invoice) =>
        new()
        {
            Id = invoice.Id,
            ClientId = invoice.ClientId,
            Number = invoice.Number,
            IssueDateUtc = invoice.IssueDateUtc,
            DueDateUtc = invoice.DueDateUtc,
            Status = invoice.Status,
            Currency = invoice.Currency,
            Total = invoice.Total
        };
}
