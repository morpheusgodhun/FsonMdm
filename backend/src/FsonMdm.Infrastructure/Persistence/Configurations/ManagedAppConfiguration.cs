using FsonMdm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FsonMdm.Infrastructure.Persistence.Configurations;

public class ManagedAppConfiguration : IEntityTypeConfiguration<ManagedApp>
{
    public void Configure(EntityTypeBuilder<ManagedApp> builder)
    {
        builder.ToTable("ManagedApps");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.PackageName).IsRequired().HasMaxLength(256);
        builder.Property(a => a.AppLabel).HasMaxLength(256);
        builder.Property(a => a.VersionName).HasMaxLength(64);
        builder.Property(a => a.FileName).IsRequired().HasMaxLength(256);
        builder.Property(a => a.StoredFileName).IsRequired().HasMaxLength(256);

        builder.HasIndex(a => a.TenantId);
    }
}
