using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Stock.RealTime.Infrastructure;

public sealed class DbInitilaizer : BackgroundService
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<DbInitilaizer> _logger;
    private readonly IConfiguration _configuration;

    public DbInitilaizer(NpgsqlDataSource dataSource,
                          ILogger<DbInitilaizer> logger,
                          IConfiguration configuration)
    {
        _dataSource = dataSource;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting database table initialization...");
            // Aspire ensures the database itself exists based on the name in AppHost
            await InitializeDatabaseAsync();
            _logger.LogInformation("Database initialization completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing database tables.");
        }
    }

    private async Task InitializeDatabaseAsync() { 
    
        const string createTableQuery = """
            CREATE TABLE IF NOT EXISTS public.stock_prices (
                id SERIAL PRIMARY KEY,
                ticker VARCHAR(10) NOT NULL,
                price NUMERIC(12, 6) NOT NULL,
                "timestamp" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC')
                );

                CREATE INDEX IF NOT EXISTS idx_stock_prices_ticker ON public.stock_prices (ticker);

                CREATE INDEX IF NOT EXISTS idx_stock_prices_timestamp ON public.stock_prices ("timestamp");
            """;
        
        using var connection = await _dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(createTableQuery);
    }
}
