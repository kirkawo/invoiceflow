using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace InvoiceFlow.ApiTests;

public class ApiIntegrationTests
{
    private HttpClient CreateClient() => new ApiWebApplicationFactory().CreateClient();

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
