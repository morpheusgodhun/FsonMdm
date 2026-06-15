using FsonMdm.Application.Common.Security;
using FsonMdm.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FsonMdm.Infrastructure.Persistence;

/// <summary>
/// Creates a demo tenant + admin + default policy on first run so the system is
/// usable end-to-end without a manual bootstrap step. Idempotent.
/// </summary>
public static class DbSeeder
{
    public const string DemoEnrollmentKey = "FSON-DEMO-ENROLLMENT-KEY";
    public const string DemoAdminUsername = "admin";
    public const string DemoAdminPassword = "Admin123!";

    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Tenants.AnyAsync(ct))
            return;

        var tenant = new Tenant
        {
            Name = "FSON Demo Tenant",
            EnrollmentKey = DemoEnrollmentKey
        };

        var admin = new AdminUser
        {
            TenantId = tenant.Id,
            Username = DemoAdminUsername,
            PasswordHash = PasswordHasher.Hash(DemoAdminPassword)
        };

        var policy = new Policy
        {
            TenantId = tenant.Id,
            Name = "Varsayılan Politika",
            Version = 1,
            ConfigJson = """
            {"kioskMode":false,"blockCamera":false,"blockSettings":false,"blockPlayStore":false,"allowedApps":[]}
            """,
            UpdatedAt = DateTime.UtcNow
        };

        await db.Tenants.AddAsync(tenant, ct);
        await db.AdminUsers.AddAsync(admin, ct);
        await db.Policies.AddAsync(policy, ct);
        await db.SaveChangesAsync(ct);
    }
}
