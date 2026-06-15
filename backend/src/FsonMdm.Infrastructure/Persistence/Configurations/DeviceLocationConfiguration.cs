using FsonMdm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FsonMdm.Infrastructure.Persistence.Configurations;

public class DeviceLocationConfiguration : IEntityTypeConfiguration<DeviceLocation>
{
    public void Configure(EntityTypeBuilder<DeviceLocation> builder)
    {
        builder.ToTable("DeviceLocations");
        builder.HasKey(l => l.Id);

        builder.HasIndex(l => new { l.DeviceId, l.CapturedAt });

        builder.HasOne(l => l.Device)
               .WithMany(d => d.Locations)
               .HasForeignKey(l => l.DeviceId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
