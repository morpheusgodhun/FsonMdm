using FsonMdm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FsonMdm.Infrastructure.Persistence.Configurations;

public class DeviceAppConfiguration : IEntityTypeConfiguration<DeviceApp>
{
    public void Configure(EntityTypeBuilder<DeviceApp> builder)
    {
        builder.ToTable("DeviceApps");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.PackageName).IsRequired().HasMaxLength(256);
        builder.Property(a => a.AppLabel).HasMaxLength(256);

        // One row per app per device.
        builder.HasIndex(a => new { a.DeviceId, a.PackageName }).IsUnique();

        builder.HasOne(a => a.Device)
               .WithMany(d => d.Apps)
               .HasForeignKey(a => a.DeviceId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
