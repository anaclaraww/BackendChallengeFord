using BackendChallengeFord.DTOs;
using System.Net;
using System.Text.Json;

namespace BackendChallengeFord.Middlewares;

/// <summary>
/// Middleware global de tratamento de erros.
/// Garante que nenhuma exceção não tratada exponha stack trace,
/// estrutura interna ou tecnologia usada ao cliente.
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ERRO INTERNO] Path={Path} Method={Method}",
                context.Request.Path, context.Request.Method);

            await TratarExcecao(context, ex);
        }
    }

    private async Task TratarExcecao(HttpContext context, Exception ex)
    {
        var statusCode = ex switch
        {
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            KeyNotFoundException        => HttpStatusCode.NotFound,
            ArgumentException           => HttpStatusCode.BadRequest,
            _                           => HttpStatusCode.InternalServerError
        };

        var response = new ErroResponseDTO
        {
            Status = (int)statusCode,

            
            Mensagem = statusCode == HttpStatusCode.InternalServerError
                ? "Ocorreu um erro interno. Tente novamente mais tarde."
                : ex.Message,

            Detalhe = _env.IsDevelopment() ? ex.StackTrace : null
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        context.Response.Headers.Remove("Server");

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
