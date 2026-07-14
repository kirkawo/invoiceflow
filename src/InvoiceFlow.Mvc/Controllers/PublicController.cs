using InvoiceFlow.Application.Invoices;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceFlow.Mvc.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PublicController : ControllerBase
{
    private readonly PublicInvoiceService _publicInvoiceService;

    public PublicController(PublicInvoiceService publicInvoiceService)
    {
        _publicInvoiceService = publicInvoiceService;
    }

    [HttpGet("invoices/{publicId}")]
    public async Task<IActionResult> GetPublicInvoice(string publicId)
    {
        var invoice = await _publicInvoiceService.GetPublicInvoiceAsync(publicId);
        return invoice is not null ? Ok(invoice) : NotFound();
    }
}
