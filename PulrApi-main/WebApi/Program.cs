using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Web;
using Core.Infrastructure.Persistence;
using Core.Domain.Entities;
using Core.Application;
using Core.Infrastructure;
using Dashboard.Application;
using WebApi.Configurations.NLog;
using WebApi.Middleware;
using WebApi.ViewModels;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Core.Application.Models.MediaFiles;
using Core.Application.Hubs;
using WebApi.Configurations.AutoMapper;
using Hangfire;
using Dashboard.Application.Hubs;
using Core.Application.Security;
using Core.Infrastructure.Services.Cron;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Kestrel configuration
builder.WebHost.UseUrls("http://0.0.0.0:5000");
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000);
    options.Limits.MaxRequestBodySize = 30 * 1024 * 1024; // 30MB
});

// NLog config
NLogSetup.Configure(builder.Configuration);

// Authentication and Authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError("Authentication failed for {RequestPath}: {Message}", 
                    context.Request.Path, context.Exception.Message);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("Authentication challenge for {RequestPath}", context.Request.Path);
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization(options => options.FallbackPolicy = null);

// Service registration
builder.Services.AddApplication();
builder.Services.AddDashboardApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment, "_myAllowSpecificOrigins");
builder.Services.AddAutoMapper(MappingRegistrationFromMultipleAssembiles.GetAssemblies());
builder.Services.AddTransient<UserEngagementMiddleware>();
builder.Services.AddControllers(options =>
{
    options.ModelBinderProviders.Insert(0, new UploadMediaFileDtoModelBinderProvider(builder.Services.BuildServiceProvider().GetRequiredService<ILogger<UploadMediaFileDtoModelBinder>>()));
});
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 30 * 1024 * 1024; // 30MB
});

var app = builder.Build();

// Log startup information after building the app
var appStartupLogger = app.Services.GetRequiredService<ILogger<Program>>();
appStartupLogger.LogInformation("Starting application with ASPNETCORE_ENVIRONMENT: {Environment}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
appStartupLogger.LogInformation("Configured to listen on: http://0.0.0.0:5000");

// Log all requests
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Processing request: {Path} at {Time}", context.Request.Path, DateTime.UtcNow);
    await next();
    logger.LogInformation("Finished processing request: {Path}", context.Request.Path);
});

// Middleware pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<UserEngagementMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/jobs", new DashboardOptions()
    {
        Authorization = new[] { new HangFireAuthorizationFilter() }
    });
    app.UseDeveloperExceptionPage();
    app.UseSwagger(c => c.SerializeAsV2 = true);
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PULR API v1");
        c.InjectStylesheet("/swagger/ui/swagger-custom.css");
        c.InjectJavascript("/swagger/ui/swagger-custom.js");
        c.DefaultModelsExpandDepth(-1);
        c.DocumentTitle = "PULR API";
        string baseUrl = builder.Configuration["BaseUrl"]?.TrimEnd('/') ?? ""; // Corrected line
        c.SwaggerEndpoint($"{baseUrl}/swagger/v1/swagger.json", "PULR API v1");
    });
}

app.UseExceptionHandler("/errors");
app.UseStaticFiles();
app.UseCors("_myAllowSpecificOrigins");
HangfireJobScheduler.ScheduleRecurringJobs();
app.UseHangfireDashboard();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notification");

// Database migration
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbLogger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        if (context.Database.IsNpgsql())
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            dbLogger.LogInformation("Applying database migrations...");
            context.Database.Migrate();
            var userManager = services.GetRequiredService<UserManager<User>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var configuration = services.GetRequiredService<IConfiguration>();
            dbLogger.LogInformation("Seeding database...");
            await ApplicationDbContextSeed.SeedAsync(userManager, roleManager, configuration, context);
        }
        dbLogger.LogInformation("Application started successfully.");
    }
    catch (Exception e)
    {
        dbLogger.LogError(e, "Error during migration or seeding.");
        throw;
    }
}

appStartupLogger.LogInformation("Application is now running...");
app.Run();
