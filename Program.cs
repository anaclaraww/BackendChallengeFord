using System.Text;
using BackendChallengeFord.Controllers;
using BackendChallengeFord.Data;
using BackendChallengeFord.Middlewares;
using BackendChallengeFord.Repositories;
using BackendChallengeFord.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,             
            ValidateIssuerSigningKey = true,    
            ClockSkew = TimeSpan.Zero,          
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                logger.LogWarning("[ALERTA SEGURANÇA] Token inválido — IP={IP} Path={Path}",
                    context.HttpContext.Connection.RemoteIpAddress,
                    context.HttpContext.Request.Path);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ClientePolicy", policy => policy.RequireClaim("role", "cliente"));
    options.AddPolicy("AdminPolicy", policy => policy.RequireClaim("role", "admin"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000"
            )
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
            .WithHeaders("Authorization", "Content-Type", "X-Signature")
            .AllowCredentials();
    });
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Ford Platform API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Insira o token JWT desta maneira: Bearer {seu_token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var erros = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .SelectMany(e => e.Value!.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return new BadRequestObjectResult(new
            {
                status = 400,
                mensagem = "Dados inválidos.",
                erros
            });
        };
    });

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<VeiculoRepository>();
builder.Services.AddScoped<VeiculoService>();
builder.Services.AddScoped<ManutencaoRepository>();
builder.Services.AddScoped<PontosRepository>();
builder.Services.AddScoped<ManutencaoService>();
builder.Services.AddScoped<ClienteRepository>();
builder.Services.AddScoped<ClienteService>();

builder.Services.AddEndpointsApiExplorer();

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1 * 1024 * 1024; 
});

var app = builder.Build();


app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseMiddleware<RateLimitingMiddleware>();

app.UseMiddleware<PayloadSizeLimitMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AuditMiddleware>();
// app.UseMiddleware<PayloadSignatureMiddleware>(); // TODO: frontend estiver enviando X-Signature

app.MapControllers();

app.Run();
