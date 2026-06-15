using FsonMdm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FsonMdm.Infrastructure.Persistence.Configurations;

public class PolicyConfiguration : IEntityTypeConfiguration<Policy>
{
    public void Configure(EntityTypeBuilder<Policy> builder)
    {
        builder.ToTable("Policies");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.ConfigJson).IsRequired();
        builder.HasIndex(p => p.TenantId);

        builder.HasOne(p => p.Tenant)
               .WithMany(t => t.Policies)
               .HasForeignKey(p => p.TenantId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
