using System;
using System.Data;
using System.Web;

public sealed class ExpenseChatbotService
{
    private readonly ExpenseChatbotRepository _repository;

    public ExpenseChatbotService()
    {
    }

    public ExpenseChatbotService(ExpenseChatbotRepository repository)
    {
        if (repository == null)
        {
            throw new ArgumentNullException("repository");
        }

        _repository = repository;
    }

    public ExpenseChatbotResponse Ask(ExpenseChatbotQueryType queryType, string input)
    {
        ExpenseChatbotQueryDefinition definition = ExpenseChatbotQueryDefinition.FromType(queryType);
        string normalizedInput = NormalizeInput(input);

        if (string.IsNullOrWhiteSpace(normalizedInput))
        {
            return new ExpenseChatbotResponse(definition.EmptyInputMessage, null, false);
        }

        try
        {
            DataTable results = GetRepository().Execute(definition, normalizedInput);
            int originalRowCount = results.Rows.Count;

            if (originalRowCount == 0)
            {
                return new ExpenseChatbotResponse(definition.NotFoundMessage, results, false);
            }

            int maxRows = ExpenseChatbotConfig.GetMaxRows();
            DataTable displayResults = LimitRows(results, maxRows);
            string message = string.Format(definition.FoundMessage, originalRowCount);

            if (originalRowCount > maxRows)
            {
                message += string.Format(" Showing the first {0} result(s).", maxRows);
            }

            return new ExpenseChatbotResponse(message, displayResults, false);
        }
        catch (Exception exception)
        {
            TraceException(exception);
            return new ExpenseChatbotResponse(
                "I could not complete that query. Please check the chatbot configuration or contact support.",
                null,
                true);
        }
    }

    private ExpenseChatbotRepository GetRepository()
    {
        return _repository ?? new ExpenseChatbotRepository();
    }

    public static bool TryParseQueryType(string value, out ExpenseChatbotQueryType queryType)
    {
        if (Enum.TryParse(value, true, out queryType) && Enum.IsDefined(typeof(ExpenseChatbotQueryType), queryType))
        {
            return true;
        }

        queryType = ExpenseChatbotQueryType.FileLinks;
        return false;
    }

    private static string NormalizeInput(string input)
    {
        return (input ?? string.Empty).Trim();
    }

    private static DataTable LimitRows(DataTable source, int maxRows)
    {
        if (source == null || source.Rows.Count <= maxRows)
        {
            return source;
        }

        DataTable limited = source.Clone();

        for (int rowIndex = 0; rowIndex < maxRows; rowIndex++)
        {
            limited.ImportRow(source.Rows[rowIndex]);
        }

        return limited;
    }

    private static void TraceException(Exception exception)
    {
        if (HttpContext.Current == null || HttpContext.Current.Trace == null)
        {
            return;
        }

        HttpContext.Current.Trace.Warn("ExpenseChatbot", exception.Message, exception);
    }
}
