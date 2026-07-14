using System.Security.Claims;
using InvoiceFlow.Application.Clients;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Mvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceFlow.Mvc.Controllers;

[Authorize]
public class ClientsController : Controller
{
    private readonly ClientService _clientService;
    private readonly InvoiceService _invoiceService;

    public ClientsController(ClientService clientService, InvoiceService invoiceService)
    {
        _clientService = clientService;
        _invoiceService = invoiceService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var clients = await _clientService.GetClientsAsync(GetWorkspaceId());
            return View(clients);
        }
        catch (Exception ex)
        {
            ViewBag.Error = ex.Message;
            return View((IReadOnlyList<ClientDto>)Array.Empty<ClientDto>());
        }
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string email, string? companyName)
    {
        try
        {
            await _clientService.CreateClientAsync(GetWorkspaceId(), name, email, companyName);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ViewBag.Error = ex.Message;
            return View();
        }
    }

    public async Task<IActionResult> Invoices(Guid clientId)
    {
        try
        {
            var workspaceId = GetWorkspaceId();
            var allClients = await _clientService.GetClientsAsync(workspaceId);
            var client = allClients.FirstOrDefault(c => c.Id == clientId);
            ViewBag.ClientName = client?.Name ?? "Unknown";
            ViewBag.ClientId = clientId;

            var invoices = await _invoiceService.GetClientInvoicesAsync(clientId, workspaceId);
            ViewBag.Invoices = invoices.OrderByDescending(i => i.DueDateUtc).ToList().AsReadOnly();
        }
        catch (Exception ex)
        {
            ViewBag.Error = ex.Message;
            ViewBag.Invoices = (IReadOnlyList<InvoiceDto>)Array.Empty<InvoiceDto>();
        }

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
