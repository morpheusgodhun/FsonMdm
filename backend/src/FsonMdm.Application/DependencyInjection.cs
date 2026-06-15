using FsonMdm.Application.Interfaces.Services;
using FsonMdm.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FsonMdm.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<IPolicyService, PolicyService>();
        services.AddScoped<ICommandService, CommandService>();
        services.AddScoped<IAppCatalogService, AppCatalogService>();
        return services;
    }
}
