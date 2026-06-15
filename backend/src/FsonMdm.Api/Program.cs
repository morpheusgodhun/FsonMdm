using FsonMdm.Api.Middleware;
using FsonMdm.Api.Services;
using FsonMdm.Application;
using FsonMdm.Infrastructure;
using FsonMdm.Infrastructure.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// API controllers + server-rendered dashboard (MVC + Razor views) in one host.
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "FSON MDM API", Version = "v1" });

    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Bearer {token}",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    options.AddSecurityDefinition("Bearer", scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });
});

builder.Services.AddCors(options =>
    options.AddPolicy("Default", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// File storage for uploaded APKs and screenshots.
builder.Services.AddScoped<FileStorage>();

// Cookie scheme for the dashboard. JWT remains the default scheme for the API;
// dashboard controllers opt into this scheme explicitly. The shared ITenantContext
// resolves tenant/role from whichever principal authenticated.
builder.Services.AddAuthentication()
    .AddCookie(AuthSchemes.Dashboard, options =>
    {
        options.Cookie.Name = "fson_mdm_dashboard";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/dashboard/login";
        options.LogoutPath = "/dashboard/logout";
        options.AccessDeniedPath = "/dashboard/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Allow large APK uploads through the multipart form limit.
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 512_000_000; // 512 MB
});

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("Default");
app.UseAuthentication();
app.UseAuthorization();

// API attribute-routed controllers + dashboard MVC routes.
app.MapControllers();
app.MapControllerRoute(
    name: "dashboard",
    pattern: "dashboard/{action=Index}/{id?}",
    defaults: new { controller = "Dashboard" });

// Root → dashboard.
app.MapGet("/", () => Results.Redirect("/dashboard"));

app.Run();
