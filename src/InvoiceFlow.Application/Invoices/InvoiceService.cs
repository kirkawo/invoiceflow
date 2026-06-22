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
        string? number,
        DateTime issueDateUtc,
        DateTime dueDateUtc,
        string currency,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        if (clientId == Guid.Empty)
            throw new ArgumentException("Client ID cannot be empty.", nameof(clientId));

        if (string.IsNullOrWhiteSpace(number))
            number = await _invoiceRepository.GetNextInvoiceNumberAsync(cancellationToken);

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

    public async Task<IReadOnlyList<InvoiceDto>> GetAllInvoicesAsync(
        Guid? clientId = null,
        InvoiceStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var invoices = await _invoiceRepository.ListAllAsync(cancellationToken);

        if (clientId.HasValue)
            invoices = invoices.Where(i => i.ClientId == clientId.Value).ToList();

        if (status.HasValue)
            invoices = invoices.Where(i => i.Status == status.Value).ToList();

        return invoices.Select(MapToDto).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<InvoiceDto>> GetClientInvoicesAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        var invoices = await _invoiceRepository.ListByClientAsync(clientId, cancellationToken);
        return invoices.Select(MapToDto).ToList().AsReadOnly();
    }

    public async Task IssueInvoiceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(id, cancellationToken);
        if (invoice is null)
            throw new InvalidOperationException($"Invoice with ID '{id}' not found.");

        invoice.Issue();
        await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
    }

    public async Task MarkInvoicePaidAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(id, cancellationToken);
        if (invoice is null)
            throw new InvalidOperationException($"Invoice with ID '{id}' not found.");

        invoice.MarkPaid();
        await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
    }

    public async Task MarkInvoiceOverdueAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(id, cancellationToken);
        if (invoice is null)
            throw new InvalidOperationException($"Invoice with ID '{id}' not found.");

        invoice.MarkOverdue();
        await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
    }

    public async Task CancelInvoiceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(id, cancellationToken);
        if (invoice is null)
            throw new InvalidOperationException($"Invoice with ID '{id}' not found.");

        invoice.Cancel();
        await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
    }

    public async Task<int> AddLineItemAsync(
        Guid invoiceId,
        string description,
        decimal quantity,
        decimal unitPrice,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfNegative(unitPrice);

        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId, cancellationToken)
            ?? throw new InvalidOperationException($"Invoice with ID '{invoiceId}' not found.");

        invoice.AddLineItem(description, quantity, unitPrice);
        await _invoiceRepository.UpdateAsync(invoice, cancellationToken);

        var added = invoice.LineItems.Last();
        return added.Id;
    }

    public async Task UpdateLineItemAsync(
        Guid invoiceId,
        int lineItemId,
        string description,
        decimal quantity,
        decimal unitPrice,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfNegative(unitPrice);

        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId, cancellationToken)
            ?? throw new InvalidOperationException($"Invoice with ID '{invoiceId}' not found.");

        invoice.UpdateLineItem(lineItemId, description, quantity, unitPrice);
        await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
    }

    public async Task RemoveLineItemAsync(
        Guid invoiceId,
        int lineItemId,
        CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId, cancellationToken)
            ?? throw new InvalidOperationException($"Invoice with ID '{invoiceId}' not found.");

        invoice.RemoveLineItem(lineItemId);
        await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
    }

    public async Task<Invoice?> LoadInvoiceDomainAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _invoiceRepository.GetByIdAsync(id, cancellationToken);
    }

    private static InvoiceDto MapToDto(Invoice invoice) =>
        new()
        {
            Id = invoice.Id,
            ClientId = invoice.ClientId,
            Number = invoice.Number,
            PublicId = invoice.PublicId,
            IssueDateUtc = invoice.IssueDateUtc,
            DueDateUtc = invoice.DueDateUtc,
            Status = invoice.Status,
            Currency = invoice.Currency,
            Total = invoice.Total,
            Notes = invoice.Notes,
            IssuedAtUtc = invoice.IssuedAtUtc,
            PaidAtUtc = invoice.PaidAtUtc,
            CancelledAtUtc = invoice.CancelledAtUtc,
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
