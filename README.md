# BackendChallengeFord

## Descrição do Projeto

O **Ford Connect** é uma plataforma web desenvolvida em **ASP.NET Core 8 com MySQL**, destinada a proprietários de veículos Ford. A plataforma permite que clientes visualizem métricas de seus veículos, registrem manutenções realizadas e acumulem pontos em um programa de fidelidade integrado. O backend expõe uma **API REST** consumida por um frontend React, com autenticação baseada em JWT e arquitetura em camadas (Controllers → Services → Repositories).

Este documento apresenta as medidas de segurança implementadas no backend, organizadas pelos critérios de avaliação do Sprint de Cybersecurity.

---

## 1. Segurança de Entrada e Validação de Dados

### Validação de Entradas do Usuário

Todos os DTOs (Data Transfer Objects) utilizam **Data Annotations** do .NET para validação declarativa antes de qualquer processamento. A resposta de erro é padronizada via `ConfigureApiBehaviorOptions`, retornando sempre o mesmo formato JSON sem revelar detalhes internos.

```csharp
 RegisterDTO.cs
[Required(ErrorMessage = "Nome é obrigatório.")]
[MinLength(3), MaxLength(100)]
[RegularExpression(@"^[\p{L}\s]+$", ErrorMessage = "Nome deve conter apenas letras.")]
public string Nome { get; set; }
....
[MinLength(8), MaxLength(100)]
[RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).+$",
    ErrorMessage = "Senha deve ter maiúscula, minúscula, número e caractere especial.")]
public string Senha { get; set; }
```

### Sanitização contra SQL Injection, XSS e Command Injection

A aplicação utiliza **Entity Framework Core** como ORM, que por padrão parametriza todas as queries SQL, eliminando o vetor de SQL Injection. Não há queries SQL escritas manualmente. Adicionalmente, o `AuthService` realiza sanitização e normalização explícita das entradas antes de qualquer operação:

```csharp
dto.Email = dto.Email.Trim().ToLowerInvariant();
dto.Nome  = dto.Nome.Trim();
dto.CPF   = Regex.Replace(dto.CPF.Trim(), @"[^\d]", "");

if (!Regex.IsMatch(dto.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
    return null;
...
[RegularExpression(@"^[A-HJ-NPR-Z0-9]{17}$", ErrorMessage = "Chassi inválido.")]
public string Chassi { get; set; }
```

### Limitação de Tamanho e Formato de Entrada

O tamanho máximo do corpo das requisições é limitado em **1 MB** em dois níveis: via configuração do Kestrel e via middleware dedicado (`PayloadSizeLimitMiddleware`), que bloqueia a requisição antes de qualquer processamento:

```csharp
 Program.cs — limite no servidor
builder.WebHost.ConfigureKestrel(options => {
    options.Limits.MaxRequestBodySize = 1 * 1024 * 1024;
});

 PayloadSizeLimitMiddleware.cs — middleware de rejeição
if (context.Request.ContentLength > MaxBodySize) {
    context.Response.StatusCode = 413;
    await context.Response.WriteAsync("{\"status\":413,\"mensagem\":\"Payload muito grande.\"}");
    return;
}
```

Todos os campos de string possuem `MaxLength` definido no banco e no código.

### Tratamento Seguro de Erros

O `ErrorHandlingMiddleware` intercepta todas as exceções não tratadas e retorna mensagens genéricas em produção, sem expor stack trace e informações sensíveis. O header `Server` é removido de todas as respostas:

```csharp
 ErrorHandlingMiddleware.cs
Mensagem = statusCode == HttpStatusCode.InternalServerError
    ? "Ocorreu um erro interno. Tente novamente mais tarde."
    : ex.Message,

Detalhe = _env.IsDevelopment() ? ex.StackTrace : null

context.Response.Headers.Remove("Server");
```

---

## 2. Autenticação e Autorização

### Implementação JWT com Tokens Seguros

A autenticação utiliza **JWT (JSON Web Token)** com algoritmo de assinatura **HMAC-SHA256**. Cada token possui tempo de expiração de 8 horas, claim `jti` único para identificação individual do token e `notBefore` para impedir uso antecipado. O `ClockSkew` é definido como zero, tornando a expiração precisa sem margem de tolerância:

```csharp
 Program.cs
options.TokenValidationParameters = new TokenValidationParameters {
    ValidateIssuer           = true,
    ValidateAudience         = true,
    ValidateLifetime         = true,
    ValidateIssuerSigningKey = true,
    ClockSkew                = TimeSpan.Zero,
    IssuerSigningKey         = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!))
};

 AuthService.cs — geração do token
var claims = new[] {
    new Claim(JwtRegisteredClaimNames.Sub, cliente.Id.ToString()),
    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    new Claim("role", "cliente"),
    new Claim(JwtRegisteredClaimNames.Email, cliente.Email)
};
```

Tentativas com tokens inválidos são registradas com IP e path no log de segurança via `JwtBearerEvents`.

### RBAC

A aplicação implementa RBAC através de claims roles embutidas no token JWT. Políticas de acesso são definidas no startup e aplicadas por controller ou endpoint:

```csharp
 Program.cs
builder.Services.AddAuthorization(options => {
    options.AddPolicy("ClientePolicy", policy =>
        policy.RequireClaim("role", "cliente"));
    options.AddPolicy("AdminPolicy", policy =>
        policy.RequireClaim("role", "admin"));
});
```

Todos os controllers após login exigem [Authorize] (Token JWT), garantindo que apenas usuários autenticados acessem os endpoints. O ID do cliente é extraído diretamente do token JWT, impedindo que um cliente acesse dados de outro:

```csharp
 VeiculoController.cs
[ApiController]
[Authorize]
public class VeiculoController : ControllerBase {
    private int GetClienteId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
```

### Proteção contra Timing Attack no Login

A verificação de senha utiliza `BCrypt.Net.BCrypt.Verify()` mesmo quando o usuário não existe, impedindo que um atacante de saber se o email existe no Nexus pelo tempo de retorno da resposta:

```csharp
 AuthService.cs
var senhaCorreta = cliente != null &&
    BCrypt.Net.BCrypt.Verify(dto.Senha, cliente.SenhaHash);

if (!senhaCorreta) {
    _logger.LogWarning("[WAR - SEC] Email ou senha não válidos");
    return null;
}
```

---

## 3. Proteção de APIs e Serviços

### HTTPS/TLS

A aplicação é configurada para operar exclusivamente via HTTPS/TLS. Em desenvolvimento, rodar pelo IIS Express provisiona automaticamente um certificado TLS. O middleware de security headers inclui **HSTS (HTTP Strict Transport Security)** com validade de um ano, forçando o browser a usar HTTPS em todas as conexões futuras:

```csharp
 SecurityHeadersMiddleware.cs
context.Response.Headers["Strict-Transport-Security"] =
    "max-age=31536000; includeSubDomains";
```

### Rate Limiting 

O `RateLimitingMiddleware` implementa janela deslizante por IP com limites diferenciados por contexto. Endpoints de autenticação possuem limite mais restrito para prevenir ataques de força bruta:

```csharp
 RateLimitingMiddleware.cs
private const int MaxRequests     = 60;   
private const int AuthMaxRequests = 10;  

if (timestamps.Count >= maxReqs) {
    context.Response.StatusCode = 429;
    context.Response.Headers["Retry-After"] = windowSecs.ToString();
    return;
}
```

### CORS Configurado por Domínio Autorizado

O CORS permite apenas origens explicitamente listadas, com métodos e headers específicos. `AllowAnyOrigin` não é utilizado em nenhuma configuração:

```csharp
 Program.cs
policy.WithOrigins("https://localhost:5173", "http://localhost:5173")
      .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
      .WithHeaders("Authorization", "Content-Type", "X-Signature")
      .AllowCredentials();
```

### Assinatura e Verificação de Integridade de Payloads

O `PayloadSignatureMiddleware` verifica um hash **HMAC-SHA256** do corpo da requisição, enviado pelo cliente no header `X-Signature`. A comparação utiliza `CryptographicOperations.FixedTimeEquals` para evitar timing attacks. A chave vem exclusivamente do `appsettings`, nunca hardcoded:

```csharp
 PayloadSignatureMiddleware.cs
var computedSignature = ComputeHmac(body);
if (!CryptographicOperations.FixedTimeEquals(
        Encoding.UTF8.GetBytes(signature),
        Encoding.UTF8.GetBytes(computedSignature))) {
    context.Response.StatusCode = 401;
    return;
}

private string ComputeHmac(string data) {
    var secret = _configuration["Security:PayloadSignatureKey"];
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
    return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data))).ToLower();
}
```

### Headers de Segurança HTTP

O `SecurityHeadersMiddleware` adiciona headers de proteção em todas as respostas e remove informações sobre a tecnologia usada no servidor:

| Header | Valor | Proteção |
|---|---|---|


---

## 4. Segurança de Dados e Privacidade

### Criptografia de Senhas em Repouso

Senhas nunca são armazenadas em texto plano. O algoritmo **BCrypt** com work factor 12 é utilizado, tornando ataques de força bruta computacionalmente inviáveis:

```csharp
 AuthService.cs
SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha, workFactor: 12)
```

### Separação de Dados Sensíveis por Ambiente

Credenciais de banco de dados e chaves JWT apartados do código fonte. Em desenvolvimento, é utilizado o `appsettings.Development.json`. E em produção, são provisionadas exclusivamente via variáveis de ambiente, usando a convenção de duplo underscore do .NET:

```bash
export Jwt__Key="CHAVE-FIAP...."
export ConnectionStrings__DefaultConnection="Server=...;Password=..;"
```

### Proteção contra Exposição Acidental de Dados

Os DTOs de resposta expõem apenas os campos necessários para cada contexto. O Swagger é habilitado exclusivamente em ambiente de desenvolvimento:

```csharp
 Program.cs
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

### Anonimização em Logs

Dados pessoais identificáveis não são escritos nos logs. Eventos de falha usam descrições genéricas:

```csharp
_logger.LogWarning("[WAR - SEC] Falha de login para email ");
_logger.LogInformation("[REGISTRO] Tentativa com email já cadastrado ");
```

---

## 5. Monitoramento

### Logs Estruturados e Seguros

Todos os logs utilizam a API estruturada do `ILogger<T>` do .NET, com campos nomeados que permitem indexação e filtragem por ferramentas como Elasticsearch ou Application Insights. Nenhum dado sensível é incluído:

```csharp
_logger.LogInformation(
    "[Info] UserId={UserId} IP={IP} Method={Method} Path={Path} Status={Status} Timestamp={Timestamp}",
    userId, ip, method, path, statusCode, DateTime.UtcNow
);
```

Em produção, o nível de log do Entity Framework é configurado como `Warning` apenas.

### Monitoramento de Eventos Suspeitos

Alertas automáticos para eventos suspeitos por segurança relevantes em três pontos da aplicação:

**Rate limit atingido** — `RateLimitingMiddleware`:
```csharp
_logger.LogWarning("[WAR - SEC] Rate limit atingido para IP {IP} no path {Path}", ip, path);
```

**Token JWT inválido** — `JwtBearerEvents`:
```csharp
logger.LogWarning("[WAR - SEC] Token inválido — IP={IP} Path={Path}", ip, path);
```

**Payload com assinatura inválida** — `PayloadSignatureMiddleware`:
```csharp
_logger.LogWarning("[WAR - SEC] Assinatura inválida — IP={IP} Path={Path}", ip, path);
```

### Trilha de Info para Ações Críticas

O `AuditMiddleware` registra automaticamente todas as requisições em endpoints sensíveis (`/api/auth/`, `/api/veiculo`, `/api/manutencao`, `/api/cliente`), incluindo identidade do usuário, IP de origem, método HTTP, path e código de resposta. Respostas 401 e 403 geram alertas de nível `Warning` adicionais para facilitar a detecção de tentativas de acesso indevido:

```csharp
 AuditMiddleware.cs
if (statusCode == 401 || statusCode == 403) {
    _logger.LogWarning(
        "[WAR - SEC] Acesso negado — UserId={UserId} IP={IP} Path={Path} Status={Status}",
        userId, ip, path, statusCode
    );
}
```

Eventos críticos dos services:

```csharp
_logger.LogInformation("[Info] Novo cliente registrado — Id={Id}", cliente.Id);
_logger.LogInformation("[Info] Login bem-sucedido — ClienteId={Id}", cliente.Id);
```

---

## Sumário das Implementações

| Categoria | Implementação | Arquivo |
|---|---|---|
| Validação de entrada | Data Annotations em todos os DTOs | `DTOs/*.cs` |
| Sanitização | Normalização e regex no AuthService | `AuthService.cs` |
| Anti SQL Injection | Entity Framework Core (queries parametrizadas) | `*Repository.cs` |
| Limite de payload | Kestrel + PayloadSizeLimitMiddleware | `Program.cs`, `Middlewares/` |
| Erro seguro | ErrorHandlingMiddleware sem stack trace em produção | `Middlewares/ErrorHandlingMiddleware.cs` |
| Autenticação JWT | HMAC-SHA256, expiração, jti, notBefore, ClockSkew=0 | `AuthService.cs`, `Program.cs` |
| RBAC | Policies por papel via claims no token | `Program.cs`, Controllers |
| Anti timing attack | BCrypt.Verify sempre executado + FixedTimeEquals | `AuthService.cs`, `Middlewares/` |
| Rate limiting | Janela deslizante por IP, limite duplo para auth | `Middlewares/RateLimitingMiddleware.cs` |
| CORS restritivo | Origens, métodos e headers explícitos | `Program.cs` |
| Assinatura de payload | HMAC-SHA256 via header X-Signature | `Middlewares/PayloadSignatureMiddleware.cs` |
| Security headers | 6 headers + remoção do Server | `Middlewares/SecurityHeadersMiddleware.cs` |
| HTTPS/HSTS | TLS obrigatório + HSTS 1 ano | `SecurityHeadersMiddleware.cs` |
| Senha em repouso | BCrypt work factor 12 | `AuthService.cs` |
| Segredos por ambiente | Variáveis de ambiente em produção | `appsettings.*.json` |
| Swagger restrito | Apenas em Development | `Program.cs` |
| Logs sem PII | Emails e CPFs mascarados nos logs | `AuthService.cs` |
| Info | AuditMiddleware em todos os endpoints críticos | `Middlewares/AuditMiddleware.cs` |
| Alertas suspeitos | LogWarning em 401/403, rate limit, token inválido | Múltiplos middlewares |
