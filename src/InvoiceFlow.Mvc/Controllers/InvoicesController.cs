using System.Security.Claims;
using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Application.Clients;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Application.Reminders;
using InvoiceFlow.Domain;
using InvoiceFlow.Mvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceFlow.Mvc.Controllers;

[Authorize]
public class InvoicesController : Controller
{
    private readonly InvoiceService _invoiceService;
    private readonly ClientService _clientService;
    private readonly IInvoicePdfService _pdfService;
    private readonly ManualReminderService _reminderService;
    private readonly InvoiceDeliveryService _deliveryService;

    public InvoicesController(
        InvoiceService invoiceService,
        ClientService clientService,
        IInvoicePdfService pdfService,
        ManualReminderService reminderService,
        InvoiceDeliveryService deliveryService)
    {
        _invoiceService = invoiceService;
        _clientService = clientService;
        _pdfService = pdfService;
        _reminderService = reminderService;
        _deliveryService = deliveryService;
    }

    public async Task<IActionResult> Index(Guid? clientId, InvoiceStatus? status)
    {
        try
        {
            var workspaceId = GetWorkspaceId();
            ViewBag.Clients = await _clientService.GetClientsAsync(workspaceId);
            ViewBag.Invoices = await _invoiceService.GetAllInvoicesAsync(workspaceId, clientId, status);
            ViewBag.SelectedClientId = clientId?.ToString() ?? "";
            ViewBag.SelectedStatus = status?.ToString() ?? "";
        }
        catch (Exception ex)
        {
            ViewBag.Error = ex.Message;
            ViewBag.Clients = (IReadOnlyList<ClientDto>)Array.Empty<ClientDto>();
            ViewBag.Invoices = (IReadOnlyList<InvoiceDto>)Array.Empty<InvoiceDto>();
        }

        return View();
    }

    public async Task<IActionResult> Details(Guid id)
    {
        try
        {
            var workspaceId = GetWorkspaceId();
            var invoice = await _invoiceService.GetInvoiceAsync(id, workspaceId);
            if (invoice is null) return NotFound();

            var clients = await _clientService.GetClientsAsync(workspaceId);
            var client = clients.FirstOrDefault(c => c.Id == invoice.ClientId);

            ViewBag.Invoice = invoice;
            ViewBag.ClientName = client?.Name ?? "Unknown";
            ViewBag.ClientEmail = client?.Email;

            if (invoice.Status is InvoiceStatus.Issued or InvoiceStatus.Overdue or InvoiceStatus.Paid)
                ViewBag.Deliveries = await _deliveryService.GetDeliveryHistoryAsync(id, workspaceId);
            else
                ViewBag.Deliveries = (IReadOnlyList<ReminderDto>)Array.Empty<ReminderDto>();

            if (invoice.Status != InvoiceStatus.Draft)
                ViewBag.Reminders = await _reminderService.GetReminderHistoryAsync(id, workspaceId);
            else
                ViewBag.Reminders = (IReadOnlyList<ReminderDto>)Array.Empty<ReminderDto>();
        }
        catch (Exception ex)
        {
            ViewBag.Error = ex.Message;
        }

        return View();
    }

    public async Task<IActionResult> Create(Guid? clientId)
    {
        try
        {
            var clients = await _clientService.GetClientsAsync(GetWorkspaceId());
            ViewBag.Clients = clients;
            ViewBag.PreselectedClientId = clientId;
        }
        catch (Exception ex)
        {
            ViewBag.Error = ex.Message;
            ViewBag.Clients = (IReadOnlyList<ClientDto>)Array.Empty<ClientDto>();
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateInvoiceDraftRequest request)
    {
        try
        {
            var workspaceId = GetWorkspaceId();
            var issueDate = request.IssueDateUtc.ToUniversalTime();
            var dueDate = request.DueDateUtc.ToUniversalTime();

            await _invoiceService.CreateInvoiceDraftAsync(
                workspaceId, request.ClientId, null, issueDate, dueDate,
                request.Currency?.ToUpperInvariant() ?? "USD", request.Notes);

            return RedirectToAction(nameof(ClientsController.Invoices), "Clients", new { clientId = request.ClientId });
        }
        catch (Exception ex)
        {
            ViewBag.Error = ex.Message;
            ViewBag.Clients = await _clientService.GetClientsAsync(GetWorkspaceId());
            return View();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Issue(Guid id)
    {
        try
        {
            await _invoiceService.IssueInvoiceAsync(id, GetWorkspaceId());
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPaid(Guid id)
    {
        try
        {
            await _invoiceService.MarkInvoicePaidAsync(id, GetWorkspaceId());
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkOverdue(Guid id)
    {
        try
        {
            await _invoiceService.MarkInvoiceOverdueAsync(id, GetWorkspaceId());
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try
        {
            await _invoiceService.CancelInvoiceAsync(id, GetWorkspaceId());
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddLineItem(Guid id, string description, decimal quantity, decimal unitPrice)
    {
        try
        {
            await _invoiceService.AddLineItemAsync(id, GetWorkspaceId(), description, quantity, unitPrice);
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveLineItem(Guid id, int lineItemId)
    {
        try
        {
            await _invoiceService.RemoveLineItemAsync(id, GetWorkspaceId(), lineItemId);
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Pdf(Guid id)
    {
        var workspaceId = GetWorkspaceId();
        var invoiceDto = await _invoiceService.GetInvoiceAsync(id, workspaceId);
        if (invoiceDto is null) return NotFound();

        var invoice = await _invoiceService.LoadInvoiceDomainAsync(id);
        if (invoice is null) return NotFound();

        if (invoice.LineItems.Count == 0)
            return BadRequest("Cannot generate PDF for an invoice without line items.");

        var pdf = _pdfService.GeneratePdf(invoice);
        return File(pdf, "application/pdf", $"invoice-{invoiceDto.Number}.pdf");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendInvoice(Guid id, string? customMessage)
    {
        try
        {
            await _deliveryService.SendInvoiceEmailAsync(id, GetWorkspaceId(), customMessage);
            TempData["DeliveryFeedback"] = "Invoice sent successfully.";
            TempData["DeliveryFeedbackCss"] = "alert-success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["DeliveryFeedback"] = ex.Message;
            TempData["DeliveryFeedbackCss"] = "alert-danger";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendReminder(Guid id, string? customMessage)
    {
        try
        {
            await _reminderService.SendManualReminderAsync(id, GetWorkspaceId(), customMessage);
            TempData["ReminderFeedback"] = "Reminder sent successfully.";
            TempData["ReminderFeedbackCss"] = "alert-success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ReminderFeedback"] = ex.Message;
            TempData["ReminderFeedbackCss"] = "alert-danger";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private Guid GetWorkspaceId()
    {
        var claim = User.FindFirst("WorkspaceId");
        if (claim is not null && Guid.TryParse(claim.Value, out var workspaceId))
            return workspaceId;
        throw new UnauthorizedAccessException("Unable to determine workspace from authentication state.");
    }
}
