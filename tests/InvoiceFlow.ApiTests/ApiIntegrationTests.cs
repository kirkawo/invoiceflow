using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace InvoiceFlow.ApiTests;

public class ApiIntegrationTests
{
    private HttpClient CreateClient() => new ApiWebApplicationFactory().CreateClient();

    private async Task<Guid> CreateDraftInvoiceAsync(HttpClient client)
    {
        var createResponse = await client.PostAsJsonAsync("/api/clients", new
        {
            name = "Alice",
            email = "alice@example.com"
        });
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var clientId = createBody.GetProperty("id").GetGuid();

        var issueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var dueDate = issueDate.AddDays(30);
        var invoiceResponse = await client.PostAsJsonAsync("/api/invoices", new
        {
            clientId,
            number = "INV-001",
            issueDateUtc = issueDate,
            dueDateUtc = dueDate,
            currency = "USD"
        });
        var invoiceBody = await invoiceResponse.Content.ReadFromJsonAsync<JsonElement>();
        return invoiceBody.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task PostClient_Returns201_WithId()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/clients", new
        {
            name = "Alice",
            email = "alice@example.com",
            companyName = "Acme Inc."
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("id", out var idProp));
        Assert.NotEqual(Guid.Empty, idProp.GetGuid());
    }

    [Fact]
    public async Task GetClients_ReturnsCreatedClients()
    {
        using var client = CreateClient();

        await client.PostAsJsonAsync("/api/clients", new { name = "Alice", email = "alice@example.com" });
        await client.PostAsJsonAsync("/api/clients", new { name = "Bob", email = "bob@example.com" });

        var response = await client.GetAsync("/api/clients");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var clients = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, clients.GetArrayLength());
    }

    [Fact]
    public async Task PostInvoice_Returns201_WhenClientExists()
    {
        using var client = CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/clients", new
        {
            name = "Alice",
            email = "alice@example.com"
        });
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var clientId = createBody.GetProperty("id").GetGuid();

        var issueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var dueDate = issueDate.AddDays(30);

        var response = await client.PostAsJsonAsync("/api/invoices", new
        {
            clientId,
            number = "INV-001",
            issueDateUtc = issueDate,
            dueDateUtc = dueDate,
            currency = "USD"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("id", out var idProp));
        Assert.NotEqual(Guid.Empty, idProp.GetGuid());
    }

    [Fact]
    public async Task GetInvoiceById_ReturnsInvoice()
    {
        using var client = CreateClient();

        var createClientResponse = await client.PostAsJsonAsync("/api/clients", new
        {
            name = "Alice",
            email = "alice@example.com"
        });
        var clientId = (await createClientResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var issueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var dueDate = issueDate.AddDays(30);
        var createInvoiceResponse = await client.PostAsJsonAsync("/api/invoices", new
        {
            clientId,
            number = "INV-001",
            issueDateUtc = issueDate,
            dueDateUtc = dueDate,
            currency = "USD"
        });
        var invoiceId = (await createInvoiceResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var response = await client.GetAsync($"/api/invoices/{invoiceId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var invoice = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(invoiceId, invoice.GetProperty("id").GetGuid());
        Assert.Equal("INV-001", invoice.GetProperty("number").GetString());
        Assert.Equal("USD", invoice.GetProperty("currency").GetString());
    }

    [Fact]
    public async Task GetClientInvoices_ReturnsInvoicesForClient()
    {
        using var client = CreateClient();

        var createClientResponse = await client.PostAsJsonAsync("/api/clients", new
        {
            name = "Alice",
            email = "alice@example.com"
        });
        var clientId = (await createClientResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var issueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        await client.PostAsJsonAsync("/api/invoices", new
        {
            clientId,
            number = "INV-001",
            issueDateUtc = issueDate,
            dueDateUtc = issueDate.AddDays(30),
            currency = "USD"
        });
        await client.PostAsJsonAsync("/api/invoices", new
        {
            clientId,
            number = "INV-002",
            issueDateUtc = issueDate,
            dueDateUtc = issueDate.AddDays(30),
            currency = "EUR"
        });

        var response = await client.GetAsync($"/api/clients/{clientId}/invoices");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var invoices = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, invoices.GetArrayLength());
    }

    [Fact]
    public async Task AddLineItem_ToDraftInvoice_Returns201()
    {
        using var client = CreateClient();
        var invoiceId = await CreateDraftInvoiceAsync(client);

        var response = await client.PostAsJsonAsync($"/api/invoices/{invoiceId}/line-items", new
        {
            description = "Consulting",
            quantity = 10,
            unitPrice = 100
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var getResponse = await client.GetAsync($"/api/invoices/{invoiceId}");
        var invoice = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1000, invoice.GetProperty("total").GetDecimal());
        Assert.Single(invoice.GetProperty("lineItems").EnumerateArray());
    }

    [Fact]
    public async Task AddLineItem_ToNonDraftInvoice_Returns400()
    {
        using var client = CreateClient();
        var invoiceId = await CreateDraftInvoiceAsync(client);
        await client.PostAsJsonAsync($"/api/invoices/{invoiceId}/line-items", new
        {
            description = "Service",
            quantity = 1,
            unitPrice = 500
        });
        await client.PostAsync($"/api/invoices/{invoiceId}/issue", null);

        var response = await client.PostAsJsonAsync($"/api/invoices/{invoiceId}/line-items", new
        {
            description = "Extra",
            quantity = 1,
            unitPrice = 100
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateLineItem_UpdatesInvoiceTotal()
    {
        using var client = CreateClient();
        var invoiceId = await CreateDraftInvoiceAsync(client);
        var addResponse = await client.PostAsJsonAsync($"/api/invoices/{invoiceId}/line-items", new
        {
            description = "Consulting",
            quantity = 10,
            unitPrice = 100
        });
        var addBody = await addResponse.Content.ReadFromJsonAsync<JsonElement>();
        var lineItemId = addBody.GetProperty("lineItemId").GetInt32();

        var response = await client.PutAsJsonAsync($"/api/invoices/{invoiceId}/line-items/{lineItemId}", new
        {
            description = "Premium Consulting",
            quantity = 5,
            unitPrice = 200
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var getResponse = await client.GetAsync($"/api/invoices/{invoiceId}");
        var invoice = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1000, invoice.GetProperty("total").GetDecimal());
        var items = invoice.GetProperty("lineItems").EnumerateArray().ToList();
        Assert.Single(items);
        Assert.Equal("Premium Consulting", items[0].GetProperty("description").GetString());
    }

    [Fact]
    public async Task RemoveLineItem_RecalculatesTotal()
    {
        using var client = CreateClient();
        var invoiceId = await CreateDraftInvoiceAsync(client);
        var addResponse = await client.PostAsJsonAsync($"/api/invoices/{invoiceId}/line-items", new
        {
            description = "Item",
            quantity = 2,
            unitPrice = 50
        });
        var addBody = await addResponse.Content.ReadFromJsonAsync<JsonElement>();
        var lineItemId = addBody.GetProperty("lineItemId").GetInt32();

        var response = await client.DeleteAsync($"/api/invoices/{invoiceId}/line-items/{lineItemId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var getResponse = await client.GetAsync($"/api/invoices/{invoiceId}");
        var invoice = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, invoice.GetProperty("total").GetDecimal());
        Assert.Empty(invoice.GetProperty("lineItems").EnumerateArray());
    }

    [Fact]
    public async Task GetAllInvoices_ReturnsAllInvoices()
    {
        using var client = CreateClient();

        var createClientResponse = await client.PostAsJsonAsync("/api/clients", new
        {
            name = "Alice",
            email = "alice@example.com"
        });
        var clientId = (await createClientResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var issueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        await client.PostAsJsonAsync("/api/invoices", new
        {
            clientId,
            number = "INV-001",
            issueDateUtc = issueDate,
            dueDateUtc = issueDate.AddDays(30),
            currency = "USD"
        });
        await client.PostAsJsonAsync("/api/invoices", new
        {
            clientId,
            number = "INV-002",
            issueDateUtc = issueDate,
            dueDateUtc = issueDate.AddDays(30),
            currency = "EUR"
        });

        var response = await client.GetAsync("/api/invoices");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var invoices = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, invoices.GetArrayLength());
    }

    [Fact]
    public async Task GetAllInvoices_FiltersByClient()
    {
        using var client = CreateClient();

        var createClient1 = await client.PostAsJsonAsync("/api/clients", new { name = "Alice", email = "alice@example.com" });
        var client1Id = (await createClient1.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var createClient2 = await client.PostAsJsonAsync("/api/clients", new { name = "Bob", email = "bob@example.com" });
        var client2Id = (await createClient2.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var issueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        await client.PostAsJsonAsync("/api/invoices", new { clientId = client1Id, number = "INV-001", issueDateUtc = issueDate, dueDateUtc = issueDate.AddDays(30), currency = "USD" });
        await client.PostAsJsonAsync("/api/invoices", new { clientId = client2Id, number = "INV-002", issueDateUtc = issueDate, dueDateUtc = issueDate.AddDays(30), currency = "USD" });

        var response = await client.GetAsync($"/api/invoices?clientId={client1Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var invoices = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, invoices.GetArrayLength());
        Assert.Equal("INV-001", invoices[0].GetProperty("number").GetString());
    }

    [Fact]
    public async Task GetAllInvoices_FiltersByStatus()
    {
        using var client = CreateClient();

        var createClient = await client.PostAsJsonAsync("/api/clients", new { name = "Alice", email = "alice@example.com" });
        var clientId = (await createClient.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var issueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var inv1Resp = await client.PostAsJsonAsync("/api/invoices", new { clientId, number = "INV-001", issueDateUtc = issueDate, dueDateUtc = issueDate.AddDays(30), currency = "USD" });
        var inv1Id = (await inv1Resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await client.PostAsJsonAsync("/api/invoices", new { clientId, number = "INV-002", issueDateUtc = issueDate, dueDateUtc = issueDate.AddDays(30), currency = "USD" });
        await client.PostAsJsonAsync($"/api/invoices/{inv1Id}/line-items", new { description = "Item", quantity = 1, unitPrice = 100 });
        await client.PostAsync($"/api/invoices/{inv1Id}/issue", null);

        var response = await client.GetAsync($"/api/invoices?status=Issued");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var invoices = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, invoices.GetArrayLength());
        Assert.Equal("INV-001", invoices[0].GetProperty("number").GetString());
    }

    [Fact]
    public async Task PostManualReminder_OverdueInvoice_Returns201()
    {
        using var client = CreateClient();
        var (clientId, invoiceId) = await CreateOverdueInvoiceAsync(client);

        var response = await client.PostAsync($"/api/invoices/{invoiceId}/reminders/manual", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var remindersResponse = await client.GetAsync($"/api/invoices/{invoiceId}/reminders");
        var reminders = await remindersResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Single(reminders.EnumerateArray());
        Assert.Equal("Sent", reminders[0].GetProperty("status").GetString());
        Assert.Equal("client@example.com", reminders[0].GetProperty("recipientEmail").GetString());
    }

    [Fact]
    public async Task PostManualReminder_DraftInvoice_Returns400()
    {
        using var client = CreateClient();
        var invoiceId = await CreateDraftInvoiceAsync(client);

        var response = await client.PostAsync($"/api/invoices/{invoiceId}/reminders/manual", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostManualReminder_NonExistentInvoice_Returns400()
    {
        using var client = CreateClient();

        var response = await client.PostAsync($"/api/invoices/{Guid.NewGuid()}/reminders/manual", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetReminderHistory_ReturnsOnlyRemindersForInvoice()
    {
        using var client = CreateClient();
        var (_, invoice1Id) = await CreateOverdueInvoiceAsync(client);
        var (_, invoice2Id) = await CreateOverdueInvoiceAsync(client);

        await client.PostAsync($"/api/invoices/{invoice1Id}/reminders/manual", null);
        await client.PostAsync($"/api/invoices/{invoice2Id}/reminders/manual", null);

        var response = await client.GetAsync($"/api/invoices/{invoice1Id}/reminders");
        var reminders = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Single(reminders.EnumerateArray());
    }

    private async Task<(Guid clientId, Guid invoiceId)> CreateOverdueInvoiceAsync(HttpClient client)
    {
        var createResponse = await client.PostAsJsonAsync("/api/clients", new
        {
            name = "Alice",
            email = "client@example.com"
        });
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var clientId = createBody.GetProperty("id").GetGuid();

        var issueDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var dueDate = new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc);
        var invoiceResponse = await client.PostAsJsonAsync("/api/invoices", new
        {
            clientId,
            number = "INV-OD-001",
            issueDateUtc = issueDate,
            dueDateUtc = dueDate,
            currency = "USD"
        });
        var invoiceBody = await invoiceResponse.Content.ReadFromJsonAsync<JsonElement>();
        var invoiceId = invoiceBody.GetProperty("id").GetGuid();

        await client.PostAsJsonAsync($"/api/invoices/{invoiceId}/line-items", new
        {
            description = "Service",
            quantity = 1,
            unitPrice = 100
        });
        await client.PostAsync($"/api/invoices/{invoiceId}/issue", null);
        await client.PostAsync($"/api/invoices/{invoiceId}/mark-overdue", null);

        return (clientId, invoiceId);
    }

    [Fact]
    public async Task PostInvoice_Returns404_WhenClientDoesNotExist()
    {
        using var client = CreateClient();

        var issueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var response = await client.PostAsJsonAsync("/api/invoices", new
        {
            clientId = Guid.NewGuid(),
            number = "INV-001",
            issueDateUtc = issueDate,
            dueDateUtc = issueDate.AddDays(30),
            currency = "USD"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
