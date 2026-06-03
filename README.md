# Expenses WebForms Chatbot

This repository contains a drop-in ASP.NET WebForms chatbot module for an expenses application.
It gives users three guided buttons:

- Find file links
- Find expense code details for a specific invoice
- Find expenses related to a specific person

The module uses parameterized SQL stored procedure calls through ADO.NET. The fixed button flow does
not require ML.NET; if ML.NET is already installed in your application, you can add it later to classify
free-text questions and map them to the existing `ExpenseChatbotQueryType` values.

## Files

- `ChatBot/ExpensesChatBot.aspx` - WebForms chatbot page and UI.
- `ChatBot/ExpensesChatBot.aspx.cs` - page event handlers, button selection, and result binding.
- `App_Code/ExpenseChatbot*.cs` - query definitions, configuration, SQL repository, and service logic.
- `docs/Web.config.chatbot.example.config` - Web.config connection string and appSettings example.
- `docs/sql/chatbot-procedures-template.sql` - SQL stored procedure templates to adapt to your schema.

## Integration steps

1. Copy the `App_Code` files into the root `App_Code` folder of the existing WebForms site.
   - For a Web Application project, include the `.cs` files in the project and change the page directive
     from `CodeFile` to `CodeBehind` if that is how the project is structured.
2. Copy `ChatBot/ExpensesChatBot.aspx` and `ChatBot/ExpensesChatBot.aspx.cs` into the application.
   - If the existing app uses a master page, move the markup inside the appropriate `<asp:Content>` blocks.
3. Copy the relevant entries from `docs/Web.config.chatbot.example.config` into the existing `Web.config`.
4. Adapt and run `docs/sql/chatbot-procedures-template.sql` against the expenses database.
   - Replace the sample table names (`ExpenseFiles`, `Expenses`, `People`) and column names with the real schema.
5. Add a menu item or hyperlink in the existing app that points to `ChatBot/ExpensesChatBot.aspx`.
6. Restrict access to the chatbot page using the same authentication/authorization rules as the expenses pages.

## Configuration

By default, the chatbot expects a connection string named `ExpensesDb` and these stored procedures:

- `dbo.Chatbot_GetFileLinks @SearchText`
- `dbo.Chatbot_GetInvoiceExpenseCodes @InvoiceNumber`
- `dbo.Chatbot_GetPersonExpenses @PersonName`

You can override the connection string name, stored procedure names, parameter names, command timeout,
and max displayed rows through `appSettings` in `Web.config`.

## Security notes

- User input is sent to SQL Server as parameters, not string-concatenated SQL.
- Result text is HTML-encoded by WebForms controls.
- Columns whose names look like links or URLs are rendered as clickable links only for safe relative,
  `http`, or `https` values.
- Prefer granting the application login `EXECUTE` permission on the chatbot procedures instead of direct
  table access.
