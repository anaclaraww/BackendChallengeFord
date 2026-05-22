using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace BackendChallengeFord.Middlewares;

/// <summary>
/// Verifica a assinatura HMAC-SHA256 do payload para garantir integridade dos dados.
/// A chave vem do appsettings — NUNCA hardcoded no código.
/// Apenas endpoints que modificam dados (POST/PUT/PATCH) exigem assinatura.
/// </summary>
public class PayloadSignatureMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PayloadSignatureMiddleware> _logger;

    private static readonly string[] SignedMethods = { "POST", "PUT", "PATCH" };

    private static readonly string[] ExcludedPaths = { "/api/auth/login", "/api/auth/register" };

    public PayloadSignatureMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<PayloadSignatureMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var method = context.Request.Method.ToUpper();
        var path = context.Request.Path.Value?.ToLower() ?? "";

        var requiresSignature =
            SignedMethods.Contains(method) &&
            !ExcludedPaths.Any(p => path.StartsWith(p));

        if (!requiresSignature)
        {
            await _next(context);
            return;
        }

        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        var signature = context.Request.Headers["X-Signature"].FirstOrDefault();

        if (string.IsNullOrEmpty(signature))
        {
            _logger.LogWarning("[SEGURANÇA] Assinatura ausente — IP={IP} Path={Path}",
                context.Connection.RemoteIpAddress, path);
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"status\":401,\"mensagem\":\"Assinatura do payload ausente.\"}");
            return;
        }

        var computedSignature = ComputeHmac(body);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(signature),
                Encoding.UTF8.GetBytes(computedSignature)))
        {
            _logger.LogWarning("[ALERTA SEGURANÇA] Assinatura inválida — IP={IP} Path={Path}",
                context.Connection.RemoteIpAddress, path);
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"status\":401,\"mensagem\":\"Assinatura do payload inválida.\"}");
            return;
        }

        await _next(context);
    }

    private string ComputeHmac(string data)
    {
        
        var secret = _configuration["Security:PayloadSignatureKey"]
            ?? throw new InvalidOperationException("PayloadSignatureKey não configurada.");

        var key = Encoding.UTF8.GetBytes(secret);
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLower();
    }
}
