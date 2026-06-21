# Expenses WebForms Chatbot

This repository contains a drop-in ASP.NET WebForms chatbot module for an expenses application.
Users type natural-language questions into one ChatGPT-style prompt. The page does not require
query-type buttons; the chatbot classifies the question, runs the matching SQL procedure, analyzes the
returned data, and explains the result in text.

Supported examples include:

- student revenue analytics;
- expenses related to a specific accounting account/category;
- total fixed assets using accounting prefix `1`;
- total buildings using accounting prefix `112`;
- total expenses using accounting prefix `3`, a person name, or a specific accounting code;
- accounting and student-revenue summaries.

The module uses ML.NET to classify the user's intent, simple extraction rules to pull out the search
value, parameterized SQL stored procedure calls through ADO.NET to query the database, and a reasoning
layer that summarizes the returned rows before displaying the table.

Examples:

```text
User: ما هي مجموع الايرادات الطلبة لكل الاعوام الدراسية؟
Intent detected: StudentRevenueSummary
Search value extracted by rules: all academic years
SQL procedure called: dbo.Chatbot_GetStudentRevenueSummary @SearchText = 'all academic years'
```

```text
User: show me expenses for Ahmed
Intent detected: AccountingExpensesTotal
Search value extracted by rules: Ahmed
SQL procedure called: dbo.Chatbot_GetAccountingExpensesTotal @SearchText = 'Ahmed'
```

```text
User: how many expenses for last 3 years
Intent detected: ExpenseNetValue
Search value extracted by rules: last 3 years
SQL procedure called: dbo.Chatbot_GetExpenseNetValue @SearchText = 'last 3 years'
```

## Files

- `ChatBot/ExpensesChatBot.aspx` - WebForms chatbot page and UI.
- `ChatBot/ExpensesChatBot.aspx.cs` - page event handlers, natural-language request handling, and result binding.
- `App_Code/ExpenseChatbot*.cs` - query definitions, configuration, SQL repository, and service logic.
- `App_Code/ExpenseChatbotNaturalLanguage.cs` - ML.NET intent classifier and natural-language value extraction.
- `App_Code/ExpenseChatbotReasoningEngine.cs` - post-query analysis for totals, averages, trends, categories, and anomalies.
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
   - The accounting templates are based on `ExpencesAccDocSum`, `FromAccountID`, `FromAccountName`,
     `DebetValue`, and `CreditValue`.
6. Add a menu item or hyperlink in the existing app that points to `ChatBot/ExpensesChatBot.aspx`.
7. Restrict access to the chatbot page using the same authentication/authorization rules as the expenses pages.

## Natural-language examples

These examples are classified automatically:

- `ما هي مجموع الايرادات الطلبة لكل الاعوام الدراسية؟` -> `StudentRevenueSummary`, search value `all academic years`
- `المصاريف المتعلقة بالصيانة السيارات 3314` -> `AccountRelatedExpenses`, search value `3314`
- `المجموع الكلي للموجودات الثابتة للكلية لكل السنوات` -> `FixedAssetsTotal`, search value `all years`
- `المجموع الكلي للمباني للكلية لكل الاعوام` -> `BuildingsTotal`, search value `all years`
- `المجموع الكلي للمصروفات لتبويب محاسبي 3314` -> `AccountingExpensesTotal`, search value `3314`
- `المجموع الكلي للمصروفات لشخص احمد` -> `AccountingExpensesTotal`, search value `احمد`
- `how many expenses for last 3 years` -> `ExpenseNetValue`, search value `last 3 years`
- `total expenses from 2023 till now` -> `ExpenseNetValue`, search value `from 2023`
- `صافي المصروفات من 2023 حتى الآن` -> `ExpenseNetValue`, search value `from 2023`
- `اريد المصاريف الكلية للسنوات الثلاثة الاخيرة` -> `ExpenseNetValue`, search value `last 3 years`
- `مجموع الموجودات الثابته مفصلة حسب الحسابات الثلاثير لمدة اخر 3 سنوات` -> `FixedAssetsByAccount`, search value `last 3 years`
- `ما هي القيمة الكلية للمبالغ المصروفة الى السيح حسين حيدر` -> `PersonPaymentTotal`, search value `حسين حيدر`

The page no longer uses query-type buttons. If a user types only a direct value, such as `3314`,
the service treats it as an accounting-expense/category fallback.

For the `adminmodeluniversitiy` app, the accounting analytics use `dbo.ExpencesAccDocSum`.
The accounting category/code is read from `FromAccountID`, the account name from `FromAccountName`,
and values are calculated from `DebetValue` and `CreditValue`.

For account-related expense questions like `المصاريف المتعلقة بالصيانة السيارات 3314`, the chatbot
runs `dbo.Chatbot_GetAccountRelatedExpenses` and filters `FromAccountID`/`FromAccountName`.

For fixed-assets questions, the chatbot runs `dbo.Chatbot_GetFixedAssetsTotal` and sums rows whose
`FromAccountID` starts with `1`.

For building questions, the chatbot runs `dbo.Chatbot_GetBuildingsTotal` and sums rows whose
`FromAccountID` starts with `112`.

For total expense questions, the chatbot runs `dbo.Chatbot_GetAccountingExpensesTotal`. It uses
`FromAccountID LIKE '3%'` for all expenses, a specific code such as `3314` when provided, or text
matching in account/person/document fields when a person/name is included.

For accounting summary questions like `how many expenses for last 3 years`, the chatbot runs
`dbo.Chatbot_GetExpenseNetValue`. The SQL template calculates net expense value for accounts whose
account code starts with `3` and uses a period from January 1 of the calculated start year until today.
For example, in 2026, `last 3 years` starts from `2023-01-01`.

For fixed-assets analysis questions like `مجموع الموجودات الثابته مفصلة حسب الحسابات الثلاثير لمدة اخر 3 سنوات`,
the chatbot runs `dbo.Chatbot_GetFixedAssetsByAccount`. The SQL template groups fixed assets by tertiary
account code and returns debit, credit, net value, and transaction count.

For person payment/expense analysis questions like `ما هي القيمة الكلية للمبالغ المصروفة الى السيح حسين حيدر`,
the chatbot runs `dbo.Chatbot_GetPersonPaymentTotal` after extracting the name `حسين حيدر`, and the SQL
template searches `ExpencesAccDocSum` text/account fields for that person.

For student revenue questions like `ما هي مجموع الايرادات الطلبة لكل الاعوام الدراسية؟`, the chatbot
runs `dbo.Chatbot_GetStudentRevenueSummary`. The SQL template sums `ReceiptDocTb.InputValue` across
all rows by default, with a commented grouped-by-academic-year version if your table has an academic
year column.

## Reasoning layer

After a stored procedure returns data, `ExpenseChatbotReasoningEngine` inspects the `DataTable` before
responding. Natural-language answers are text-only, so the GridView stays hidden for those responses.
It can add insights such as:

- account-related expense totals by code/name;
- fixed-assets and buildings totals by accounting prefix;
- person/category expense totals from `ExpencesAccDocSum`;
- student revenue totals, receipt counts, average receipt value, and academic-year coverage when available;
- numeric totals, averages, minimums, and maximums;
- date ranges and year-over-year direction when date and amount columns are present;
- largest account/category/person by value or count;
- possible outliers when one value is much higher than the average.

This keeps the database responsible for retrieving the correct data while the C# chatbot explains what
the result appears to mean.

For analytics questions without a time period, the bot returns the main net/total number only. For
example, a student revenue question without a year range returns the total student revenue. When the
question includes a time period such as `last 3 years`, `from 2023`, `لكل الاعوام الدراسية`, or
`آخر 3 سنوات`, the bot gives a more detailed text explanation with period/category rows instead of a
single number.

## Configuration

By default for `adminmodeluniversitiy`, the chatbot expects a connection string named `generalUniversityDB`
and these stored procedures:

- `dbo.Chatbot_GetExpenseNetValue @SearchText`
- `dbo.Chatbot_GetFixedAssetsByAccount @SearchText`
- `dbo.Chatbot_GetPersonPaymentTotal @SearchText`
- `dbo.Chatbot_GetStudentRevenueSummary @SearchText`
- `dbo.Chatbot_GetAccountRelatedExpenses @SearchText`
- `dbo.Chatbot_GetFixedAssetsTotal @SearchText`
- `dbo.Chatbot_GetBuildingsTotal @SearchText`
- `dbo.Chatbot_GetAccountingExpensesTotal @SearchText`

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

   For `المصاريف المتعلقة بالصيانة السيارات 3314`, the code calls:

   ```sql
   dbo.Chatbot_GetAccountRelatedExpenses @SearchText = '3314'
   ```

4. The application database user has permission to execute the stored procedure.

   ```sql
   GRANT EXECUTE ON dbo.Chatbot_GetAccountRelatedExpenses TO [YourAppUser];
   ```

During local setup only, you can temporarily enable detailed errors:

```xml
<add key="ExpenseChatbot.ShowDetailedErrors" value="true" />
```

Set it back to `false` before production use.

## Troubleshooting WebForms compile errors

If ASP.NET shows an error like:

```text
'expenseschatbot_aspx' does not contain a definition for 'btnSend_Click'
```

the `.aspx` markup is not connected to the code-behind class that contains:

```csharp
protected void btnSend_Click(object sender, EventArgs e)
```

Use one of these setups, depending on the project type.

### Web Site project

Use `CodeFile` and make sure `Inherits` matches the code-behind class name:

```aspx
<%@ Page Language="C#" AutoEventWireup="true"
    CodeFile="ExpensesChatBot.aspx.cs"
    Inherits="ExpensesChatBot" %>
```

```csharp
public partial class ExpensesChatBot : System.Web.UI.Page
{
    protected void btnSend_Click(object sender, EventArgs e)
    {
    }
}
```

### Web Application project with namespace

Use `CodeBehind` and include the full namespace in `Inherits`:

```aspx
<%@ Page Language="C#" AutoEventWireup="true"
    CodeBehind="ExpensesChatBot.aspx.cs"
    Inherits="adminmodeluniversitiy.ExpensesChatBot" %>
```

```csharp
namespace adminmodeluniversitiy
{
    public partial class ExpensesChatBot : System.Web.UI.Page
    {
        protected void btnSend_Click(object sender, EventArgs e)
        {
        }
    }
}
```

Also confirm `ExpensesChatBot.aspx.cs` is included in the project and its Build Action is `Compile`.

## Security notes

- User input is sent to SQL Server as parameters, not string-concatenated SQL.
- Result text is HTML-encoded by WebForms controls.
- Columns whose names look like links or URLs are rendered as clickable links only for safe relative,
  `http`, or `https` values.
- Prefer granting the application login `EXECUTE` permission on the chatbot procedures instead of direct
  table access.
