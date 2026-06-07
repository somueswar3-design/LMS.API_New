using System.Text;
using LMS.API.Data;
using LMS.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
    .Enrich.FromLogContext()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// ─── Allow large uploads (2 GB) ───────────────────────────────
// Kestrel: remove default 30MB body limit
builder.Services.Configure<KestrelServerOptions>(opt =>
{
    opt.Limits.MaxRequestBodySize = 2L * 1024 * 1024 * 1024; // 2 GB
    opt.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
    opt.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
});

// IIS (if hosted on IIS)
builder.Services.Configure<IISServerOptions>(opt =>
{
    opt.MaxRequestBodySize = 2L * 1024 * 1024 * 1024;
});

// Form options — allow large multipart bodies
builder.Services.Configure<FormOptions>(opt =>
{
    opt.MultipartBodyLengthLimit  = 2L * 1024 * 1024 * 1024;
    opt.ValueLengthLimit          = int.MaxValue;
    opt.MultipartHeadersLengthLimit = int.MaxValue;
    opt.BufferBodyLengthLimit     = 2L * 1024 * 1024 * 1024;
});

// ─── MySQL via Pomelo ──────────────────────────────────────────
var conn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<LmsDbContext>(opt =>
    opt.UseMySql(conn, ServerVersion.AutoDetect(conn)));

// ─── JWT Auth ─────────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ValidateIssuer   = true, ValidIssuer   = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true, ValidAudience = builder.Configuration["Jwt:Audience"],
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<IAuthService,       AuthService>();
builder.Services.AddScoped<IRazorpayService,   RazorpayService>();
builder.Services.AddScoped<ICloudflareService, CloudflareService>();
builder.Services.AddScoped<IEmailService,      EmailService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "LMS API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Bearer token", Name = "Authorization",
        In = ParameterLocation.Header, Type = SecuritySchemeType.ApiKey, Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {{
        new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }}, []
    }});
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LmsDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LMS API v1"));
app.UseSerilogRequestLogging();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
