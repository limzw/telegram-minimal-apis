using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using NpgsqlTypes;
using Serilog;
using Serilog.Sinks.PostgreSQL;
using Serilog.Sinks.PostgreSQL.ColumnWriters;
using System.Reflection;
using System.Text;
using System.Text.Json;
using TelegramMinimalAPIs.Common;
using TelegramMinimalAPIs.Common.Behaviours;
using TelegramMinimalAPIs.Common.Configuration;
using TelegramMinimalAPIs.Common.Database;
using TelegramMinimalAPIs.Common.ExceptionHandlers;
using TelegramMinimalAPIs.Common.Middleware;
using TelegramMinimalAPIs.Common.Services.Cookies;
using TelegramMinimalAPIs.Common.Services.Loggers;
using TelegramMinimalAPIs.Common.Services.RuntimeUser;

namespace TelegramMinimalAPIs
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            //this calls AddUserSecrets<T>() if environment is development which then ingests secrets.json
            var builder = WebApplication.CreateBuilder(args);

            //options pattern for configuration settings
            builder.Services.RegisterMyOptions(builder.Configuration);

            builder.Host.AddLogger(builder.Configuration, builder.Services);

            builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

            //register behaviours in MediatR pipeline
            builder.Services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));

            });

            builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
            builder.Services.AddExceptionHandler<IdempotencyExceptionHandler>();
            builder.Services.AddProblemDetails();

            builder.Services.AddHealthChecks();
            builder.Services.RegisterAppServices();
            builder.Services.RegisterDatabase(builder.Configuration);

            builder.Services.AddAuthorization();

            Action<JwtBearerOptions> myOptions = (o) =>
            {
                o.GetTokenValidationParameters(builder.Configuration);
                o.IncludeErrorDetails = true;
                o.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        var token = ctx.Request.Cookies["myToken"];

                        if (string.IsNullOrEmpty(token))
                        {
                            // explicitly mark as no token
                            ctx.HttpContext.Items["AuthFailure"] = "no_token";
                        }
                        else
                        {
                            ctx.Token = token;
                        }

                        return Task.CompletedTask;
                    },

                    OnAuthenticationFailed = ctx =>
                    {
                        ctx.HttpContext.Items["AuthFailure"] = ctx.Exception switch
                        {
                            SecurityTokenExpiredException => "token_expired",
                            SecurityTokenInvalidSignatureException => "invalid_signature",
                            _ => "unauthorized"
                        };
                        return Task.CompletedTask;
                    },

                    OnChallenge = async ctx =>
                    {
                        ctx.HandleResponse();

                        ctx.Response.StatusCode = 401;
                        ctx.Response.ContentType = "application/json";
                        ctx.Response.Headers["Cache-Control"] = "no-store";

                        // read from items — set in OnMessageReceived or OnAuthenticationFailed
                        var failure = ctx.HttpContext.Items["AuthFailure"] as string
                            ?? ctx.AuthenticateFailure switch
                            {
                                SecurityTokenExpiredException => "token_expired",
                                SecurityTokenInvalidSignatureException => "invalid_signature",
                                _ => "unauthorized"
                            };
                        await ctx.Response.WriteAsync(
                            JsonSerializer.Serialize(new { error = failure })
                        );
                    },
                };
            };
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(myOptions);

            builder.Services.AddEndpoints(typeof(Program).Assembly);

            if (builder.Environment.IsDevelopment())
            {
                builder.Services.AddCors(options =>
                {
                    //when running frontend in vscode or through nginx
                    options.AddPolicy("AllowFrontend",
                        policy =>
                        {
                            policy.WithOrigins(
                                "http://localhost:5173",
                                "http://localhost"
                                )
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials();
                        });
                });
            }
            else if (builder.Environment.IsProduction())
            {
                //configure kestrel if in production
                builder.WebHost.ConfigureKestrel(options =>
                {
                    options.ListenLocalhost(3000);
                });
            }

            var app = builder.Build();

            app.UseMiddleware<RequestLoggingMiddleware>();

            app.UseExceptionHandler();

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            //initialise all available ServiceUsers
            var userRegistry = app.Services.GetRequiredService<RuntimeUserRegistry>();
            using var scope = app.Services.CreateScope();
            await userRegistry.InitialiseAllActiveUsers(scope.ServiceProvider.GetRequiredService<AppDbContext>());

            //CORS come before auth
            if (app.Environment.IsDevelopment())
            {
                app.UseCors("AllowFrontend");
            }

            //identifies the user
            app.UseAuthentication();

            //checks if the user is allowed to access endpoints
            app.UseAuthorization();
            app.MapHealthChecks("/api/get-database-status").RequireAuthorization();


            app.MapEndpoints();

            var customLoggerWrapper = app.Services.GetRequiredService<CustomLoggerWrapper>();
            customLoggerWrapper.Log(Database.OVERVIEWLOGS, LogLevel.Information, "Starting application");

            var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStopping.Register(() =>
            {
                customLoggerWrapper.Log(Database.OVERVIEWLOGS, LogLevel.Information, "Closing application");
            });

            if (app.Environment.IsDevelopment())
            {
                IdentityModelEventSource.ShowPII = true;
                IConfiguration configuration = app.Configuration;
                await app.RunAsync($"http://{configuration["NetworkConfiguration:NetworkIp"]}:{configuration["NetworkConfiguration:NetworkPort"]}");
            }
            else
            {
                await app.RunAsync();
            }
        }
    }

    static class OptionsExtensions
    {
        public static IServiceCollection RegisterMyOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddOptions<ConnectionStrings>()
                .Bind(configuration.GetSection("ConnectionStrings"))
                .ValidateOnStart();

            services
                .AddOptions<JwtSettings>()
                .Bind(configuration.GetSection("JwtSettings"))
                .ValidateOnStart();

            services
                .AddOptions<RefreshTokenSettings>()
                .Bind(configuration.GetSection("RefreshTokenSettings"))
                .ValidateOnStart();

            services
                .AddOptions<TelegramSettings>()
                .Bind(configuration.GetSection("TelegramSettings"))
                .ValidateOnStart();

            return services;
        }
    }

    static class ServiceCollectionExtensions
    {
        public static IServiceCollection RegisterAppServices(this IServiceCollection services)
        {
            services.AddSingleton<RuntimeUserRegistry>();//this service persists runtime user objects
            services.AddSingleton<ICookieGenerator, CookieGenerator>();

            return services;
        }

        public static IServiceCollection RegisterDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            var connStringsSettings = configuration.GetSection("ConnectionStrings").Get<ConnectionStrings>();
            services.AddDbContext<AppDbContext>(ctx => ctx.UseNpgsql(connStringsSettings.DatabaseConnectionString));
            services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

            return services;
        }
    }

    static class LoggingServiceExtensions
    {
        public static IHostBuilder AddLogger(this IHostBuilder builder, IConfiguration configuration, IServiceCollection services)
        {
            //columnOptions have to be defined here in code and not in appSettings.json due to limitation of serilog library for postgresql
            var apiLogsColumnOptions = new Dictionary<string, ColumnWriterBase>
            {
                { "Method",     new SinglePropertyColumnWriter("method") },
                { "Endpoint",   new SinglePropertyColumnWriter("path") },
                { "StatusCode", new SinglePropertyColumnWriter("status_code", PropertyWriteMethod.Raw, NpgsqlDbType.Integer) },
                { "Duration",   new SinglePropertyColumnWriter("duration", PropertyWriteMethod.Raw, NpgsqlDbType.Integer) },
                { "Timestamp",  new TimestampColumnWriter(NpgsqlDbType.TimestampTz) },
                { "Message",    new RenderedMessageColumnWriter() },
            };

            Dictionary<string, ColumnWriterBase> overviewLogsColumnOptions = new Dictionary<string, ColumnWriterBase>
            {
                { "Severity", new LevelColumnWriter(renderAsText: true, NpgsqlDbType.Text)},
                { "Timestamp",  new TimestampColumnWriter(NpgsqlDbType.TimestampTz) },
                { "Message",    new RenderedMessageColumnWriter() },
            };

            Dictionary<string, ColumnWriterBase> serviceUserActivitiesColumnOptions = new Dictionary<string, ColumnWriterBase>
            {
                {"Guid", new SinglePropertyColumnWriter("phoneNumber", PropertyWriteMethod.ToString, NpgsqlDbType.Text, format: "l") },
                {"Timestamp", new TimestampColumnWriter(NpgsqlDbType.TimestampTz)},
                {"Details", new RenderedMessageColumnWriter() },
            };

            var connectionStrings = configuration.GetSection("ConnectionStrings").Get<ConnectionStrings>()!;

            builder.UseSerilog((_, loggerConfig) =>
            {
                loggerConfig.ReadFrom.Configuration(configuration) //read from secrets.json
                .Enrich.FromLogContext() //allows the use of PushProperty
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e => e.Properties.TryGetValue("database", out var db) && db is Serilog.Events.ScalarValue sv &&
                        sv.Value?.ToString() == Database.APILOGS.ToString())
                    .WriteTo.PostgreSQL(
                        connectionString: connectionStrings.DatabaseConnectionString,
                        tableName: "ApiLogs",
                        columnOptions: apiLogsColumnOptions,
                        needAutoCreateTable: false,
                        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error
                    )
                )
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e => e.Properties.TryGetValue("database", out var db) && db is Serilog.Events.ScalarValue sv &&
                         sv.Value?.ToString() == Database.OVERVIEWLOGS.ToString())
                    .WriteTo.PostgreSQL(
                        connectionString: connectionStrings.DatabaseConnectionString,
                        tableName: "OverviewLogs",
                        columnOptions: overviewLogsColumnOptions,
                        needAutoCreateTable: false,
                        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information
                    )
                )
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e => e.Properties.TryGetValue("database", out var db) && db is Serilog.Events.ScalarValue sv &&
                         sv.Value?.ToString() == Database.SERVICEUSERSACTIVITY.ToString())
                    .WriteTo.PostgreSQL(
                        connectionString: connectionStrings.DatabaseConnectionString,
                        tableName: "ServiceUsersActivity",
                        columnOptions: serviceUserActivitiesColumnOptions,
                        needAutoCreateTable: false,
                        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error
                    )
                );
            });

            services.AddSingleton<CustomLoggerWrapper>();

            return builder;
        }
    }

    static class EndpointExtensions
    {
        public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
        {
            //gets all public concrete classes that implement IEndpoint
            var endpointTypes = assembly
                                .GetExportedTypes() //gets all public typed only
                                .Where(t => typeof(IEndpoint).IsAssignableFrom(t)
                                         && t is { IsAbstract: false, IsInterface: false });

            foreach (var type in endpointTypes)
            {
                services.AddTransient(typeof(IEndpoint), type);
            }

            return services;
        }

        public static IApplicationBuilder MapEndpoints(this WebApplication app)
        {
            var endpoints = app.Services.GetRequiredService<IEnumerable<IEndpoint>>();

            foreach (var endpoint in endpoints)
            {
                endpoint.MapEndpoint(app);
            }

            return app;
        }
    }

    static class JwtBearerOptionsExtensions
    {
        //extension method to handle setting of token validation parameters based on environment
        public static void GetTokenValidationParameters(this JwtBearerOptions o, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();

            o.TokenValidationParameters = new TokenValidationParameters
            {
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.JwtSecretKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.JwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.JwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        }
    }
}
