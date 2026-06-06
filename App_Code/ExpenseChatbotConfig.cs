using System;
using System.Configuration;

public static class ExpenseChatbotConfig
{
    private const string DefaultConnectionStringName = "generalUniversityDB";
    private const int DefaultCommandTimeoutSeconds = 30;
    private const int DefaultMaxRows = 50;
    private const double DefaultIntentConfidenceThreshold = 0.35D;

    public static string GetConnectionString()
    {
        string connectionStringName = GetConnectionStringName();
        ConnectionStringSettings connectionString = ConfigurationManager.ConnectionStrings[connectionStringName];

        if (connectionString == null || string.IsNullOrWhiteSpace(connectionString.ConnectionString))
        {
            throw new ConfigurationErrorsException(
                string.Format(
                    "Missing connection string '{0}'. Add it to Web.config or set ExpenseChatbot.ConnectionStringName to an existing connection string.",
                    connectionStringName));
        }

        return connectionString.ConnectionString;
    }

    public static string GetConnectionStringName()
    {
        return GetAppSetting("ExpenseChatbot.ConnectionStringName", DefaultConnectionStringName);
    }

    public static int GetCommandTimeoutSeconds()
    {
        return GetPositiveIntSetting("ExpenseChatbot.CommandTimeoutSeconds", DefaultCommandTimeoutSeconds);
    }

    public static int GetMaxRows()
    {
        return GetPositiveIntSetting("ExpenseChatbot.MaxRows", DefaultMaxRows);
    }

    public static double GetIntentConfidenceThreshold()
    {
        string value = ConfigurationManager.AppSettings["ExpenseChatbot.IntentConfidenceThreshold"];
        double parsedValue;

        if (!double.TryParse(value, out parsedValue) || parsedValue <= 0D || parsedValue > 1D)
        {
            return DefaultIntentConfidenceThreshold;
        }

        return parsedValue;
    }

    public static bool ShowDetailedErrors()
    {
        string value = ConfigurationManager.AppSettings["ExpenseChatbot.ShowDetailedErrors"];
        bool parsedValue;
        return bool.TryParse(value, out parsedValue) && parsedValue;
    }

    private static string GetAppSetting(string key, string defaultValue)
    {
        string value = ConfigurationManager.AppSettings[key];
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
    }

    private static int GetPositiveIntSetting(string key, int defaultValue)
    {
        string value = ConfigurationManager.AppSettings[key];
        int parsedValue;

        if (!int.TryParse(value, out parsedValue) || parsedValue <= 0)
        {
            return defaultValue;
        }

        return parsedValue;
    }
}
