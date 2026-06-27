# ExpencesAccDocSum Accounting Chatbot

This chatbot is a single-prompt ASP.NET WebForms assistant for `adminmodeluniversitiy`.
It answers natural-language Arabic/English accounting questions by querying only:

```text
dbo.ExpencesAccDocSum
```

Relevant database columns:

- `date`
- `DocTitl`
- `DocDetails`
- `DebetValue`
- `CreditValue`
- `FromAccountID`
- `FromAccountName`
- `ToAccountName`
- `AddedBy`
- `YearName`
- `DepartmentName`

The UI has no query-type buttons. The user types a question, the bot classifies it, runs the matching
stored procedure, then returns a text answer with reasoning.

For account lookup:

```text
Account code search -> FromAccountID
Account name search -> ToAccountName
Description/details search -> DocDetails and DocTitl
```

## Supported questions

### 1. Expenses related to a specific accounting account/category

Example:

```text
المصاريف المتعلقة بالصيانة السيارات 3314
```

Intent:

```text
AccountRelatedExpenses
```

Procedure:

```sql
dbo.Chatbot_GetAccountRelatedExpenses @SearchText = '3314'
```

Logic:

```text
If the user writes an account code, search `FromAccountID`.
If the user writes an account name, search `ToAccountName`.
If the user writes descriptive text, also search `DocDetails` and `DocTitl`.
Then calculate total, net, average, minimum, maximum, and square-root values from `DebetValue` and `CreditValue`.
```

### 2. Total fixed assets

Example:

```text
المجموع الكلي للموجودات الثابتة للكلية لكل السنوات
```

Intent:

```text
FixedAssetsTotal
```

Procedure:

```sql
dbo.Chatbot_GetFixedAssetsTotal @SearchText = 'all years'
```

Logic:

```text
Use rows where FromAccountID starts with 1.
Sum DebetValue and CreditValue.
```

### 3. Total buildings

Example:

```text
المجموع الكلي للمباني للكلية لكل الاعوام
```

Intent:

```text
BuildingsTotal
```

Procedure:

```sql
dbo.Chatbot_GetBuildingsTotal @SearchText = 'all years'
```

Logic:

```text
Use rows where FromAccountID starts with 112.
Sum DebetValue and CreditValue.
```

### 4. Total expenses

Examples:

```text
المجموع الكلي للمصروفات لتبويب محاسبي 3314
المجموع الكلي للمصروفات لشخص احمد
```

Intent:

```text
AccountingExpensesTotal
```

Procedure:

```sql
dbo.Chatbot_GetAccountingExpensesTotal @SearchText = '3314'
dbo.Chatbot_GetAccountingExpensesTotal @SearchText = 'احمد'
```

Logic:

```text
All expenses: `FromAccountID` starts with `3`.
Specific account code: `FromAccountID` starts with the extracted account code.
Specific account name: search `ToAccountName`.
Specific descriptive text/person: search `DocTitl`, `DocDetails`, `AddedBy`, and `DepartmentName`.
Then calculate total, net, average, minimum, maximum, and square-root values from `DebetValue` and `CreditValue`.
```

## Files

- `ChatBot/ExpensesChatBot.aspx` - single-prompt chatbot page.
- `ChatBot/ExpensesChatBot.aspx.cs` - request handling and result binding.
- `App_Code/ExpenseChatbotNaturalLanguage.cs` - Arabic/English intent classification and value extraction.
- `App_Code/ExpenseChatbotQueryDefinition.cs` - intent-to-stored-procedure mapping.
- `App_Code/ExpenseChatbotRepository.cs` - SQL execution.
- `App_Code/ExpenseChatbotService.cs` - chatbot orchestration.
- `App_Code/ExpenseChatbotReasoningEngine.cs` - text answer and analytics reasoning.
- `docs/Web.config.chatbot.example.config` - Web.config keys.
- `docs/sql/chatbot-procedures-template.sql` - SQL procedure templates.

## Web.config keys

```xml
<add key="ExpenseChatbot.AccountRelatedExpenses.StoredProcedure" value="dbo.Chatbot_GetAccountRelatedExpenses" />
<add key="ExpenseChatbot.AccountRelatedExpenses.ParameterName" value="@SearchText" />

<add key="ExpenseChatbot.FixedAssetsTotal.StoredProcedure" value="dbo.Chatbot_GetFixedAssetsTotal" />
<add key="ExpenseChatbot.FixedAssetsTotal.ParameterName" value="@SearchText" />

<add key="ExpenseChatbot.BuildingsTotal.StoredProcedure" value="dbo.Chatbot_GetBuildingsTotal" />
<add key="ExpenseChatbot.BuildingsTotal.ParameterName" value="@SearchText" />

<add key="ExpenseChatbot.AccountingExpensesTotal.StoredProcedure" value="dbo.Chatbot_GetAccountingExpensesTotal" />
<add key="ExpenseChatbot.AccountingExpensesTotal.ParameterName" value="@SearchText" />
```

## Response behavior

- If the question has no time period, the bot returns the main total/net number.
- If the question includes a period or year, the bot gives a detailed text explanation.
- Responses are text-only; the page does not render a GridView/table in this version.

## Notes

- The current scope intentionally excludes file URL search, invoice-code lookup, student revenue, and unrelated tables.
- All accounting procedures in this version are based on `dbo.ExpencesAccDocSum`.
