using InvoiceFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceFlow.Infrastructure.Persistence.Configurations;

public class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> builder)
    {
        builder.ToTable("Reminders");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Type).HasConversion<string>().HasMaxLength(50);
        builder.Property(r => r.Channel).HasConversion<string>().HasMaxLength(50);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(50);

        builder.Property(r => r.RecipientEmail).HasMaxLength(256).IsRequired();
        builder.Property(r => r.Subject).HasMaxLength(256).IsRequired();
        builder.Property(r => r.FailureReason).HasMaxLength(1024);

        builder.HasIndex(r => r.WorkspaceId);
        builder.HasIndex(r => r.InvoiceId);
        builder.HasIndex(r => new { r.WorkspaceId, r.InvoiceId });
    }
}
