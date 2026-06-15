using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FsonMdm.Infrastructure.Persistence;

/// <summary>
/// Enables `dotnet ef migrations add ...` to be run from the Infrastructure
/// project without spinning up the API host.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Data Source=BSTNB290;Initial Catalog=MDM;Integrated Security=True;TrustServerCertificate=True;Encrypt=False")
            .Options;
        return new AppDbContext(options);
    }
}
