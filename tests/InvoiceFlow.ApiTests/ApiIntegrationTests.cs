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
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("lineItemId", out var idProp));
        Assert.NotEqual(0, idProp.GetInt32());

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
        await client.PostAsJsonAsync($"/api/invoices/{invoiceId}/line-items", new
        {
            description = "Item A",
            quantity = 2,
            unitPrice = 50
        });
        var addResponse = await client.PostAsJsonAsync($"/api/invoices/{invoiceId}/line-items", new
        {
            description = "Item B",
            quantity = 3,
            unitPrice = 30
        });
        var addBody = await addResponse.Content.ReadFromJsonAsync<JsonElement>();
        var lineItemId = addBody.GetProperty("lineItemId").GetInt32();

        var response = await client.DeleteAsync($"/api/invoices/{invoiceId}/line-items/{lineItemId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var getResponse = await client.GetAsync($"/api/invoices/{invoiceId}");
        var invoice = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(100, invoice.GetProperty("total").GetDecimal());
        Assert.Single(invoice.GetProperty("lineItems").EnumerateArray());
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
