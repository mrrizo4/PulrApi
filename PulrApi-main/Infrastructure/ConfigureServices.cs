using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Core.Application.Helpers;
using Core.Application.Interfaces;
using Core.Application.Services;
using Core.Domain.Entities;
using Core.Infrastructure.Persistence;
using Core.Infrastructure.Persistence.Interceptors;
using Core.Infrastructure.Security;
using Core.Infrastructure.Services;
using Core.Infrastructure.Services.Stripe;
using Core.Infrastructure.Services.Users;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Stripe;

namespace Core.Infrastructure
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services,
            IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment, string MyAllowSpecificOrigins)
        {
            services.AddSignalR(cfg => cfg.EnableDetailedErrors = true);

            services.AddScoped<EntitySaveChangesInterceptor>();

            var connectionString = configuration.GetConnectionString("Pulr");
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString,
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

            services.AddScoped<IApplicationDbContext>(provider => provider.GetService<ApplicationDbContext>());

            services.AddCors(options =>
            {
                options.AddPolicy(name: MyAllowSpecificOrigins,
                    builder =>
                    {
                        builder
                            //.WithOrigins(configuration["Cors:Origins:Origin1"],
                            //         configuration["Cors:Origins:Origin2"],
                            //         configuration["Cors:Origins:Origin3"])
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowAnyOrigin();
                        //.SetIsOriginAllowed(origin => true);

                    });
            });

            services.AddHangfire(hf => hf.UsePostgreSqlStorage(connectionString, new PostgreSqlStorageOptions() { SchemaName = "cron" }));
            services.AddHangfireServer();

            services
                .AddControllers(options =>
                {
                    options.Conventions.Add(new RouteTokenTransformerConvention(new SlugifyParameterTransformer()));
                })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                });

            services.AddHttpClient();
            services.AddHttpContextAccessor();
            services.AddTransient<IHttpClientService, HttpClientService>();
            services.AddTransient<IEmailService, EmailService>();
            services.AddTransient<IExchangeRateService, ExchangeRateService>();

            services.AddScoped<IUserBlockService, UserBlockService>();

            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IQueryHelperService, QueryHelperService>();
            //services.AddScoped<ISearchRepository, SearchRepository>();

            services.AddSingleton<IFacebookAuthService, FacebookAuthService>();
            services.AddSingleton<IGoogleAuthService, GoogleAuthService>();
            services.AddSingleton<IAppleAuthService, AppleAuthService>();
            // Register TokenBlacklistService
            services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();

            #region Auth Config

            //Configuration from AppSettings
            services.Configure<JWT>(configuration.GetSection("JWT"));
            services.AddIdentity<User, IdentityRole>(o =>
                {
                    o.Password.RequiredLength = 6;
                    //o.Password.RequireUppercase = true;
                    //o.Password.RequireDigit = true;
                    //o.Password.RequireNonAlphanumeric = true;
                    //o.Password.RequiredUniqueChars = 3;

                    o.User.RequireUniqueEmail = true;
                    //o.SignIn.RequireConfirmedEmail = true;

                    o.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
                    o.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders()
                .AddTokenProvider<CustomEmailConfirmationTokenProvider<User>>("CustomEmailConfirmation");

            services.Configure<DataProtectionTokenProviderOptions>(o => { o.TokenLifespan = TimeSpan.FromHours(6); });

            //Adding Athentication - JWT
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(o =>
                {
                    o.RequireHttpsMetadata = false;
                    o.SaveToken = true; // check this
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,
                        ValidIssuer = configuration["JWT:Issuer"],
                        ValidAudience = configuration["JWT:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Key"]))
                    };
                    o.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];

                            // If the request is for our hubs...
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) && (path.StartsWithSegments("/hubs")))
                            {
                                // Read the token out of the query string
                                context.Token = accessToken;
                            }

                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddAuthorization(options =>
            {
                // protect all routes
                options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

                //options.AddPolicy("SuperAdmin", policy => { policy.Requirements.Add(new SuperAdminRequirement()); });
            });

            #endregion

            #region Services

            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IProfileService, ProfileService>();
            services.AddScoped<IStoreService, StoreService>();
            services.AddScoped<IPostService, PostService>();
            services.AddScoped<IProfileSettingsService, ProfileSettingsService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddTransient<IFileUploadService, FileUploadService>();
            services.AddTransient<IQueryHelperService, QueryHelperService>();
            services.AddTransient<IStripeService, StripeService>();
            services.AddTransient<IStripeClient, StripeClient>();
            services.AddHttpClient<IGoogleAuthService, GoogleAuthService>();

            #endregion

            #region Swagger

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "API",
                    Version = "v1",
                });
                //TODO Fix swagger authorize role filter
                //options.DocumentFilter<SwaggerAuthorizeRoleFilter>();
                options.AddSecurityDefinition("BearerDefinition", new OpenApiSecurityScheme()
                {
                    Name = "Authorization",
                    Description = "Type 'Bearer' (no quotes) followed by space and paste your JWT token here.",
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Scheme = "Bearer"
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "BearerDefinition"
                            }
                        },
                        new List<string>()
                    }
                });
            });

            #endregion
            
            #region Hangfire
            services.AddHangfire(config =>
                config.UsePostgreSqlStorage(configuration.GetConnectionString("Pulr"),
                    new PostgreSqlStorageOptions
                    {
                        InvisibilityTimeout = TimeSpan.FromMinutes(5),
                        QueuePollInterval = TimeSpan.FromMilliseconds(200),
                        DistributedLockTimeout = TimeSpan.FromMinutes(1)
                    }));

            #endregion

            return services;
        }
    }
}