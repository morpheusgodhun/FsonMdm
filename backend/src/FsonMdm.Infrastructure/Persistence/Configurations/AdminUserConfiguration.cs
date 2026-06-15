using FsonMdm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FsonMdm.Infrastructure.Persistence.Configurations;

public class AdminUserConfiguration : IEntityTypeConfiguration<AdminUser>
{
    public void Configure(EntityTypeBuilder<AdminUser> builder)
    {
        builder.ToTable("AdminUsers");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username).IsRequired().HasMaxLength(100);
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(512);
        builder.HasIndex(u => u.Username).IsUnique();

        builder.HasOne(u => u.Tenant)
               .WithMany(t => t.AdminUsers)
               .HasForeignKey(u => u.TenantId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
