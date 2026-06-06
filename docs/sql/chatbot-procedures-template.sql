/*
    Expenses Chatbot stored procedure templates

    Replace the table and column names below with the names from your expenses database.
    The WebForms page calls these procedures with parameterized ADO.NET commands.
*/

CREATE OR ALTER PROCEDURE dbo.Chatbot_GetFileLinks
    @SearchText NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (50)
        f.DocNum,
        f.[date],
        f.FilePath,
        f.DocType
        -- Add these columns if they exist in your table:
        --, f.FileName
        --, f.FileNotes
        --, f.TotalCost
    FROM dbo.UploadExpenseIncome AS f
    WHERE CAST(f.DocNum AS NVARCHAR(200)) LIKE '%' + @SearchText + '%'
       OR CONVERT(NVARCHAR(30), f.[date], 23) LIKE '%' + @SearchText + '%'
       OR f.FilePath LIKE '%' + @SearchText + '%'
       OR f.DocType LIKE '%' + @SearchText + '%'
       -- Uncomment and rename these columns if your table has them:
       -- OR f.FileName LIKE '%' + @SearchText + '%'
       -- OR f.FileNotes LIKE '%' + @SearchText + '%'
       -- OR CAST(f.TotalCost AS NVARCHAR(200)) LIKE '%' + @SearchText + '%'
    ORDER BY f.[date] DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.Chatbot_GetInvoiceExpenseCodes
    @InvoiceNumber NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (50)
        e.InvoiceNumber,
        e.ExpenseCode,
        e.ExpenseDescription,
        e.Amount,
        e.CurrencyCode,
        e.ExpenseDate,
        e.Status
    FROM dbo.Expenses AS e
    WHERE e.InvoiceNumber = @InvoiceNumber
    ORDER BY e.ExpenseDate DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.Chatbot_GetPersonExpenses
    @PersonName NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (50)
        p.DisplayName,
        p.EmployeeNumber,
        e.InvoiceNumber,
        e.ExpenseCode,
        e.ExpenseDescription,
        e.Amount,
        e.CurrencyCode,
        e.ExpenseDate,
        e.Status
    FROM dbo.Expenses AS e
    INNER JOIN dbo.People AS p
        ON p.PersonId = e.PersonId
    WHERE p.DisplayName LIKE '%' + @PersonName + '%'
       OR p.EmployeeNumber = @PersonName
       OR p.UserName = @PersonName
    ORDER BY e.ExpenseDate DESC;
END;
GO
