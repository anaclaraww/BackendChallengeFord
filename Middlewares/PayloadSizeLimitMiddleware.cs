namespace BackendChallengeFord.Middlewares;

/// <summary>
/// Limita o tamanho máximo do payload das requisições.
/// Previne ataques de payload flooding e buffer overflow.
/// </summary>
public class PayloadSizeLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PayloadSizeLimitMiddleware> _logger;

    private const long MaxBodySize = 1 * 1024 * 1024;

    public PayloadSizeLimitMiddleware(RequestDelegate next, ILogger<PayloadSizeLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        if (context.Request.ContentLength.HasValue &&
            context.Request.ContentLength > MaxBodySize)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            _logger.LogWarning(
                "[ALERTA SEGURANÇA] Payload muito grande — IP={IP} Size={Size} Path={Path}",
                ip, context.Request.ContentLength, context.Request.Path);

            context.Response.StatusCode = 413;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"status\":413,\"mensagem\":\"Payload muito grande.\"}");
            return;
        }

        context.Request.EnableBuffering();
        context.Features.Set<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>(
            new MaxRequestBodySizeFeature(MaxBodySize));

        await _next(context);
    }
}

public class MaxRequestBodySizeFeature : Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature
{
    public MaxRequestBodySizeFeature(long maxSize) => MaxRequestBodySize = maxSize;
    public bool IsReadOnly => false;
    public long? MaxRequestBodySize { get; set; }
}
