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

    DECLARE @NormalizedSearch NVARCHAR(200) = LTRIM(RTRIM(@SearchText));
    DECLARE @DocumentNumber NVARCHAR(50) = NULL;
    DECLARE @DocumentYear NVARCHAR(50) = NULL;
    DECLARE @YearMarker INT = CHARINDEX(N'لسنة', @NormalizedSearch);

    -- Supports Arabic input like: 42 لسنة 2024-2025
    IF @YearMarker > 0
    BEGIN
        SET @DocumentNumber = LTRIM(RTRIM(LEFT(@NormalizedSearch, @YearMarker - 1)));
        SET @DocumentYear = LTRIM(RTRIM(SUBSTRING(@NormalizedSearch, @YearMarker + LEN(N'لسنة'), 50)));
    END;

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
    WHERE CAST(f.DocNum AS NVARCHAR(200)) LIKE '%' + @NormalizedSearch + '%'
       OR CONVERT(NVARCHAR(30), f.[date], 23) LIKE '%' + @NormalizedSearch + '%'
       OR f.FilePath LIKE '%' + @NormalizedSearch + '%'
       OR f.DocType LIKE '%' + @NormalizedSearch + '%'
       OR (
            @DocumentNumber IS NOT NULL
            AND @DocumentYear IS NOT NULL
            AND CAST(f.DocNum AS NVARCHAR(200)) = @DocumentNumber
            AND (
                -- Best option: uncomment and rename if your table has an academic/fiscal year column.
                -- f.AcademicYear = @DocumentYear
                -- OR
                f.FilePath LIKE '%' + @DocumentYear + '%'
                OR f.DocType LIKE '%' + @DocumentYear + '%'
                OR CONVERT(NVARCHAR(30), f.[date], 23) LIKE LEFT(@DocumentYear, 4) + '%'
            )
       )
       -- Uncomment and rename these columns if your table has them:
       -- OR f.FileName LIKE '%' + @NormalizedSearch + '%'
       -- OR f.FileNotes LIKE '%' + @NormalizedSearch + '%'
       -- OR CAST(f.TotalCost AS NVARCHAR(200)) LIKE '%' + @NormalizedSearch + '%'
       -- OR f.AcademicYear LIKE '%' + @NormalizedSearch + '%'
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
