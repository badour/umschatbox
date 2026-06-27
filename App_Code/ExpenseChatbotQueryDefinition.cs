using System;
using System.Configuration;

public sealed class ExpenseChatbotQueryDefinition
{
    public ExpenseChatbotQueryDefinition(
        ExpenseChatbotQueryType queryType,
        string title,
        string inputLabel,
        string inputPlaceholder,
        string emptyInputMessage,
        string foundMessage,
        string notFoundMessage,
        string storedProcedureSettingName,
        string defaultStoredProcedureName,
        string parameterSettingName,
        string defaultParameterName)
    {
        QueryType = queryType;
        Title = title;
        InputLabel = inputLabel;
        InputPlaceholder = inputPlaceholder;
        EmptyInputMessage = emptyInputMessage;
        FoundMessage = foundMessage;
        NotFoundMessage = notFoundMessage;
        StoredProcedureName = GetSetting(storedProcedureSettingName, defaultStoredProcedureName);
        ParameterName = NormalizeParameterName(GetSetting(parameterSettingName, defaultParameterName));
    }

    public ExpenseChatbotQueryType QueryType { get; private set; }

    public string Title { get; private set; }

    public string InputLabel { get; private set; }

    public string InputPlaceholder { get; private set; }

    public string EmptyInputMessage { get; private set; }

    public string FoundMessage { get; private set; }

    public string NotFoundMessage { get; private set; }

    public string StoredProcedureName { get; private set; }

    public string ParameterName { get; private set; }

    public static ExpenseChatbotQueryDefinition FromType(ExpenseChatbotQueryType queryType)
    {
        switch (queryType)
        {
            case ExpenseChatbotQueryType.AccountRelatedExpenses:
                return new ExpenseChatbotQueryDefinition(
                    queryType,
                    "Analyze expenses for an accounting account",
                    "Account code or account name",
                    "Example: المصاريف المتعلقة بالصيانة السيارات 3314",
                    "Ask about expenses related to an accounting account or category.",
                    "I calculated {0} account-related expense result(s).",
                    "I could not calculate expenses for that accounting account.",
                    "ExpenseChatbot.AccountRelatedExpenses.StoredProcedure",
                    "dbo.Chatbot_GetAccountRelatedExpenses",
                    "ExpenseChatbot.AccountRelatedExpenses.ParameterName",
                    "@SearchText");

            case ExpenseChatbotQueryType.FixedAssetsTotal:
                return new ExpenseChatbotQueryDefinition(
                    queryType,
                    "Analyze total fixed assets",
                    "Period or all-years fixed-assets question",
                    "Example: المجموع الكلي للموجودات الثابتة للكلية لكل السنوات",
                    "Ask about total fixed assets for a period or all years.",
                    "I calculated {0} fixed-assets total result(s).",
                    "I could not calculate total fixed assets.",
                    "ExpenseChatbot.FixedAssetsTotal.StoredProcedure",
                    "dbo.Chatbot_GetFixedAssetsTotal",
                    "ExpenseChatbot.FixedAssetsTotal.ParameterName",
                    "@SearchText");

            case ExpenseChatbotQueryType.BuildingsTotal:
                return new ExpenseChatbotQueryDefinition(
                    queryType,
                    "Analyze total buildings",
                    "Period or all-years buildings question",
                    "Example: المجموع الكلي للمباني للكلية لكل الاعوام",
                    "Ask about total buildings for a period or all years.",
                    "I calculated {0} buildings total result(s).",
                    "I could not calculate total buildings.",
                    "ExpenseChatbot.BuildingsTotal.StoredProcedure",
                    "dbo.Chatbot_GetBuildingsTotal",
                    "ExpenseChatbot.BuildingsTotal.ParameterName",
                    "@SearchText");

            case ExpenseChatbotQueryType.AccountingExpensesTotal:
                return new ExpenseChatbotQueryDefinition(
                    queryType,
                    "Analyze total accounting expenses",
                    "Person, account code, account name, period, or all expenses question",
                    "Example: المجموع الكلي للمصروفات لتبويب محاسبي 3314",
                    "Ask about total expenses for all expenses, a person, or an accounting code.",
                    "I calculated {0} accounting expense total result(s).",
                    "I could not calculate accounting expenses for that question.",
                    "ExpenseChatbot.AccountingExpensesTotal.StoredProcedure",
                    "dbo.Chatbot_GetAccountingExpensesTotal",
                    "ExpenseChatbot.AccountingExpensesTotal.ParameterName",
                    "@SearchText");

            default:
                throw new ArgumentOutOfRangeException("queryType");
        }
    }

    private static string GetSetting(string key, string defaultValue)
    {
        string configuredValue = ConfigurationManager.AppSettings[key];
        return string.IsNullOrWhiteSpace(configuredValue) ? defaultValue : configuredValue.Trim();
    }

    private static string NormalizeParameterName(string parameterName)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
        {
            return "@SearchText";
        }

        string trimmedName = parameterName.Trim();
        return trimmedName.StartsWith("@", StringComparison.Ordinal) ? trimmedName : "@" + trimmedName;
    }
}
