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

            case ExpenseChatbotQueryType.ExpenseNetValue:
                return new ExpenseChatbotQueryDefinition(
                    queryType,
                    "Calculate expense net value",
                    "Period or natural-language expense summary question",
                    "Example: how many expenses for last 3 years",
                    "Enter a period such as 'last 3 years' or ask for total/net expenses.",
                    "I calculated {0} expense summary result(s).",
                    "I could not calculate expense net value for that period.",
                    "ExpenseChatbot.ExpenseNetValue.StoredProcedure",
                    "dbo.Chatbot_GetExpenseNetValue",
                    "ExpenseChatbot.ExpenseNetValue.ParameterName",
                    "@SearchText");

            case ExpenseChatbotQueryType.FixedAssetsByAccount:
                return new ExpenseChatbotQueryDefinition(
                    queryType,
                    "Analyze fixed assets by account",
                    "Period or natural-language fixed assets question",
                    "Example: مجموع الموجودات الثابته مفصلة حسب الحسابات الثلاثية لمدة اخر 3 سنوات",
                    "Enter a period or ask for fixed assets grouped by account.",
                    "I calculated {0} fixed assets analysis result(s).",
                    "I could not calculate fixed assets analysis for that period.",
                    "ExpenseChatbot.FixedAssetsByAccount.StoredProcedure",
                    "dbo.Chatbot_GetFixedAssetsByAccount",
                    "ExpenseChatbot.FixedAssetsByAccount.ParameterName",
                    "@SearchText");

            case ExpenseChatbotQueryType.PersonPaymentTotal:
                return new ExpenseChatbotQueryDefinition(
                    queryType,
                    "Analyze paid amounts by person",
                    "Person name or natural-language payment question",
                    "Example: ما هي القيمة الكلية للمبالغ المصروفة الى حسين حيدر",
                    "Enter a person name or ask for total paid amounts to a person.",
                    "I calculated {0} person payment analysis result(s).",
                    "I could not calculate paid amounts for that person.",
                    "ExpenseChatbot.PersonPaymentTotal.StoredProcedure",
                    "dbo.Chatbot_GetPersonPaymentTotal",
                    "ExpenseChatbot.PersonPaymentTotal.ParameterName",
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
