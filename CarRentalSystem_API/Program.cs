using CarRentalSystem_API.Function;
using CarRentalSystem_API.Function.BackgroundServices;
using CarRentalSystem_API.Interface;
using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Malaysia time zone configuration UTC+8
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// Email configuration injection
builder.Services.AddTransient<IEmailService, EmailServices>();

// PDF configuration injection
builder.Services.AddTransient<IPdfService, PdfService>(); 
builder.Services.AddSwaggerGen(x =>
{
    x.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Car Rental System API",
        Version = "v1",
    });
    x.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    x.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
            },
             new string[] {}
        }

    });
});

// Security configuration (Rate Limiting) // This is global rate limiting policy, you can also create different policies for different endpoints if needed.
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("GlobalPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }
    ));
    options.AddPolicy("StrictPolicy", httpContext =>
         RateLimitPartition.GetFixedWindowLimiter(
             partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
             factory: _ => new FixedWindowRateLimiterOptions
             {
                 PermitLimit = 5,
                 Window = TimeSpan.FromMinutes(1)
             }
    ));
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            success = false,
            error = "Too Many Requests",
            message = "You have exceeded the allowed number of requests. Please try again later."
        }, token);
    };
});

// This is JWT authentication configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(option =>
{
    option.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
    // This is return error message when user have no permission to access resource, for example user with "user" role try to access admin resource.
    option.Events = new JwtBearerEvents
    {
        // Return role error message when user have no permission to access resource, for example user with "user" role try to access admin resource.
        OnForbidden = context =>
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            var errorResponse = new
            {
                success = false,
                errorType = "RoleError",
                message = "You do not have permission to access this resource."
            };
            return context.Response.WriteAsJsonAsync(errorResponse);
        },
        // Return token is expired error message when token is expired.
        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            var errorResponse = new
            {
                success = false,
                errorType = "TokenExpired",
                message = "Your token has expired. Please log in again to obtain a new token."
            };
            return context.Response.WriteAsJsonAsync(errorResponse);
        }
    };
});
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
// Add DbContext with SQL Server connection string from appsettings.json
builder.Services.AddDbContext<AppDbContext>
   (options => options
  .UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Kestrel and IIS to allow large file uploads (up to 1 GB) (This is image set up)
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodyBufferSize = 1073741824; // 1 GB
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1073741824; // 1 GB
});

// This is background services (automation tasks)
builder.Services.AddHostedService<BannersStatusUpdateServices>();
builder.Services.AddHostedService<BookingTimeoutServices>();
builder.Services.AddHostedService<DailyFinancialJobServices>();
builder.Services.AddHostedService<EmptyUserOtpServices>();
builder.Services.AddHostedService<PromotionStatusUpdateServices>();
builder.Services.AddHostedService<TripRemindersServices>();
builder.Services.AddHostedService<UserStatusDeleteServices>();
builder.Services.AddHostedService<VehicleStatusUpdateService>();
builder.Services.AddHostedService<IdempotencyCleanupService>();

var app = builder.Build();
app.UseStaticFiles();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// This is to get the real client IP address when the application is behind a reverse proxy (e.g., Nginx, Apache) or load balancer, which is important for rate limiting and logging.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});
app.UseRouting();

// This is global rate limiting middleware, you can also apply rate limiting policy to specific endpoints if needed.
app.UseRateLimiter();

// This is global exception handling middleware (jwt)
app.UseAuthentication(); 
app.UseAuthorization();

// This is global rate limiting middleware, you can also apply rate limiting policy to specific endpoints if needed.
app.MapControllers().RequireRateLimiting("GlobalPolicy"); // Apply global rate limiting policy to all endpoints



app.Run();
