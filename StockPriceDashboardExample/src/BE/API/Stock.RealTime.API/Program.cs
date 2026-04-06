
using Cortex.Mediator.DependencyInjection;
using Stock.RealTime.API.Code;
using Stock.RealTime.API.Code.Constants;
using Stock.RealTime.Application.Abstractions.RealTime;
using Stock.RealTime.Application.Abstractions.Stocks;
using Stock.RealTime.Application.Integrations.Stocks;
using Stock.RealTime.Application.Services.RealTime;
using Stock.RealTime.Application.Services.Stocks;
using Stock.RealTime.Core.Options;
using Stock.RealTime.Infrastructure;
using Stock.RealTime.ServiceDefaults;

namespace Stock.RealTime.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.AddAppSettingsJson();
            builder.Host.AddAppSettingsSecretsJson();

            // ── Aspire service defaults (OpenTelemetry, health checks, service discovery) ──
            builder.AddServiceDefaults();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddCors();
            builder.Services.AddSignalR();

            // Register Cortex.Mediator (CQRS)
            builder.Services.AddCortexMediator(
                 handlerAssemblyMarkerTypes: [typeof(Program)], // Assemblies to scan for handlers
                configure: options =>
                {
                    options.AddDefaultBehaviors(); // Logging
                }
            );

            // ── PostgreSQL via Aspire (injects NpgsqlDataSource + OpenTelemetry tracing) ──
            // Connection string key matches AppSettingsConstants.PostgreSqlConnection = "PostgreSQL"
            // Aspire injects it as ConnectionStrings__PostgreSQL env var automatically
            builder.AddNpgsqlDataSource(AppSettingsConstants.PostgreSqlConnection);

            // ── Redis HybridCache (L1 in-memory + L2 Redis distributed cache) ────────────
            // Aspire injects the Redis connection string as ConnectionStrings__redis env var.
            // AddStackExchangeRedisCache registers IDistributedCache backed by Redis (L2 for HybridCache).
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = builder.Configuration.GetConnectionString("redis")
                    ?? throw new InvalidOperationException(
                        "Redis connection string 'redis' is not configured. " +
                        "Ensure the AppHost 'redis' resource is referenced and the app is run via Aspire.");
            });
            builder.Services.AddHybridCache(); // registers IHybridCache (L1 + L2 via IDistributedCache)

            builder.Services.AddScoped<IStockDataClientService, StockDataClientService>();
            builder.Services.AddScoped<IStockInfoDataService, StockInfoDataService>();
            builder.Services.AddSingleton<IActiveTickerManagerService, ActiveTickerManagerService>();
            builder.Services.AddHostedService<DbInitilaizer>();
            builder.Services.AddHostedService<StocksFeedUpdater>();

            builder.Services.Configure<StockUpdaterJobOptions>(builder.Configuration.GetSection(AppSettingsConstants.StockUpdaterJobOptions));


            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();
            app.MapEndPoints();
            app.MapDefaultEndpoints(); // Aspire health check endpoints (/health, /alive)

            app.MapHub<StocksFeedClientHub>("/stocks-feed");
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI();

                app.UseCors(policy =>
                {
                    policy.WithOrigins(builder.Configuration[AppSettingsConstants.CorsAllowedOrigins]?.Split(',') ?? ["http://localhost:3000", "http://localhost:8100"])
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
