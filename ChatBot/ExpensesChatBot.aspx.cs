using System;
using System.Data;
using System.Web.UI;

public partial class ExpensesChatBot : Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            InitializeChatbot();
        }
    }

    protected void btnSend_Click(object sender, EventArgs e)
    {
        ExpenseChatbotResponse response = new ExpenseChatbotService().AskNaturalLanguage(
            txtUserInput.Text,
            ExpenseChatbotQueryType.AccountingExpensesTotal);

        botMessage.Attributes["class"] = response.IsError ? "chatbot-message error" : "chatbot-message";
        litBotMessage.Text = response.Message;
        BindResults(response.Results);
    }

    private void InitializeChatbot()
    {
        litSelectedQuery.Text = "Ask anything";
        lblInput.Text = "Type your question";
        txtUserInput.Attributes["placeholder"] = "Example: المصاريف المتعلقة بالصيانة السيارات 3314";
        botMessage.Attributes["class"] = "chatbot-message";
        litBotMessage.Text = "Hello. Ask a full question in Arabic or English and I will choose the right SQL query, analyze the data, and explain the result.";
        BindResults(null);
        txtUserInput.Focus();
    }

    private void BindResults(DataTable results)
    {
        // Natural-language answers are text-only. Results are analyzed by the service and not bound to a table.
    }
}
