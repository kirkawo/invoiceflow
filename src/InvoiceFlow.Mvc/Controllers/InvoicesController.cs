using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Application.Reminders;
using InvoiceFlow.Domain;
using InvoiceFlow.Mvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceFlow.Mvc.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class InvoicesController : ControllerBase
{
    private readonly InvoiceService _invoiceService;
    private readonly IInvoicePdfService _pdfService;
    private readonly ManualReminderService _reminderService;

    public InvoicesController(
        InvoiceService invoiceService,
        IInvoicePdfService pdfService,
        ManualReminderService reminderService)
    {
        _invoiceService = invoiceService;
        _pdfService = pdfService;
        _reminderService = reminderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? clientId, [FromQuery] InvoiceStatus? status)
    {
        var invoices = await _invoiceService.GetAllInvoicesAsync(clientId, status);
        return Ok(invoices);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var invoice = await _invoiceService.GetInvoiceAsync(id);
        return invoice is not null ? Ok(invoice) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> CreateDraft([FromBody] CreateInvoiceDraftRequest request)
    {
        try
        {
            var id = await _invoiceService.CreateInvoiceDraftAsync(
                request.ClientId,
                request.Number,
                request.IssueDateUtc,
                request.DueDateUtc,
                request.Currency,
                request.Notes);

            return Created($"/api/invoices/{id}", new { id });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/issue")]
    public async Task<IActionResult> Issue(Guid id)
    {
        try
        {
            await _invoiceService.IssueInvoiceAsync(id);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/mark-paid")]
    public async Task<IActionResult> MarkPaid(Guid id)
    {
        try
        {
            await _invoiceService.MarkInvoicePaidAsync(id);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/mark-overdue")]
    public async Task<IActionResult> MarkOverdue(Guid id)
    {
        try
        {
            await _invoiceService.MarkInvoiceOverdueAsync(id);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try
        {
            await _invoiceService.CancelInvoiceAsync(id);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/line-items")]
    public async Task<IActionResult> AddLineItem(Guid id, [FromBody] AddLineItemRequest request)
    {
        try
        {
            var lineItemId = await _invoiceService.AddLineItemAsync(id, request.Description, request.Quantity, request.UnitPrice);
            return Created($"/api/invoices/{id}/line-items/{lineItemId}", new { lineItemId });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}/line-items/{lineItemId:int}")]
    public async Task<IActionResult> UpdateLineItem(Guid id, int lineItemId, [FromBody] UpdateLineItemRequest request)
    {
        try
        {
            await _invoiceService.UpdateLineItemAsync(id, lineItemId, request.Description, request.Quantity, request.UnitPrice);
            return Ok();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}/line-items/{lineItemId:int}")]
    public async Task<IActionResult> RemoveLineItem(Guid id, int lineItemId)
    {
        try
        {
            await _invoiceService.RemoveLineItemAsync(id, lineItemId);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> GetPdf(Guid id)
    {
        var invoiceDto = await _invoiceService.GetInvoiceAsync(id);
        if (invoiceDto is null)
            return NotFound();

        var invoice = await _invoiceService.LoadInvoiceDomainAsync(id);
        if (invoice is null)
            return NotFound();

        if (invoice.LineItems.Count == 0)
            return BadRequest(new { error = "Cannot generate PDF for an invoice without line items." });

        var pdf = _pdfService.GeneratePdf(invoice);
        return File(pdf, "application/pdf", $"invoice-{invoiceDto.Number}.pdf");
    }

    [HttpPost("{id:guid}/reminders/manual")]
    public async Task<IActionResult> SendManualReminder(Guid id, [FromQuery] string? message)
    {
        try
        {
            var reminder = await _reminderService.SendManualReminderAsync(id, message);
            return Created($"/api/invoices/{id}/reminders", reminder);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}/reminders")]
    public async Task<IActionResult> GetReminders(Guid id)
    {
        var reminders = await _reminderService.GetReminderHistoryAsync(id);
        return Ok(reminders);
    }

    [HttpGet("~/api/clients/{clientId:guid}/invoices")]
    public async Task<IActionResult> GetClientInvoices(Guid clientId)
    {
        var invoices = await _invoiceService.GetClientInvoicesAsync(clientId);
        return Ok(invoices);
    }
}
