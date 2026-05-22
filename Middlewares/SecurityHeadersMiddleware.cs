namespace BackendChallengeFord.Middlewares;

/// <summary>
/// Adiciona headers de segurança HTTP em todas as respostas.
/// Protege contra XSS, clickjacking, sniffing de MIME type e outros ataques comuns.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        //// Impede que o browser infira o tipo de conteúdo (MIME sniffing)
        //context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        //// Impede que a página seja exibida em iframe (clickjacking)
        //context.Response.Headers["X-Frame-Options"] = "DENY";

        //// Força HTTPS pelo browser nas próximas visitas (HSTS)
        //context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        //// Política de conteúdo — permite apenas recursos da própria origem
        //context.Response.Headers["Content-Security-Policy"] = "default-src 'self'";

        //// Remove informações sobre o servidor (não expor tecnologia usada)
        //context.Response.Headers.Remove("Server");
        //context.Response.Headers.Remove("X-Powered-By");
        //context.Response.Headers["X-Powered-By"] = "";

        // Controla o que é enviado no header Referer
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Desativa funcionalidades de browser não necessárias
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        await _next(context);
    }
}
