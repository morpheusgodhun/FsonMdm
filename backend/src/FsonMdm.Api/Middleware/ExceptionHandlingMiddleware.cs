using System.Net;
using System.Text.Json;
using FsonMdm.Application.Common.Exceptions;

namespace FsonMdm.Api.Middleware;

/// <summary>
/// Translates application exceptions into consistent JSON problem responses,
/// keeping controllers free of try/catch noise.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var status = ex switch
            {
                ValidationException => HttpStatusCode.BadRequest,
                NotFoundException => HttpStatusCode.NotFound,
                AuthException => HttpStatusCode.Unauthorized,
                _ => HttpStatusCode.InternalServerError
            };

            if (status == HttpStatusCode.InternalServerError)
                _logger.LogError(ex, "Beklenmeyen hata");

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)status;

            var payload = JsonSerializer.Serialize(new
            {
                error = status == HttpStatusCode.InternalServerError ? "Sunucu hatası." : ex.Message,
                status = (int)status
            });

            await context.Response.WriteAsync(payload);
        }
    }
}
