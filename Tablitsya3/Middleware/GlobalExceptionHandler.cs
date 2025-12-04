using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using System.Text.Json;

namespace Tablitsya3.Middleware;

/// <summary>
/// Глобальна обробка помилок для Production
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

  public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
      _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
   HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "❌ Unhandled exception occurred: {Message}", exception.Message);

     var problemDetails = new
        {
         Status = (int)HttpStatusCode.InternalServerError,
         Title = "Виникла помилка",
     Detail = httpContext.Request.Host.Host.Contains("localhost") 
      ? exception.Message 
      : "Будь ласка, спробуйте пізніше або зверніться до адміністратора.",
    Instance = httpContext.Request.Path,
   Timestamp = DateTime.UtcNow
        };

   httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsync(
   JsonSerializer.Serialize(problemDetails),
       cancellationToken);

        return true;
    }
}
