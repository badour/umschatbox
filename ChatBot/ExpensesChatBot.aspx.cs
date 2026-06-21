using System;
using System.Data;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

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

    protected void grdResults_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row.RowType != DataControlRowType.Data || grdResults.HeaderRow == null)
        {
            return;
        }

        for (int cellIndex = 0; cellIndex < e.Row.Cells.Count; cellIndex++)
        {
            string headerText = HttpUtility.HtmlDecode(grdResults.HeaderRow.Cells[cellIndex].Text);
            string cellText = HttpUtility.HtmlDecode(e.Row.Cells[cellIndex].Text);

            if (!IsLinkColumn(headerText) || !IsSafeLink(cellText))
            {
                continue;
            }

            HyperLink link = new HyperLink();
            link.Text = "Open file";
            link.NavigateUrl = cellText;
            link.Target = "_blank";

            e.Row.Cells[cellIndex].Controls.Clear();
            e.Row.Cells[cellIndex].Controls.Add(link);
        }
    }

    private void InitializeChatbot()
    {
        litSelectedQuery.Text = "Ask anything";
        lblInput.Text = "Type your question";
        txtUserInput.Attributes["placeholder"] = "Example: ما هي مجموع الايرادات الطلبة لكل الاعوام الدراسية؟";
        botMessage.Attributes["class"] = "chatbot-message";
        litBotMessage.Text = "Hello. Ask a full question in Arabic or English and I will choose the right SQL query, analyze the data, and explain the result.";
        BindResults(null);
        txtUserInput.Focus();
    }

    private void BindResults(DataTable results)
    {
        grdResults.Visible = results != null && results.Rows.Count > 0;
        grdResults.DataSource = results;
        grdResults.DataBind();
    }

    private static bool IsLinkColumn(string headerText)
    {
        if (string.IsNullOrWhiteSpace(headerText))
        {
            return false;
        }

        string normalizedHeader = headerText.ToLowerInvariant();
        return normalizedHeader.Contains("link")
            || normalizedHeader.Contains("url")
            || normalizedHeader.Contains("filepath")
            || normalizedHeader.Contains("file_path")
            || normalizedHeader.Contains("path");
    }

    private static bool IsSafeLink(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "&nbsp;")
        {
            return false;
        }

        string trimmedValue = value.Trim();

        if (trimmedValue.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        Uri uri;
        if (!Uri.TryCreate(trimmedValue, UriKind.RelativeOrAbsolute, out uri))
        {
            return false;
        }

        if (!uri.IsAbsoluteUri)
        {
            return true;
        }

        return string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }
}
