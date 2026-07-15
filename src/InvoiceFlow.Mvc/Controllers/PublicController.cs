using InvoiceFlow.Application.Invoices;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceFlow.Mvc.Controllers;

public class PublicController : Controller
{
    private readonly PublicInvoiceService _publicInvoiceService;

    public PublicController(PublicInvoiceService publicInvoiceService)
    {
        _publicInvoiceService = publicInvoiceService;
    }

    public async Task<IActionResult> Invoice(string? publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            ViewBag.Error = "Invoice not found.";
            return View();
        }

        try
        {
            var invoice = await _publicInvoiceService.GetPublicInvoiceAsync(publicId);
            if (invoice is null)
            {
                ViewBag.Error = "Invoice not found.";
                return View();
            }

            ViewBag.Invoice = invoice;
        }
        catch
        {
            ViewBag.Error = "Invoice not found.";
        }

        return View();
    }
}
