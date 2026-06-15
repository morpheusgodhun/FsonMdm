using FsonMdm.Application.Interfaces.Repositories;
using FsonMdm.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FsonMdm.Infrastructure.Persistence.Repositories;

public class AdminUserRepository : IAdminUserRepository
{
    private readonly AppDbContext _db;
    public AdminUserRepository(AppDbContext db) => _db = db;

    public Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        _db.AdminUsers.FirstOrDefaultAsync(u => u.Username == username, ct);
}
