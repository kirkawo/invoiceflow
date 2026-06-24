using InvoiceFlow.Domain;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Infrastructure.Persistence;

public static class InvoiceFlowSampleDataSeeder
{
    private static readonly string[] ItemDescriptions =
    [
        "Consulting services",
        "Software license",
        "Web development",
        "Design work",
        "Hosting fees",
        "Support retainer",
        "Training session",
        "API access",
        "Maintenance contract",
        "Data migration",
    ];

    public static async Task SeedAsync(InvoiceFlowDbContext context)
    {
        var workspace = await context.Workspaces.FirstOrDefaultAsync(w => w.Name == "Default");
        if (workspace is null)
        {
            workspace = new Workspace("Default");
            context.Workspaces.Add(workspace);
            await context.SaveChangesAsync();
        }

        if (await context.Clients.AnyAsync(c => c.WorkspaceId == workspace.Id))
            return;

        var rng = new Random(42);
        var today = DateTime.UtcNow.Date;

        var clients = await CreateClientsAsync(context, workspace.Id);
        await CreateInvoicesAsync(context, workspace.Id, clients, rng, today);
    }

    private static async Task<List<Client>> CreateClientsAsync(InvoiceFlowDbContext context, Guid workspaceId)
    {
        var clientData = new[]
        {
            ("Acme Corp", "billing@acme.com", "Acme Corporation"),
            ("Globex Inc", "ar@globex.com", "Globex Industries"),
            ("Initech", "finance@initech.com", (string?)null),
            ("Umbrella Co", "payments@umbrella.com", "Umbrella Corporation"),
            ("Hooli", "accounting@hooli.com", "Hooli Technologies"),
            ("Stark Industries", "invoices@stark.com", "Stark Industries"),
            ("Wayne Enterprises", "finance@wayne.com", "Wayne Enterprises"),
            ("Cyberdyne Systems", "billing@cyberdyne.com", "Cyberdyne Systems"),
            ("Oscorp", "ar@oscorp.com", "Oscorp Inc"),
            ("Massive Dynamic", "payables@massive.com", "Massive Dynamic"),
            ("Soylent Corp", "billing@soylent.com", "Soylent Corporation"),
            ("Wonka Industries", "finance@wonka.com", "Wonka Industries"),
        };

        var clients = new List<Client>(clientData.Length);
        foreach (var (name, email, company) in clientData)
        {
            clients.Add(new Client(workspaceId, name, email, company));
        }

        context.Clients.AddRange(clients);
        await context.SaveChangesAsync();
        return clients;
    }

    private static async Task CreateInvoicesAsync(
        InvoiceFlowDbContext context,
        Guid workspaceId,
        List<Client> clients,
        Random rng,
        DateTime today)
    {
        var invoiceNumber = 0;

        foreach (var client in clients)
        {
            var invoiceCount = 2 + rng.Next(7);

            for (var i = 0; i < invoiceCount; i++)
            {
                invoiceNumber++;
                var issueDate = today.AddDays(rng.Next(-60, 31));
                var dueDate = issueDate.AddDays(15 + rng.Next(46));
                var currency = rng.Next(3) == 0 ? "EUR" : "USD";

                var invoice = new Invoice(
                    workspaceId,
                    client.Id,
                    $"INV-{today.Year}-{invoiceNumber:D4}",
                    issueDate,
                    dueDate,
                    currency);

                var lineItemCount = 1 + rng.Next(6);
                for (var j = 0; j < lineItemCount; j++)
                {
                    var description = ItemDescriptions[rng.Next(ItemDescriptions.Length)];
                    var quantity = 1 + rng.Next(20);
                    var unitPrice = Math.Round((decimal)(10 + rng.NextDouble() * 490), 2);
                    invoice.AddLineItem(description, quantity, unitPrice);
                }

                ApplyStatus(invoice, i, rng);

                context.Invoices.Add(invoice);
            }
        }

        await context.SaveChangesAsync();
    }

    private static void ApplyStatus(Invoice invoice, int index, Random rng)
    {
        if (index == 0)
        {
            invoice.Issue();
            return;
        }

        var roll = rng.Next(100);

        if (roll < 10)
            return;

        if (roll < 20)
        {
            invoice.Issue();
            invoice.Cancel();
            return;
        }

        if (roll < 40)
        {
            invoice.Issue();
            return;
        }

        if (roll < 60)
        {
            invoice.Issue();
            invoice.MarkPaid();
            return;
        }

        invoice.Issue();
        invoice.MarkOverdue();
    }
}
