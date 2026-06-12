using InvoiceFlow.Domain;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Infrastructure.Persistence;

public class InvoiceFlowDbContext : DbContext
{
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Invoice> Invoices => Set<Invoice>();

    public InvoiceFlowDbContext(DbContextOptions<InvoiceFlowDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoiceFlowDbContext).Assembly);
    }
}
