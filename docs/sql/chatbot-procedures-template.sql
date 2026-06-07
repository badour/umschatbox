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

CREATE OR ALTER PROCEDURE dbo.Chatbot_GetExpenseNetValue
    @SearchText NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NormalizedSearch NVARCHAR(200) = LOWER(LTRIM(RTRIM(ISNULL(@SearchText, N''))));
    DECLARE @CurrentYear INT = YEAR(GETDATE());
    DECLARE @StartYear INT = NULL;
    DECLARE @StartDate DATE;
    DECLARE @EndDate DATE = CAST(GETDATE() AS DATE);
    DECLARE @Years INT = NULL;

    -- Supports English/Arabic phrases like:
    -- last 3 years
    -- from 2023
    -- اخر 3 سنوات
    -- صافي المصروفات من 2023 حتى الآن
    IF PATINDEX('%last [0-9]% year%', @NormalizedSearch) > 0
    BEGIN
        SET @Years = TRY_CONVERT(INT, SUBSTRING(
            @NormalizedSearch,
            PATINDEX('%last [0-9]%', @NormalizedSearch) + LEN('last '),
            2));
    END;

    IF @Years IS NULL AND PATINDEX(N'%اخر [0-9]%', @NormalizedSearch) > 0
    BEGIN
        SET @Years = TRY_CONVERT(INT, SUBSTRING(
            @NormalizedSearch,
            PATINDEX(N'%اخر [0-9]%', @NormalizedSearch) + LEN(N'اخر '),
            2));
    END;

    IF @Years IS NULL AND PATINDEX(N'%آخر [0-9]%', @NormalizedSearch) > 0
    BEGIN
        SET @Years = TRY_CONVERT(INT, SUBSTRING(
            @NormalizedSearch,
            PATINDEX(N'%آخر [0-9]%', @NormalizedSearch) + LEN(N'آخر '),
            2));
    END;

    IF @Years IS NOT NULL AND @Years > 0
    BEGIN
        -- Example in 2026: last 3 years => from 2023-01-01 until today.
        SET @StartYear = @CurrentYear - @Years;
    END;

    IF @StartYear IS NULL
    BEGIN
        SELECT @StartYear = TRY_CONVERT(INT, value)
        FROM STRING_SPLIT(REPLACE(REPLACE(@NormalizedSearch, '-', ' '), '/', ' '), ' ')
        WHERE TRY_CONVERT(INT, value) BETWEEN 1900 AND 2099;
    END;

    IF @StartYear IS NULL
    BEGIN
        -- Default behavior for a generic question like "total expenses".
        SET @StartYear = @CurrentYear - 3;
    END;

    SET @StartDate = DATEFROMPARTS(@StartYear, 1, 1);

    /*
        Replace these sample table/column names with your real accounting schema.

        Expected logic:
        - Expense accounts start with account code 3.
        - Use transaction date between @StartDate and today.
        - Net expense value usually equals SUM(Debit - Credit).

        Example columns used below:
        dbo.AccountTransactions:
            TransactionDate, AccountCode, DebitAmount, CreditAmount
        dbo.Accounts:
            AccountCode, AccountName
    */
    SELECT
        @StartDate AS PeriodStart,
        @EndDate AS PeriodEnd,
        N'3' AS ExpenseAccountPrefix,
        COUNT_BIG(*) AS TransactionCount,
        SUM(ISNULL(t.DebitAmount, 0) - ISNULL(t.CreditAmount, 0)) AS NetExpenseValue
    FROM dbo.AccountTransactions AS t
    INNER JOIN dbo.Accounts AS a
        ON a.AccountCode = t.AccountCode
    WHERE t.AccountCode LIKE '3%'
      AND t.TransactionDate >= @StartDate
      AND t.TransactionDate < DATEADD(DAY, 1, @EndDate);
END;
GO

CREATE OR ALTER PROCEDURE dbo.Chatbot_GetFixedAssetsByAccount
    @SearchText NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NormalizedSearch NVARCHAR(200) = LOWER(LTRIM(RTRIM(ISNULL(@SearchText, N''))));
    DECLARE @CurrentYear INT = YEAR(GETDATE());
    DECLARE @StartYear INT = NULL;
    DECLARE @StartDate DATE;
    DECLARE @EndDate DATE = CAST(GETDATE() AS DATE);
    DECLARE @Years INT = NULL;

    -- Supports: last 3 years / اخر 3 سنوات.
    IF PATINDEX('%last [0-9]% year%', @NormalizedSearch) > 0
    BEGIN
        SET @Years = TRY_CONVERT(INT, SUBSTRING(
            @NormalizedSearch,
            PATINDEX('%last [0-9]%', @NormalizedSearch) + LEN('last '),
            2));
    END;

    IF @Years IS NULL AND PATINDEX(N'%اخر [0-9]%', @NormalizedSearch) > 0
    BEGIN
        SET @Years = TRY_CONVERT(INT, SUBSTRING(
            @NormalizedSearch,
            PATINDEX(N'%اخر [0-9]%', @NormalizedSearch) + LEN(N'اخر '),
            2));
    END;

    IF @Years IS NULL AND PATINDEX(N'%آخر [0-9]%', @NormalizedSearch) > 0
    BEGIN
        SET @Years = TRY_CONVERT(INT, SUBSTRING(
            @NormalizedSearch,
            PATINDEX(N'%آخر [0-9]%', @NormalizedSearch) + LEN(N'آخر '),
            2));
    END;

    IF @Years IS NOT NULL AND @Years > 0
    BEGIN
        SET @StartYear = @CurrentYear - @Years;
    END;

    IF @StartYear IS NULL
    BEGIN
        SELECT @StartYear = TRY_CONVERT(INT, value)
        FROM STRING_SPLIT(REPLACE(REPLACE(@NormalizedSearch, '-', ' '), '/', ' '), ' ')
        WHERE TRY_CONVERT(INT, value) BETWEEN 1900 AND 2099;
    END;

    IF @StartYear IS NULL
    BEGIN
        SET @StartYear = @CurrentYear - 3;
    END;

    SET @StartDate = DATEFROMPARTS(@StartYear, 1, 1);

    /*
        Replace these sample table/column names with your real accounting schema.

        Expected analysis:
        - Fixed assets only.
        - Group by tertiary account. In this template, tertiary account = first 3 digits of AccountCode.
        - Net value usually equals SUM(Debit - Credit).

        Example columns used below:
        dbo.AccountTransactions:
            TransactionDate, AccountCode, DebitAmount, CreditAmount
        dbo.Accounts:
            AccountCode, AccountName, AccountType
    */
    SELECT
        @StartDate AS PeriodStart,
        @EndDate AS PeriodEnd,
        LEFT(t.AccountCode, 3) AS TertiaryAccountCode,
        MAX(a.AccountName) AS AccountName,
        COUNT_BIG(*) AS TransactionCount,
        SUM(ISNULL(t.DebitAmount, 0)) AS TotalDebit,
        SUM(ISNULL(t.CreditAmount, 0)) AS TotalCredit,
        SUM(ISNULL(t.DebitAmount, 0) - ISNULL(t.CreditAmount, 0)) AS NetFixedAssetsValue
    FROM dbo.AccountTransactions AS t
    INNER JOIN dbo.Accounts AS a
        ON a.AccountCode = t.AccountCode
    WHERE t.TransactionDate >= @StartDate
      AND t.TransactionDate < DATEADD(DAY, 1, @EndDate)
      AND (
            -- Best option: use your real fixed-asset account flag/type.
            a.AccountType = N'FixedAsset'
            -- Or replace with the fixed-assets account prefix used in your chart of accounts.
            OR t.AccountCode LIKE '12%'
      )
    GROUP BY LEFT(t.AccountCode, 3)
    ORDER BY TertiaryAccountCode;
END;
GO

CREATE OR ALTER PROCEDURE dbo.Chatbot_GetPersonPaymentTotal
    @SearchText NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @PersonName NVARCHAR(200) = LTRIM(RTRIM(ISNULL(@SearchText, N'')));

    /*
        Replace these sample table/column names with your real payment/expense schema.

        Expected analysis:
        - Match the target person/payee by display name.
        - Return count, total debit/payment amount, and first/last payment date.

        Example columns used below:
        dbo.PaymentTransactions:
            PaymentDate, PayeeName, Amount, AccountCode, Description
    */
    SELECT
        @PersonName AS SearchName,
        COUNT_BIG(*) AS PaymentCount,
        MIN(p.PaymentDate) AS FirstPaymentDate,
        MAX(p.PaymentDate) AS LastPaymentDate,
        SUM(ISNULL(p.Amount, 0)) AS TotalPaidAmount
    FROM dbo.PaymentTransactions AS p
    WHERE p.PayeeName LIKE N'%' + @PersonName + N'%';
END;
GO
