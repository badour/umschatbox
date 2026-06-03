using System;
using System.Configuration;
using Xunit;

public sealed class ChatbotIntegrationTests
{
    private static string ConnectionString
    {
        get
        {
            ConnectionStringSettings setting = ConfigurationManager.ConnectionStrings["ExpensesDb"];
            if (setting != null && !string.IsNullOrWhiteSpace(setting.ConnectionString))
            {
                return setting.ConnectionString;
            }

            string fromEnvironment = Environment.GetEnvironmentVariable("EXPENSES_DB_CONNECTION_STRING");
            if (!string.IsNullOrWhiteSpace(fromEnvironment))
            {
                return fromEnvironment;
            }

            return "Server=127.0.0.1,1433;Database=ExpensesDev;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;";
        }
    }

    [Fact]
    public void Ask_FileLinks_ReturnsMatchingReceipts()
    {
        ExpenseChatbotResponse response = new ExpenseChatbotService(
            new ExpenseChatbotRepository(ConnectionString, 30)).Ask(ExpenseChatbotQueryType.FileLinks, "INV-10045");

        Assert.False(response.IsError);
        Assert.True(response.HasResults);
        Assert.Contains("file link", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Ask_InvoiceExpenseCode_ReturnsExpenseRows()
    {
        ExpenseChatbotResponse response = new ExpenseChatbotService(
            new ExpenseChatbotRepository(ConnectionString, 30)).Ask(ExpenseChatbotQueryType.InvoiceExpenseCode, "INV-10045");

        Assert.False(response.IsError);
        Assert.True(response.HasResults);
        Assert.Equal(2, response.Results.Rows.Count);
    }

    [Fact]
    public void Ask_PersonExpenses_ReturnsPersonRows()
    {
        ExpenseChatbotResponse response = new ExpenseChatbotService(
            new ExpenseChatbotRepository(ConnectionString, 30)).Ask(ExpenseChatbotQueryType.PersonExpenses, "Aisha");

        Assert.False(response.IsError);
        Assert.True(response.HasResults);
        Assert.Contains("Aisha", response.Results.Rows[0]["DisplayName"].ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Ask_EmptyInput_ReturnsValidationMessage()
    {
        ExpenseChatbotResponse response = new ExpenseChatbotService(
            new ExpenseChatbotRepository(ConnectionString, 30)).Ask(ExpenseChatbotQueryType.FileLinks, "   ");

        Assert.False(response.IsError);
        Assert.False(response.HasResults);
        Assert.Contains("search value", response.Message, StringComparison.OrdinalIgnoreCase);
    }
}
