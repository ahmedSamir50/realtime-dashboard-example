var builder = DistributedApplication.CreateBuilder(args);

// ── Infrastructure ──────────────────────────────────────────────────────────

var postgres = builder.AddPostgres("postgres")
                      .WithDataVolume()
                      .WithPgAdmin();  // browse DB at http://localhost:{pgadmin-port}

// Name MUST match AppSettingsConstants.PostgreSqlConnection = "PostgreSQL"
var stockDb = postgres.AddDatabase("PostgreSQL");

var redis = builder.AddRedis("redis")
                   .WithDataVolume()
                   .WithRedisInsight(); // browse cache at http://localhost:{redisinsight-port}

// ── Services ────────────────────────────────────────────────────────────────

var api = builder.AddProject<Projects.Stock_RealTime_API>("stock-realtime-api")
                 .WithReference(stockDb)
                 .WithReference(redis)
                 .WaitFor(postgres)
                 .WaitFor(redis);

// path: StockPriceDashboardExample\src\FE\ionic\stock_realtime_example
builder.AddJavaScriptApp("ionic-dashboard", "../../../FE/ionic/stock_realtime_example", "start")
       .WithReference(api)
       .WithHttpEndpoint(port: 8100, name: "ionic-http")
       .WithExternalHttpEndpoints()
       .PublishAsDockerFile();

builder.Build().Run();
