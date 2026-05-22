using System.Collections.Concurrent;

namespace BackendChallengeFord.Middlewares;

/// <summary>
/// Rate limiting por IP — evita abuso, scraping e ataques DoS.
/// Janela deslizante de 1 minuto com limite configurável de requisições.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    private static readonly ConcurrentDictionary<string, List<DateTime>> _requests = new();

    private const int MaxRequests = 60;          
    private const int WindowSeconds = 60;        
    private const int AuthMaxRequests = 10;       
    private const int AuthWindowSeconds = 60;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var path = context.Request.Path.Value?.ToLower() ?? "";

        var isAuthEndpoint = path.Contains("/auth/");
        var maxReqs = isAuthEndpoint ? AuthMaxRequests : MaxRequests;
        var windowSecs = isAuthEndpoint ? AuthWindowSeconds : WindowSeconds;

        var key = $"{ip}:{(isAuthEndpoint ? "auth" : "general")}";
        var now = DateTime.UtcNow;
        var windowStart = now.AddSeconds(-windowSecs);

        var timestamps = _requests.GetOrAdd(key, _ => new List<DateTime>());

        lock (timestamps)
        {
            timestamps.RemoveAll(t => t < windowStart);

            if (timestamps.Count >= maxReqs)
            {
                _logger.LogWarning("Rate limit atingido para IP {IP} no path {Path}", ip, path);

                context.Response.StatusCode = 429;
                context.Response.Headers["Retry-After"] = windowSecs.ToString();
                context.Response.ContentType = "application/json";
                context.Response.WriteAsync("{\"status\":429,\"mensagem\":\"Muitas requisições. Tente novamente em instantes.\"}").Wait();
                return;
            }

            timestamps.Add(now);
        }

        await _next(context);
    }
}
