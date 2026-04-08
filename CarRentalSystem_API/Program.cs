using System.Text;
using CarRentalSystem_API.Function.BackgroundServices;
using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// This is Swagger configuration with JWT authentication
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
builder.Services.AddHostedService<VehicleStatusUpdateService>();
builder.Services.AddHostedService<UserStatusDeleteServices>();
builder.Services.AddHostedService<BannersStatusUpdateServices>();
builder.Services.AddHostedService<BookingTimeoutServices>();
builder.Services.AddHostedService<UserStatusDeleteServices>();
builder.Services.AddHostedService<PromotionStatusUpdateServices>();

var app = builder.Build();
app.UseStaticFiles();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// This is global exception handling middleware (jwt)
app.UseAuthorization();

app.MapControllers();

app.Run();
