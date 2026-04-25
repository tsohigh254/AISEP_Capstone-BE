using AISEP.Application.Configuration;
using AISEP.Application.Interfaces;
using AISEP.Domain.Interfaces;
using AISEP.Infrastructure.Data;
using AISEP.Infrastructure.Services;
// AI Integration usings are resolved via the above namespaces
using AISEP.Infrastructure.Settings;
using AISEP.Infrastructure.Jobs;
using Hangfire;
using Hangfire.PostgreSql;
using AISEP.WebAPI.Hubs;
using AISEP.WebAPI.Infrastructure.SignalR;
using AISEP.WebAPI.Middlewares;
using Microsoft.AspNetCore.SignalR;
using CloudinaryDotNet;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PayOS;
using Serilog;
using Serilog.Events;
using System.Text;

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

    // Load .env file (secrets) into environment variables
    var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
    if (File.Exists(envPath))
    {
        DotNetEnv.Env.Load(envPath);
        Log.Information("Loaded .env file");
    }
    else
    {
        Log.Warning(".env file not found at {Path} — relying on system environment variables", envPath);
    }

    var builder = WebApplication.CreateBuilder(args);

    // Override config with environment variables (ConnectionStrings__DefaultConnection, Jwt__SecretKey, etc.)
    builder.Configuration.AddEnvironmentVariables();

    builder.Host.UseSerilog();

// Limit upload size to 20 MB
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 20 * 1024 * 1024;
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 20 * 1024 * 1024;
});

// Add services to the container.

// CORS — allow FE origin(s) with credentials (cookies)
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Cloudinary Configuration
builder.Services.Configure<CloudinaryOptions>(
  builder.Configuration.GetSection("CloudinaryOptions"));
// Payment Configuration
builder.Services.Configure<PaymentOptions>(
  builder.Configuration.GetSection("PaymentOptions"));


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
builder.Services.AddScoped<IAdvisorService, AdvisorService>();
builder.Services.AddScoped<IAdvisorBookmarkService, AdvisorBookmarkService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddHttpClient<IEmailService, EmailService>();
builder.Services.AddScoped<IStartupService, StartupService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IInvestorService, InvestorService>();
builder.Services.AddScoped<IMentorshipService, MentorshipService>();
builder.Services.AddScoped<IConnectionsService, ConnectionsService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<AISEP.Application.Interfaces.INotificationDeliveryService, AISEP.WebAPI.Services.NotificationDeliveryService>();


builder.Services.AddScoped<IModerationService, ModerationService>();
builder.Services.AddScoped<IRegistrationService, RegistrationApprovalService>();
builder.Services.AddScoped<IBlockchainProofService, BlockchainProofService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IStaffDashboardService, StaffDashboardService>();
builder.Services.AddScoped<IStaffMasterDataService, StaffMasterDataService>();
builder.Services.AddScoped<IIssueReportService, IssueReportService>();
builder.Services.AddTransient<ICloudinaryService, CloudinaryService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<AISEP.Infrastructure.Jobs.SubscriptionExpirationJob>();
builder.Services.AddScoped<AISEP.Infrastructure.Jobs.SessionAutoConfirmJob>();
builder.Services.AddHttpClient<IAIService, AIService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["AIService:BaseUrl"] ?? "http://ai:8000");
    client.Timeout = TimeSpan.FromSeconds(300);
});

// ── Python AI Service Integration ──────────────────────────────
builder.Services.Configure<PythonAiOptions>(builder.Configuration.GetSection(PythonAiOptions.SectionName));
var pythonAiOpts = builder.Configuration.GetSection(PythonAiOptions.SectionName).Get<PythonAiOptions>() ?? new PythonAiOptions();
builder.Services.AddHttpClient<PythonAiClient>(client =>
{
    client.BaseAddress = new Uri(pythonAiOpts.BaseUrl);
    // Set Infinite so HttpClient never cuts the connection globally.
    // Each method in PythonAiClient controls its own per-call timeout via CancellationToken.
    client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddScoped<IAiEvaluationService, AiEvaluationService>();
builder.Services.AddScoped<IAiRecommendationService, AiRecommendationService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<IAiInvestorAgentService, AiInvestorAgentService>();

builder.Services.AddKeyedSingleton("OrderClient", (p, key) =>
    {
        var options = p.GetRequiredService<IOptions<PaymentOptions>>().Value;

        var account = new PayOSClient
        (
            options.ClientId,
            options.ApiKey, 
            options.ChecksumKey
        );

        return account;
    });

builder.Services.AddKeyedSingleton("TransferClient", (p, key) =>
    {
        var options = p.GetRequiredService<IOptions<PaymentOptions>>().Value;

        var account = new PayOSClient
        (
            options.CashoutClientId,
            options.CashoutApiKey,
            options.CashoutChecksumKey
        );

        return account;
    });



    // Blockchain — Ethereum (Sepolia) via Nethereum
    builder.Services.Configure<BlockchainSettings>(builder.Configuration.GetSection("Blockchain"));
builder.Services.AddSingleton<IBlockchainService, EthereumBlockchainService>();

// Storage (local file system for dev — swap to Azure Blob / S3 for production)
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");

builder.Services.AddTransient<ICloudinaryService, CloudinaryService>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

// Hangfire Background Jobs setup
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));

builder.Services.AddHangfireServer();

builder.Services.Configure<CloudinaryOptions>(
    builder.Configuration.GetSection("CloudinaryOptions"));

    // Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false; // Don't remap JWT claim names to .NET claim names

    // SignalR gửi token qua query string ?access_token= (không thể dùng Authorization header với WebSocket)
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };

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

// Controllers – override default ProblemDetails for validation errors → ApiEnvelope
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .Select(e => new { field = e.Key, messages = e.Value!.Errors.Select(x => x.ErrorMessage).ToArray() })
                .ToList();

            var envelope = new AISEP.Application.DTOs.Common.ApiEnvelope<object>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = "Validation failed",
                Data = errors
            };

            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(envelope);
        };
    });

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

    var skipMigrateOnStartup = builder.Configuration.GetValue<bool>("Database:SkipMigrateOnStartup");
    if (!skipMigrateOnStartup)
    {
        // Migrate & seed database — app must not start with a broken schema
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();
            await DbSeeder.SeedAsync(context);
        }
    }
    else
    {
        Log.Warning("Skipping database migration/seed on startup because Database:SkipMigrateOnStartup=true");
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
        // Giảm noise: OPTIONS preflight + polling endpoints chỉ log ở Debug
        options.GetLevel = (httpContext, elapsed, ex) =>
        {
            if (ex != null) return LogEventLevel.Error;
            if (httpContext.Request.Method == "OPTIONS") return LogEventLevel.Debug;
            var path = httpContext.Request.Path.Value ?? "";
            if (path.StartsWith("/api/notifications", StringComparison.OrdinalIgnoreCase))
                return LogEventLevel.Debug;
            return LogEventLevel.Information;
        };
    });
    
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AISEP API v1"));
        app.UseHangfireDashboard("/hangfire");
    }

    // Recurring job: auto-confirm sessions that ended > 24h ago without Startup confirmation
    // Use IRecurringJobManager (DI-based) instead of static RecurringJob to avoid
    // "JobStorage not initialized" crash when Hangfire server starts as a hosted service
    var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
    recurringJobs.AddOrUpdate<AISEP.Infrastructure.Jobs.SessionAutoConfirmJob>(
        "session-auto-confirm",
        job => job.RunAsync(),
        "*/15 * * * *");  // every 15 minutes

    app.UseHttpsRedirection();
    app.UseCors("Frontend");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHub<ChatHub>("/hubs/chat");
    app.MapHub<NotificationHub>("/hubs/notifications");

    // [TEMPORARY FIX] Ensure missing mentorship columns exist
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        try {
            await context.Database.ExecuteSqlRawAsync(@"
                ALTER TABLE ""StartupAdvisorMentorships"" ADD COLUMN IF NOT EXISTS ""CancelledAt"" timestamp with time zone NULL;
                ALTER TABLE ""StartupAdvisorMentorships"" ADD COLUMN IF NOT EXISTS ""CancelledBy"" text NULL;
                ALTER TABLE ""StartupAdvisorMentorships"" ADD COLUMN IF NOT EXISTS ""CancellationReason"" text NULL;
            ");
            Log.Information("Applied manual database fix for mentorship columns");
        } catch (Exception ex) {
            Log.Warning("Manual database fix skipped or failed: {Message}", ex.Message);
        }
    }

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
