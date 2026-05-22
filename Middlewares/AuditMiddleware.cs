using System.Security.Claims;

namespace BackendChallengeFord.Middlewares;

/// <summary>
/// Trilha de auditoria registra ações críticas com rastreabilidade.
/// Loga IP, usuário, método, path e status de cada requisição sensível.
/// Dados sensíveis (senha, token) não são logados.
/// </summary>
public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;

   
    private static readonly string[] AuditedPaths =
    {
        "/api/auth/",
        "/api/veiculo",
        "/api/manutencao",
        "/api/cliente"
    };

    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        await _next(context);

        var path = context.Request.Path.Value?.ToLower() ?? "";
        var shouldAudit = AuditedPaths.Any(p => path.StartsWith(p));

        if (!shouldAudit) return;

        var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anônimo";
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var method = context.Request.Method;
        var statusCode = context.Response.StatusCode;

        _logger.LogInformation(
            "[AUDITORIA] UserId={UserId} IP={IP} Method={Method} Path={Path} Status={StatusCode} Timestamp={Timestamp}",
            userId, ip, method, path, statusCode, DateTime.UtcNow
        );


        if (statusCode == 401 || statusCode == 403)
        {
            _logger.LogWarning(
                "[ALERTA SEGURANÇA] Acesso negado — UserId={UserId} IP={IP} Path={Path} Status={StatusCode}",
                userId, ip, path, statusCode
            );
        }
    }
}
