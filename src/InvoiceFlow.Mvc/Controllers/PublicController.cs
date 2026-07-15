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
            return NotFound();

        try
        {
            var invoice = await _publicInvoiceService.GetPublicInvoiceAsync(publicId);
            if (invoice is null)
                return NotFound();

            ViewBag.Invoice = invoice;
        }
        catch
        {
            return NotFound();
        }

        return View();
    }
}
