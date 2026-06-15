using FsonMdm.Application.Interfaces.Repositories;
using FsonMdm.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FsonMdm.Infrastructure.Persistence;

public class AppDbContext : DbContext, IUnitOfWork
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<Command> Commands => Set<Command>();
    public DbSet<DeviceLocation> DeviceLocations => Set<DeviceLocation>();
    public DbSet<DeviceApp> DeviceApps => Set<DeviceApp>();
    public DbSet<ManagedApp> ManagedApps => Set<ManagedApp>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
