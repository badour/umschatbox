using System;
using System.Data;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class ExpensesChatBot : Page
{
    private const string ActiveButtonClass = "chatbot-action active";
    private const string ButtonClass = "chatbot-action";

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            SetSelectedQueryType(ExpenseChatbotQueryType.FileLinks, true);
            litBotMessage.Text = "Hello. Choose one of the expense questions, enter the requested value, and click Ask.";
        }
    }

    protected void btnFileLinks_Click(object sender, EventArgs e)
    {
        SetSelectedQueryType(ExpenseChatbotQueryType.FileLinks, true);
    }

    protected void btnInvoiceExpenseCode_Click(object sender, EventArgs e)
    {
        SetSelectedQueryType(ExpenseChatbotQueryType.InvoiceExpenseCode, true);
    }

    protected void btnPersonExpenses_Click(object sender, EventArgs e)
    {
        SetSelectedQueryType(ExpenseChatbotQueryType.PersonExpenses, true);
    }

    protected void btnSend_Click(object sender, EventArgs e)
    {
        ExpenseChatbotQueryType queryType = GetSelectedQueryType();
        ExpenseChatbotResponse response = new ExpenseChatbotService().Ask(queryType, txtUserInput.Text);

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

    private void SetSelectedQueryType(ExpenseChatbotQueryType queryType, bool clearResults)
    {
        ExpenseChatbotQueryDefinition definition = ExpenseChatbotQueryDefinition.FromType(queryType);

        ViewState["ExpenseChatbotQueryType"] = queryType.ToString();
        hdnQueryType.Value = queryType.ToString();
        litSelectedQuery.Text = definition.Title;
        lblInput.Text = definition.InputLabel;
        txtUserInput.Attributes["placeholder"] = definition.InputPlaceholder;
        botMessage.Attributes["class"] = "chatbot-message";

        UpdateActiveButton(queryType);

        if (clearResults)
        {
            txtUserInput.Text = string.Empty;
            litBotMessage.Text = definition.EmptyInputMessage;
            BindResults(null);
            txtUserInput.Focus();
        }
    }

    private ExpenseChatbotQueryType GetSelectedQueryType()
    {
        object selectedValue = ViewState["ExpenseChatbotQueryType"];
        string selectedText = selectedValue == null ? hdnQueryType.Value : selectedValue.ToString();

        ExpenseChatbotQueryType queryType;
        if (ExpenseChatbotService.TryParseQueryType(selectedText, out queryType))
        {
            return queryType;
        }

        return ExpenseChatbotQueryType.FileLinks;
    }

    private void UpdateActiveButton(ExpenseChatbotQueryType queryType)
    {
        btnFileLinks.CssClass = queryType == ExpenseChatbotQueryType.FileLinks ? ActiveButtonClass : ButtonClass;
        btnInvoiceExpenseCode.CssClass = queryType == ExpenseChatbotQueryType.InvoiceExpenseCode ? ActiveButtonClass : ButtonClass;
        btnPersonExpenses.CssClass = queryType == ExpenseChatbotQueryType.PersonExpenses ? ActiveButtonClass : ButtonClass;
    }

    private void BindResults(DataTable results)
    {
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
