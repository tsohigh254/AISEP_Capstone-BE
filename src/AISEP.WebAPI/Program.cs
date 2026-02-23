using System.Text;
using AISEP.Application.Configuration;
using AISEP.Application.Interfaces;
using AISEP.Domain.Interfaces;
using AISEP.Infrastructure.Services;
using AISEP.Infrastructure.Data;
using AISEP.Infrastructure.Settings;
using AISEP.WebAPI.Middlewares;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/aisep-.log", 
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{RequestId}] [{UserId}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting AISEP Web API");
    
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

// Add services to the container.

// JWT Settings
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("AISEP.Infrastructure")));

// Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IStartupService, StartupService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IInvestorService, InvestorService>();
builder.Services.AddScoped<IAdvisorService, AdvisorService>();
builder.Services.AddScoped<IMentorshipService, MentorshipService>();
builder.Services.AddScoped<IConnectionsService, ConnectionsService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IModerationService, ModerationService>();
builder.Services.AddScoped<IBlockchainProofService, BlockchainProofService>();
builder.Services.AddSingleton<IBlockchainService, StubBlockchainService>(); // TODO: swap with real blockchain RPC for production

// Storage (local file system for dev — swap to Azure Blob / S3 for production)
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
builder.Services.AddSingleton<IStorageService>(new LocalStorageService(uploadsPath));

// Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false; // Don't remap JWT claim names to .NET claim names
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.Zero,
        NameClaimType = "sub"
    };
});

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("StartupOnly", policy => policy.RequireClaim("userType", "Startup"));
    options.AddPolicy("InvestorOnly", policy => policy.RequireClaim("userType", "Investor"));
    options.AddPolicy("AdvisorOnly", policy => policy.RequireClaim("userType", "Advisor"));
    options.AddPolicy("StaffOrAdmin", policy => policy.RequireClaim("userType", "Staff", "Admin"));
    options.AddPolicy("AdminOnly", policy => policy.RequireClaim("userType", "Admin"));
});

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Controllers
builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "AISEP API", Version = "v1" });
    
    // JWT Bearer configuration for Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
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
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

    // Seed database
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await DbSeeder.SeedAsync(context);
    }

    // Configure the HTTP request pipeline.
    app.UseRequestId();
    app.UseGlobalExceptionHandler();
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestId", httpContext.TraceIdentifier);
            diagnosticContext.Set("UserId", httpContext.User?.FindFirst("sub")?.Value ?? "anonymous");
        };
    });
    
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AISEP API v1"));
    }

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
