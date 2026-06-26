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

    private static readonly (string Name, string Email, string? Company)[][] ClientBatches =
    [
        [
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
        ],
        [
            ("Pied Piper", "billing@piedpiper.com", (string?)null),
            ("Dunder Mifflin", "accounting@dundermifflin.com", (string?)null),
            ("Sterling Cooper Draper Pryce", "finance@scdp.com", (string?)null),
            ("Weyland-Yutani", "billing@weyland.com", "Weyland-Yutani Corp"),
            ("Tyrell Corporation", "invoices@tyrell.com", "Tyrell Corporation"),
            ("Buy n Large", "ar@bnl.com", "Buy n Large Inc"),
            ("Oceanic Airlines", "billing@oceanic-air.com", (string?)null),
            ("Aperture Science", "invoices@aperture.com", "Aperture Science Inc"),
            ("Black Mesa Research", "finance@blackmesa.com", "Black Mesa Research"),
            ("Dharma Initiative", "billing@dharma.org", "Dharma Initiative"),
            ("Solyent Green", "payables@solyent.com", (string?)null),
            ("Vaught Industries", "accounting@vaught.com", "Vaught Industries"),
        ],
    ];

    public static async Task SeedAsync(InvoiceFlowDbContext context, bool refresh = false, bool append = false)
    {
        var workspace = await context.Workspaces.FirstOrDefaultAsync(w => w.Name == "Default");
        if (workspace is null)
        {
            workspace = new Workspace("Default");
            context.Workspaces.Add(workspace);
            await context.SaveChangesAsync();
        }

        var hasExistingData = await context.Clients.AnyAsync(c => c.WorkspaceId == workspace.Id);

        if (hasExistingData)
        {
            if (refresh)
            {
                await ClearWorkspaceDataAsync(context, workspace.Id);
            }
            else if (append)
            {
                var nextBatch = await DetermineNextBatchAsync(context, workspace.Id);
                if (nextBatch < 0)
                    return;

                await SeedBatchAsync(context, workspace.Id, nextBatch);
                return;
            }
            else
            {
                return;
            }
        }

        await SeedBatchAsync(context, workspace.Id, 0);
    }

    private static async Task SeedBatchAsync(InvoiceFlowDbContext context, Guid workspaceId, int batchIndex)
    {
        var rng = new Random(42 * (batchIndex + 1));
        var today = DateTime.UtcNow.Date;
        var startingInvoiceNumber = await GetMaxInvoiceSequenceAsync(context, workspaceId, today.Year);

        var clients = await CreateClientsForBatchAsync(context, workspaceId, batchIndex);
        await CreateInvoicesAsync(context, workspaceId, clients, rng, today, startingInvoiceNumber);
    }

    private static async Task<int> DetermineNextBatchAsync(InvoiceFlowDbContext context, Guid workspaceId)
    {
        for (var batch = 1; batch < ClientBatches.Length; batch++)
        {
            var firstName = ClientBatches[batch][0].Name;
            var exists = await context.Clients.AnyAsync(c => c.WorkspaceId == workspaceId && c.Name == firstName);
            if (!exists)
                return batch;
        }

        return -1;
    }

    private static async Task<int> GetMaxInvoiceSequenceAsync(InvoiceFlowDbContext context, Guid workspaceId, int year)
    {
        var prefix = $"INV-{year}-";
        var existingNumbers = await context.Invoices
            .Where(i => i.WorkspaceId == workspaceId && i.Number.StartsWith(prefix))
            .Select(i => i.Number)
            .ToListAsync();

        return existingNumbers
            .Select(n => int.TryParse(n[prefix.Length..], out var s) ? s : 0)
            .DefaultIfEmpty(0)
            .Max();
    }

    private static async Task ClearWorkspaceDataAsync(InvoiceFlowDbContext context, Guid workspaceId)
    {
        var invoices = await context.Invoices
            .Where(i => i.WorkspaceId == workspaceId)
            .ToListAsync();

        context.Invoices.RemoveRange(invoices);
        await context.SaveChangesAsync();

        var clients = await context.Clients
            .Where(c => c.WorkspaceId == workspaceId)
            .ToListAsync();

        context.Clients.RemoveRange(clients);
        await context.SaveChangesAsync();
    }

    private static async Task<List<Client>> CreateClientsForBatchAsync(InvoiceFlowDbContext context, Guid workspaceId, int batchIndex)
    {
        var clientData = ClientBatches[batchIndex];
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
        DateTime today,
        int startingInvoiceNumber)
    {
        var invoiceNumber = startingInvoiceNumber;

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
