using ServiceScan.SourceGenerator;
using Stock.RealTime.API.Code.Constants;


namespace Stock.RealTime.API.Code;

public static partial class MinimalHttpEndPointServiceCollectionExtensions
{
    [GenerateServiceRegistrations(
        AssignableTo = typeof(IEndPoint),
        CustomHandler = nameof(MapEndPoint)
        )]
    public static partial IEndpointRouteBuilder MapEndPoints(this IEndpointRouteBuilder endpointRouteBuilder);

    private static void MapEndPoint<T>(IEndpointRouteBuilder builder)
        where T : IEndPoint
    {
        T.MapEndPoint(builder);
    }
}

public static class AppSettingsExtentions
{
    public static WebApplicationBuilder AddAppSettingsJson(
            this WebApplicationBuilder hostBuilder,
            bool optional = true,
            bool reloadOnChange = true)
    {
        hostBuilder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
           .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true);
        return hostBuilder;
    }

    public static IHostBuilder AddAppSettingsSecretsJson(
            this IHostBuilder hostBuilder,
            bool optional = true,
            bool reloadOnChange = true)
    {
        return hostBuilder.ConfigureAppConfiguration((_, builder) =>
        {
            builder.AddJsonFile(
                path: AppSettingsConstants.AppSettingsSecretJsonPath,
                optional: optional,
                reloadOnChange: reloadOnChange
            );
        });
    }

}
