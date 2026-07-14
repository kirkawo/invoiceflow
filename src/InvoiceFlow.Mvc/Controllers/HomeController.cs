using System.Security.Claims;
using InvoiceFlow.Application.Clients;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Application.Reminders;
using InvoiceFlow.Domain;
using InvoiceFlow.Mvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceFlow.Mvc.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ClientService _clientService;
    private readonly InvoiceService _invoiceService;

    public HomeController(ClientService clientService, InvoiceService invoiceService)
    {
        _clientService = clientService;
        _invoiceService = invoiceService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var workspaceId = GetWorkspaceId();
            var clients = await _clientService.GetClientsAsync(workspaceId);
            ViewBag.Clients = clients;

            var allInvoices = await _invoiceService.GetAllInvoicesAsync(workspaceId);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            ViewBag.OverdueCount = allInvoices.Count(i => i.Status == InvoiceStatus.Overdue);
            ViewBag.UpcomingCount = allInvoices.Count(i =>
                i.Status == InvoiceStatus.Issued
                && (DateOnly.FromDateTime(i.DueDateUtc).DayNumber - today.DayNumber) is >= 0 and <= 3);
        }
        catch (Exception ex)
        {
            ViewBag.Error = ex.Message;
            ViewBag.Clients = (IReadOnlyList<ClientDto>)Array.Empty<ClientDto>();
            ViewBag.OverdueCount = 0;
            ViewBag.UpcomingCount = 0;
        }

        return View();
    }

    public IActionResult Error()
    {
        return View();
    }

    private Guid GetWorkspaceId()
    {
        var claim = User.FindFirst("WorkspaceId");
        if (claim is not null && Guid.TryParse(claim.Value, out var workspaceId))
            return workspaceId;
        throw new UnauthorizedAccessException("Unable to determine workspace from authentication state.");
    }
}
