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

            case ExpenseChatbotQueryType.StudentRevenueSummary:
                return new ExpenseChatbotQueryDefinition(
                    queryType,
                    "Analyze student revenues",
                    "Natural-language student revenue question",
                    "Example: ما هي مجموع الايرادات الطلبة لكل الاعوام الدراسية؟",
                    "Ask about student revenues, income, or receipts.",
                    "I calculated {0} student revenue result(s).",
                    "I could not calculate student revenues for that question.",
                    "ExpenseChatbot.StudentRevenueSummary.StoredProcedure",
                    "dbo.Chatbot_GetStudentRevenueSummary",
                    "ExpenseChatbot.StudentRevenueSummary.ParameterName",
                    "@SearchText");

            case ExpenseChatbotQueryType.AccountRelatedExpenses:
                return new ExpenseChatbotQueryDefinition(
                    queryType,
                    "Analyze expenses for an accounting account",
                    "Account code, account name, or natural-language account expense question",
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
                    "Natural-language fixed-assets total question",
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
                    "Natural-language buildings total question",
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
                    "Natural-language total expenses question",
                    "Example: المجموع الكلي للمصروفات لشخص معين او لتبويب محاسبي معين",
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
