using InvoiceFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceFlow.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();
        builder.Property(i => i.WorkspaceId).IsRequired();
        builder.Property(i => i.ClientId).IsRequired();
        builder.Property(i => i.Number).HasMaxLength(50).IsRequired();
        builder.Property(i => i.IssueDateUtc).IsRequired();
        builder.Property(i => i.DueDateUtc).IsRequired();
        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(i => i.Currency).HasMaxLength(3).IsRequired();
        builder.Property(i => i.Notes).HasMaxLength(2000);
        builder.Property(i => i.IssuedAtUtc);
        builder.Property(i => i.PaidAtUtc);
        builder.Property(i => i.CancelledAtUtc);
        builder.Ignore(i => i.Subtotal);
        builder.Ignore(i => i.Total);

        builder.OwnsMany(i => i.LineItems, li =>
        {
            li.WithOwner().HasForeignKey("InvoiceId");
            li.ToTable("InvoiceLineItems");
            li.Property(l => l.Id).ValueGeneratedOnAdd();
            li.Property(l => l.Description).HasMaxLength(500).IsRequired();
            li.Property(l => l.Quantity).HasPrecision(18, 2);
            li.Property(l => l.UnitPrice).HasPrecision(18, 2);
            li.Ignore(l => l.Amount);
        });

        builder.Navigation(i => i.LineItems).HasField("_lineItems");

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(i => i.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.WorkspaceId);
        builder.HasIndex(i => new { i.WorkspaceId, i.ClientId });
        builder.HasIndex(i => i.ClientId).HasDatabaseName("IX_Invoices_ClientId");
        builder.HasIndex(i => i.Number).HasDatabaseName("IX_Invoices_Number");
    }
}
