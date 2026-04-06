using Cortex.Mediator.DependencyInjection;
using Stock.RealTime.API.Code;
using Stock.RealTime.API.Code.Constants;
using Stock.RealTime.API.Hubs;
using Stock.RealTime.Application.Abstractions.RealTime;
using Stock.RealTime.Application.Abstractions.Stocks;
using Stock.RealTime.Core.Options;
using Stock.RealTime.Infrastructure;
using Stock.RealTime.Infrastructure.Services.RealTime;
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

            // ── PostgreSQL via Aspire ──
            builder.AddNpgsqlDataSource(AppSettingsConstants.PostgreSqlConnection);

            // ── Redis HybridCache ────────────
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = builder.Configuration.GetConnectionString("redis")
                    ?? throw new InvalidOperationException("Redis connection string 'redis' is not configured.");
            });
            builder.Services.AddHybridCache(); // registers IHybridCache

            // ── Infrastructure (Clean Architecture) ──
            builder.Services.AddInfrastructure();

            builder.Services.Configure<StockUpdaterJobOptions>(builder.Configuration.GetSection(AppSettingsConstants.StockUpdaterJobOptions));

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddOpenApi();

            var app = builder.Build();
            app.MapEndPoints();
            app.MapDefaultEndpoints(); // Aspire health checks

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
