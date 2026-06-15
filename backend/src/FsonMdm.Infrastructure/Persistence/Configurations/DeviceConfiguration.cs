using FsonMdm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FsonMdm.Infrastructure.Persistence.Configurations;

public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("Devices");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.DeviceId).IsRequired().HasMaxLength(200);
        builder.Property(d => d.Model).HasMaxLength(200);
        builder.Property(d => d.IPAddress).HasMaxLength(64);
        builder.Property(d => d.Status).HasConversion<int>();

        // A hardware DeviceId is unique within its tenant.
        builder.HasIndex(d => new { d.TenantId, d.DeviceId }).IsUnique();

        builder.HasOne(d => d.Tenant)
               .WithMany(t => t.Devices)
               .HasForeignKey(d => d.TenantId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
