# Expenses WebForms Chatbot

This repository contains a drop-in ASP.NET WebForms chatbot module for an expenses application.
Users can type natural-language questions, and the page also keeps three guided buttons as a fallback:

- Find file links
- Find expense code details for a specific invoice
- Find expenses related to a specific person

The module uses ML.NET to classify the user's intent, simple extraction rules to pull out the search
value, and parameterized SQL stored procedure calls through ADO.NET to query the database.

Example:

```text
User: show me expenses for Ahmed
Intent detected by ML.NET: PersonExpenses
Search value extracted by rules: Ahmed
SQL procedure called: dbo.Chatbot_GetPersonExpenses @PersonName = 'Ahmed'
```

## Files

- `ChatBot/ExpensesChatBot.aspx` - WebForms chatbot page and UI.
- `ChatBot/ExpensesChatBot.aspx.cs` - page event handlers, button selection, and result binding.
- `App_Code/ExpenseChatbot*.cs` - query definitions, configuration, SQL repository, and service logic.
- `App_Code/ExpenseChatbotNaturalLanguage.cs` - ML.NET intent classifier and natural-language value extraction.
- `docs/Web.config.chatbot.example.config` - Web.config connection string and appSettings example.
- `docs/sql/chatbot-procedures-template.sql` - SQL stored procedure templates to adapt to your schema.

## Integration steps

1. Copy the `App_Code` files into the root `App_Code` folder of the existing WebForms site.
   - For a Web Application project, include the `.cs` files in the project and change the page directive
     from `CodeFile` to `CodeBehind` if that is how the project is structured.
2. Confirm the application references ML.NET (`Microsoft.ML`). The natural-language classifier uses this package.
3. Copy `ChatBot/ExpensesChatBot.aspx` and `ChatBot/ExpensesChatBot.aspx.cs` into the application.
   - If the existing app uses a master page, move the markup inside the appropriate `<asp:Content>` blocks.
4. Copy the relevant entries from `docs/Web.config.chatbot.example.config` into the existing `Web.config`.
5. Adapt and run `docs/sql/chatbot-procedures-template.sql` against the expenses database.
   - Replace the sample table names (`ExpenseFiles`, `Expenses`, `People`) and column names with the real schema.
6. Add a menu item or hyperlink in the existing app that points to `ChatBot/ExpensesChatBot.aspx`.
7. Restrict access to the chatbot page using the same authentication/authorization rules as the expenses pages.

## Natural-language examples

These examples are classified automatically:

- `show me expenses for Ahmed` -> `PersonExpenses`, search value `Ahmed`
- `list expenses submitted by employee Sara` -> `PersonExpenses`, search value `Sara`
- `what is the expense code for invoice INV-10045` -> `InvoiceExpenseCode`, search value `INV-10045`
- `find file links for receipt.pdf` -> `FileLinks`, search value `receipt.pdf`
- `ابحث عن ملف رقم المستند 12345` -> `FileLinks`, search value `12345`
- `اعرض الملفات بالتكلفة 1500` -> `FileLinks`, search value `1500`
- `هات مرفق بتاريخ 2024-05-10` -> `FileLinks`, search value `2024-05-10`

If a user types only a direct value, such as `INV-10045`, the selected button is used as the fallback query type.

For the `adminmodeluniversitiy` app, the file-link query should search `dbo.UploadExpenseIncome`.
The `@SearchText` value can match document number, file path, document type, date, and any notes/cost
columns you add to `dbo.Chatbot_GetFileLinks`.

## Configuration

By default for `adminmodeluniversitiy`, the chatbot expects a connection string named `generalUniversityDB`
and these stored procedures:

- `dbo.Chatbot_GetFileLinks @SearchText`
- `dbo.Chatbot_GetInvoiceExpenseCodes @InvoiceNumber`
- `dbo.Chatbot_GetPersonExpenses @PersonName`

You can override the connection string name, stored procedure names, parameter names, command timeout,
max displayed rows, and ML.NET intent confidence threshold through `appSettings` in `Web.config`.

## Troubleshooting the red error message

If the page shows:

```text
I could not complete that query. Please check the chatbot configuration or contact support.
```

or a `Chatbot database error`, the natural-language part already worked, but the database call failed.
Check these items:

1. `Web.config` has a real connection string for your expenses database.

   ```xml
   <connectionStrings>
     <add name="generalUniversityDB"
          connectionString="Data Source=DESKTOP-2OC4RDU;Initial Catalog=UniversityDBiap;Integrated Security=True;TrustServerCertificate=True;"
          providerName="System.Data.SqlClient" />
   </connectionStrings>
   ```

2. The chatbot app setting points to that connection string.

   ```xml
   <add key="ExpenseChatbot.ConnectionStringName" value="generalUniversityDB" />
   ```

3. The stored procedure for the selected intent exists in the same database.

   For `find file links for receipt.pdf`, the code calls:

   ```sql
   dbo.Chatbot_GetFileLinks @SearchText = 'receipt.pdf'
   ```

4. The application database user has permission to execute the stored procedure.

   ```sql
   GRANT EXECUTE ON dbo.Chatbot_GetFileLinks TO [YourAppUser];
   ```

During local setup only, you can temporarily enable detailed errors:

```xml
<add key="ExpenseChatbot.ShowDetailedErrors" value="true" />
```

Set it back to `false` before production use.

## Security notes

- User input is sent to SQL Server as parameters, not string-concatenated SQL.
- Result text is HTML-encoded by WebForms controls.
- Columns whose names look like links or URLs are rendered as clickable links only for safe relative,
  `http`, or `https` values.
- Prefer granting the application login `EXECUTE` permission on the chatbot procedures instead of direct
  table access.
