using Microsoft.Extensions.DependencyInjection;
using Stock.RealTime.Application.Abstractions.RealTime;
using Stock.RealTime.Application.Abstractions.Stocks;
using Stock.RealTime.Infrastructure.BackgroundServices;
using Stock.RealTime.Infrastructure.Integrations;
using Stock.RealTime.Infrastructure.Persistence;
using Stock.RealTime.Infrastructure.Services.RealTime;

namespace Stock.RealTime.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            // Persistence
            services.AddScoped<IStockInfoDataService, StockInfoRepository>();

            // External Integrations
            services.AddScoped<IStockDataClientService, ExternalStockClient>();

            // Real-Time Services
            services.AddSingleton<IActiveTickerManagerService, ActiveTickerManagerService>();
            
            // Background Jobs
            services.AddHostedService<DbInitilaizer>();
            services.AddHostedService<StocksFeedUpdater>();

            return services;
        }
    }
}
