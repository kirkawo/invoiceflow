using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Domain;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InvoiceFlow.Pdf;

public class InvoicePdfService : IInvoicePdfService
{
    static InvoicePdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GeneratePdf(Invoice invoice)
    {
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(c => ComposeHeader(c, invoice));
                page.Content().Element(c => ComposeContent(c, invoice));
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("InvoiceFlow - ");
                    t.CurrentPageNumber();
                });
            });
        }).GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, Invoice invoice)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("INVOICE").FontSize(24).Bold().FontColor(Colors.Blue.Darken3);
                    c.Item().Text($"#{invoice.Number}").FontSize(14).Light();
                });

                row.RelativeItem().AlignRight().Column(c =>
                {
                    c.Item().Text($"Status: {invoice.Status}").Bold();
                    c.Item().Text($"Issued: {invoice.IssueDateUtc:MMM dd, yyyy}");
                    c.Item().Text($"Due: {invoice.DueDateUtc:MMM dd, yyyy}");
                });
            });

            col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private static void ComposeContent(IContainer container, Invoice invoice)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Bill To").FontSize(9).Bold().FontColor(Colors.Grey.Medium);
                    c.Item().PaddingTop(4).Text(invoice.ClientId.ToString()).FontSize(9).FontColor(Colors.Grey.Medium);
                });

                row.RelativeItem().AlignRight().Column(c =>
                {
                    c.Item().Text("Currency").FontSize(9).Bold().FontColor(Colors.Grey.Medium);
                    c.Item().PaddingTop(4).Text(invoice.Currency).FontSize(10);
                });
            });

            col.Item().PaddingVertical(15);

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text("Description").Bold().FontSize(9);
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignRight().Text("Qty").Bold().FontSize(9);
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignRight().Text("Unit Price").Bold().FontSize(9);
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignRight().Text("Amount").Bold().FontSize(9);
                });

                foreach (var item in invoice.LineItems)
                {
                    table.Cell().PaddingVertical(4).PaddingHorizontal(6).Text(item.Description).FontSize(9);
                    table.Cell().PaddingVertical(4).AlignRight().Text(item.Quantity.ToString("0.##")).FontSize(9);
                    table.Cell().PaddingVertical(4).AlignRight().Text(item.UnitPrice.ToString("F2")).FontSize(9);
                    table.Cell().PaddingVertical(4).AlignRight().Text(item.Amount.ToString("F2")).FontSize(9);
                }
            });

            col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            col.Item().AlignRight().Column(c =>
            {
                c.Item().PaddingVertical(2).Row(r =>
                {
                    r.RelativeItem().Text("Total:").Bold().FontSize(12);
                    r.ConstantItem(100).AlignRight().Text($"{invoice.Currency} {invoice.Total:F2}").Bold().FontSize(12);
                });
            });

            if (!string.IsNullOrWhiteSpace(invoice.Notes))
            {
                col.Item().PaddingTop(20);
                col.Item().Text("Notes").FontSize(9).Bold().FontColor(Colors.Grey.Medium);
                col.Item().PaddingTop(4).Text(invoice.Notes).FontSize(9).FontColor(Colors.Grey.Darken1);
            }
        });
    }
}
