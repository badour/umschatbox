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
            case ExpenseChatbotQueryType.FileLinks:
                return new ExpenseChatbotQueryDefinition(
                    queryType,
                    "Find file links",
                    "Invoice number, expense code, file name, or other search text",
                    "Example: INV-10045 or receipt.pdf",
                    "Enter a search value so I can look for matching file links.",
                    "I found {0} file link result(s).",
                    "I could not find any file links for that search.",
                    "ExpenseChatbot.FileLinks.StoredProcedure",
                    "dbo.Chatbot_GetFileLinks",
                    "ExpenseChatbot.FileLinks.ParameterName",
                    "@SearchText");

            case ExpenseChatbotQueryType.InvoiceExpenseCode:
                return new ExpenseChatbotQueryDefinition(
                    queryType,
                    "Find expense code by invoice",
                    "Invoice number",
                    "Example: INV-10045",
                    "Enter an invoice number so I can look up its expense code details.",
                    "I found {0} expense code result(s) for that invoice.",
                    "I could not find expense code details for that invoice.",
                    "ExpenseChatbot.InvoiceExpenseCode.StoredProcedure",
                    "dbo.Chatbot_GetInvoiceExpenseCodes",
                    "ExpenseChatbot.InvoiceExpenseCode.ParameterName",
                    "@InvoiceNumber");

            case ExpenseChatbotQueryType.PersonExpenses:
                return new ExpenseChatbotQueryDefinition(
                    queryType,
                    "Find expenses by person",
                    "Person name, employee number, or user id",
                    "Example: Aisha Khan",
                    "Enter a person name or id so I can look up related expenses.",
                    "I found {0} expense result(s) for that person.",
                    "I could not find expenses related to that person.",
                    "ExpenseChatbot.PersonExpenses.StoredProcedure",
                    "dbo.Chatbot_GetPersonExpenses",
                    "ExpenseChatbot.PersonExpenses.ParameterName",
                    "@PersonName");

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
