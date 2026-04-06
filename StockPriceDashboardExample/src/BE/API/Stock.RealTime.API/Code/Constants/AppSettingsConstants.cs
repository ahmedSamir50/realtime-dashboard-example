namespace Stock.RealTime.API.Code.Constants
{
    internal static class AppSettingsConstants
    {
        //internal const string StockDataApiBaseUrl = "StockApi:StockDataApiBaseUrl";
        internal const string PostgreSqlConnection = "PostgreSQL";

        /// <summary>
        /// The correlation item prop key.
        /// </summary>
        internal const string CorrelationItemPropKey = "CorrelationId";
        /// <summary>
        /// The application settings secret json path
        /// </summary>
        internal const string AppSettingsSecretJsonPath = "secrets/appsettings.secrets.json";

        internal const string CorsAllowedOrigins = "Cors:AllowedOrigins";

        internal const string StockUpdaterJobOptions = "StockUpdaterJobOptions";
    }
}
