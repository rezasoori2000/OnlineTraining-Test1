// ================================================================
//  PGLLMS Admin API — Program.cs
// ================================================================
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PGLLMS.Admin.Application;
using PGLLMS.Admin.Infrastructure;
using PGLLMS.Admin.API.Services;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------------------
// Authentication: two parallel schemes
//   1. Azure AD (Microsoft.Identity.Web)   — scheme "AzureAD"
//   2. Local JWT (email/password)           — scheme "LocalJwt"
//   PolicyScheme "MultiScheme" routes to the correct handler
//   based on the token issuer.
// ----------------------------------------------------------------

var jwtSettings = builder.Configuration.GetSection("Jwt");
var azureAdSection = builder.Configuration.GetSection("AzureAd");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = "MultiScheme";
        options.DefaultChallengeScheme = "MultiScheme";
    })
    .AddJwtBearer("LocalJwt", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["Key"]!)),
            ClockSkew = TimeSpan.Zero
        };
    })
    .AddMicrosoftIdentityWebApi(azureAdSection, jwtBearerScheme: "AzureAD");

// AddPolicyScheme must be chained on AuthenticationBuilder, not the Identity builder
builder.Services
    .AddAuthentication()
    .AddPolicyScheme("MultiScheme", "MultiScheme", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
            if (authHeader is not null && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader["Bearer ".Length..].Trim();
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                if (handler.CanReadToken(token))
                {
                    var jwtToken = handler.ReadJwtToken(token);
                    if (jwtToken.Issuer.Contains("microsoftonline", StringComparison.OrdinalIgnoreCase) ||
                        jwtToken.Issuer.Contains("login.microsoft", StringComparison.OrdinalIgnoreCase))
                    {
                        return "AzureAD";
                    }
                }
            }
            return "LocalJwt";
        };
    });

builder.Services.AddAuthorization();

// ----------------------------------------------------------------
// Clean Architecture layers
// ----------------------------------------------------------------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// PDF upload / OneDrive orchestration service
builder.Services.AddScoped<ChapterPdfService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevFrontend", policy =>
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://localhost:5174",
                "https://localhost:5173",
                "https://localhost:5174")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Allow large file uploads (PDF OCR endpoint accepts up to 100 MB)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100 MB
});

// ----------------------------------------------------------------
// Swagger — supports Bearer token entry for both schemes
// ----------------------------------------------------------------
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PGLLMS Admin API", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT (local login or Azure AD token)"
    };

    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PGLLMS Admin API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseCors("DevFrontend");
app.MapControllers();

app.Run();

// Marker for EF Migrations design-time factory
public partial class Program { }

// -------  dead code below removed — original template placeholder  -------

