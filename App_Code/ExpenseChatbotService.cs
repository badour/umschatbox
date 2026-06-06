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

    public ExpenseChatbotResponse AskNaturalLanguage(string question, ExpenseChatbotQueryType fallbackQueryType)
    {
        string normalizedQuestion = NormalizeInput(question);

        if (string.IsNullOrWhiteSpace(normalizedQuestion))
        {
            return new ExpenseChatbotResponse(
                "Type a question like 'show me expenses for Ahmed' or enter a value after choosing a button.",
                null,
                false);
        }

        ExpenseChatbotNaturalLanguageRequest request =
            new ExpenseChatbotNaturalLanguageParser().Parse(normalizedQuestion, fallbackQueryType);
        ExpenseChatbotQueryDefinition definition = ExpenseChatbotQueryDefinition.FromType(request.QueryType);

        if (string.IsNullOrWhiteSpace(request.SearchValue))
        {
            string emptyMessage = request.UsedNaturalLanguage
                ? string.Format("I understood your question as '{0}', but I still need a search value. {1}", definition.Title, definition.EmptyInputMessage)
                : definition.EmptyInputMessage;

            return new ExpenseChatbotResponse(emptyMessage, null, false);
        }

        ExpenseChatbotResponse response = Ask(request.QueryType, request.SearchValue);

        if (response.IsError)
        {
            return response;
        }

        string messagePrefix = request.UsedNaturalLanguage
            ? string.Format("I understood your question as '{0}' and searched for '{1}'. ", definition.Title, request.SearchValue)
            : string.Empty;

        return new ExpenseChatbotResponse(messagePrefix + response.Message, response.Results, response.IsError);
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
