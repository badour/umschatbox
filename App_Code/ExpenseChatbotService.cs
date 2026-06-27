using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
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
        return AskInternal(queryType, input, false, false);
    }

    private ExpenseChatbotResponse AskInternal(ExpenseChatbotQueryType queryType, string input, bool textOnlyResponse, bool hasTimePeriod)
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
            DataTable displayResults = textOnlyResponse ? null : LimitRows(results, maxRows);
            string message = string.Format(definition.FoundMessage, originalRowCount);
            string analysis = textOnlyResponse ? string.Empty : BuildReasoningAnalysis(queryType, results, originalRowCount);

            if (textOnlyResponse)
            {
                message = ExpenseChatbotReasoningEngine.BuildConversationalAnswer(
                    queryType,
                    results,
                    originalRowCount,
                    hasTimePeriod);
            }
            else if (originalRowCount > maxRows)
            {
                message += string.Format(" Showing the first {0} result(s).", maxRows);
            }

            if (!textOnlyResponse && !string.IsNullOrWhiteSpace(analysis))
            {
                message += "\n\n" + analysis;
            }

            return new ExpenseChatbotResponse(message, displayResults, false);
        }
        catch (ConfigurationErrorsException exception)
        {
            TraceException(exception);
            return new ExpenseChatbotResponse(
                "Chatbot configuration error: " + exception.Message,
                null,
                true);
        }
        catch (SqlException exception)
        {
            TraceException(exception);
            return new ExpenseChatbotResponse(
                BuildSqlErrorMessage(definition, exception),
                null,
                true);
        }
        catch (Exception exception)
        {
            TraceException(exception);
            return new ExpenseChatbotResponse(
                BuildUnexpectedErrorMessage(exception),
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

        bool hasTimePeriod = HasTimePeriod(request);
        ExpenseChatbotResponse response = AskInternal(request.QueryType, request.SearchValue, true, hasTimePeriod);

        if (response.IsError)
        {
            return response;
        }

        string messagePrefix = request.UsedNaturalLanguage
            ? string.Format("I understood your question as '{0}' and searched for '{1}'. ", definition.Title, CleanSearchValueForDisplay(request.SearchValue))
            : string.Empty;

        return new ExpenseChatbotResponse(messagePrefix + response.Message, response.Results, response.IsError);
    }

    private ExpenseChatbotRepository GetRepository()
    {
        return _repository ?? new ExpenseChatbotRepository();
    }

    private static bool HasTimePeriod(ExpenseChatbotNaturalLanguageRequest request)
    {
        if (request == null)
        {
            return false;
        }

        string text = ((request.OriginalQuestion ?? string.Empty) + " " + (request.SearchValue ?? string.Empty)).ToLowerInvariant();

        return text.Contains("last ")
            || text.Contains("past ")
            || text.Contains("from ")
            || text.Contains("year")
            || text.Contains("academic")
            || text.Contains("20")
            || text.Contains("19")
            || text.Contains("اخر")
            || text.Contains("آخر")
            || text.Contains("سنة")
            || text.Contains("سنوات")
            || text.Contains("اعوام")
            || text.Contains("أعوام")
            || text.Contains("الاعوام")
            || text.Contains("الأعوام")
            || text.Contains("الدراسية")
            || text.Contains("من ")
            || text.Contains("حتى");
    }

    private static string CleanSearchValueForDisplay(string searchValue)
    {
        return (searchValue ?? string.Empty).Replace("|academic_years", string.Empty).Trim();
    }

    private static string BuildReasoningAnalysis(ExpenseChatbotQueryType queryType, DataTable results, int originalRowCount)
    {
        try
        {
            return ExpenseChatbotReasoningEngine.BuildAnalysis(queryType, results, originalRowCount);
        }
        catch (Exception exception)
        {
            TraceException(exception);
            return string.Empty;
        }
    }

    public static bool TryParseQueryType(string value, out ExpenseChatbotQueryType queryType)
    {
        if (Enum.TryParse(value, true, out queryType) && Enum.IsDefined(typeof(ExpenseChatbotQueryType), queryType))
        {
            return true;
        }

        queryType = ExpenseChatbotQueryType.AccountingExpensesTotal;
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

    private static string BuildSqlErrorMessage(ExpenseChatbotQueryDefinition definition, SqlException exception)
    {
        string message = string.Format(
            "Chatbot database error while running '{0}'. Check that Web.config connection string '{1}' points to the correct SQL database and that this stored procedure exists.",
            definition.StoredProcedureName,
            ExpenseChatbotConfig.GetConnectionStringName());

        if (IsMissingStoredProcedure(exception))
        {
            message += " SQL Server says the stored procedure was not found.";
        }

        return AppendDetailedError(message, exception);
    }

    private static string BuildUnexpectedErrorMessage(Exception exception)
    {
        string message = "I could not complete that query. Please check the chatbot configuration, SQL connection, and stored procedure setup.";
        return AppendDetailedError(message, exception);
    }

    private static string AppendDetailedError(string message, Exception exception)
    {
        if (!ExpenseChatbotConfig.ShowDetailedErrors() || exception == null)
        {
            return message;
        }

        return message + " Details: " + exception.Message;
    }

    private static bool IsMissingStoredProcedure(SqlException exception)
    {
        if (exception == null)
        {
            return false;
        }

        for (int index = 0; index < exception.Errors.Count; index++)
        {
            if (exception.Errors[index].Number == 2812)
            {
                return true;
            }
        }

        return false;
    }
}
