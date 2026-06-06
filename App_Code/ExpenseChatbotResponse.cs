using System.Data;

public sealed class ExpenseChatbotResponse
{
    public ExpenseChatbotResponse(string message, DataTable results, bool isError)
    {
        Message = message;
        Results = results;
        IsError = isError;
    }

    public string Message { get; private set; }

    public DataTable Results { get; private set; }

    public bool IsError { get; private set; }

    public bool HasResults
    {
        get { return Results != null && Results.Rows.Count > 0; }
    }
}
