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
        f.InvoiceNumber,
        f.ExpenseCode,
        f.FileName,
        f.FileUrl,
        f.UploadedOn
    FROM dbo.ExpenseFiles AS f
    WHERE f.InvoiceNumber LIKE '%' + @SearchText + '%'
       OR f.ExpenseCode LIKE '%' + @SearchText + '%'
       OR f.FileName LIKE '%' + @SearchText + '%'
    ORDER BY f.UploadedOn DESC;
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
