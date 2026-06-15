using FsonMdm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FsonMdm.Infrastructure.Persistence.Configurations;

public class CommandConfiguration : IEntityTypeConfiguration<Command>
{
    public void Configure(EntityTypeBuilder<Command> builder)
    {
        builder.ToTable("Commands");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Type).HasConversion<int>();
        builder.Property(c => c.Status).HasConversion<int>();
        builder.Property(c => c.Payload).HasMaxLength(4000);

        // Pending-command polling filters by device + status.
        builder.HasIndex(c => new { c.DeviceId, c.Status });

        builder.HasOne(c => c.Device)
               .WithMany(d => d.Commands)
               .HasForeignKey(c => c.DeviceId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
