using FsonMdm.Domain.Entities;

namespace FsonMdm.Application.Interfaces.Repositories;

public interface IAdminUserRepository
{
    Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken ct = default);
}
