/*
    Expenses Chatbot stored procedure templates

    Replace the table and column names below with the names from your expenses database.
    The WebForms page calls these procedures with parameterized ADO.NET commands.
*/

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
        DECLARE @YearPosition INT = PATINDEX('%[12][0-9][0-9][0-9]%', @NormalizedSearch);

        IF @YearPosition > 0
        BEGIN
            SET @StartYear = TRY_CONVERT(INT, SUBSTRING(@NormalizedSearch, @YearPosition, 4));
        END;
    END;

    IF @StartYear IS NULL
    BEGIN
        -- Default behavior for a generic question like "total expenses".
        SET @StartYear = @CurrentYear - 3;
    END;

    SET @StartDate = DATEFROMPARTS(@StartYear, 1, 1);

    /*
        Uses dbo.ExpencesAccDocSum:
        - Expenses start with FromAccountID = 3.
        - Values are in DebetValue or CreditValue.
    */
    SELECT
        @StartDate AS PeriodStart,
        @EndDate AS PeriodEnd,
        N'3' AS ExpenseAccountPrefix,
        COUNT_BIG(*) AS TransactionCount,
        SUM(ISNULL(DebetValue, 0)) AS TotalDebetValue,
        SUM(ISNULL(CreditValue, 0)) AS TotalCreditValue,
        SUM(ISNULL(DebetValue, 0) + ISNULL(CreditValue, 0)) AS TotalExpensesValue,
        SUM(ISNULL(DebetValue, 0) - ISNULL(CreditValue, 0)) AS NetExpenseValue
    FROM dbo.ExpencesAccDocSum
    WHERE FromAccountID LIKE N'3%'
      AND [date] >= @StartDate
      AND [date] < DATEADD(DAY, 1, @EndDate);
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
        DECLARE @YearPosition INT = PATINDEX('%[12][0-9][0-9][0-9]%', @NormalizedSearch);

        IF @YearPosition > 0
        BEGIN
            SET @StartYear = TRY_CONVERT(INT, SUBSTRING(@NormalizedSearch, @YearPosition, 4));
        END;
    END;

    IF @StartYear IS NULL
    BEGIN
        SET @StartYear = @CurrentYear - 3;
    END;

    SET @StartDate = DATEFROMPARTS(@StartYear, 1, 1);

    /*
        Expected analysis:
        - Fixed assets only.
        - Group by tertiary account. In this template, tertiary account = first 3 digits of FromAccountID.
        - Values are in DebetValue or CreditValue.
    */
    SELECT
        @StartDate AS PeriodStart,
        @EndDate AS PeriodEnd,
        LEFT(FromAccountID, 3) AS TertiaryAccountCode,
        MAX(FromAccountName) AS AccountName,
        COUNT_BIG(*) AS TransactionCount,
        SUM(ISNULL(DebetValue, 0)) AS TotalDebetValue,
        SUM(ISNULL(CreditValue, 0)) AS TotalCreditValue,
        SUM(ISNULL(DebetValue, 0) + ISNULL(CreditValue, 0)) AS TotalFixedAssetsValue,
        SUM(ISNULL(DebetValue, 0) - ISNULL(CreditValue, 0)) AS NetFixedAssetsValue
    FROM dbo.ExpencesAccDocSum
    WHERE [date] >= @StartDate
      AND [date] < DATEADD(DAY, 1, @EndDate)
      AND FromAccountID LIKE N'1%'
    GROUP BY LEFT(FromAccountID, 3)
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
        Uses dbo.ExpencesAccDocSum:
        - Match the target person/name in FromAccountName, ToAccountName, DocTitl,
          DocDetails, AddedBy, or DepartmentName.
        - Expense rows use FromAccountID starting with 3.
        - Values are in DebetValue or CreditValue.
    */
    SELECT
        @PersonName AS SearchName,
        COUNT_BIG(*) AS PaymentCount,
        MIN([date]) AS FirstPaymentDate,
        MAX([date]) AS LastPaymentDate,
        SUM(ISNULL(DebetValue, 0)) AS TotalDebetValue,
        SUM(ISNULL(CreditValue, 0)) AS TotalCreditValue,
        SUM(ISNULL(DebetValue, 0) + ISNULL(CreditValue, 0)) AS TotalPaidAmount,
        SUM(ISNULL(DebetValue, 0) - ISNULL(CreditValue, 0)) AS NetPaidAmount
    FROM dbo.ExpencesAccDocSum
    WHERE FromAccountID LIKE N'3%'
      AND (
            FromAccountName LIKE N'%' + @PersonName + N'%'
            OR ToAccountName LIKE N'%' + @PersonName + N'%'
            OR DocTitl LIKE N'%' + @PersonName + N'%'
            OR CAST(DocDetails AS NVARCHAR(MAX)) LIKE N'%' + @PersonName + N'%'
            OR AddedBy LIKE N'%' + @PersonName + N'%'
            OR DepartmentName LIKE N'%' + @PersonName + N'%'
      );
END;
GO

CREATE OR ALTER PROCEDURE dbo.Chatbot_GetStudentRevenueSummary
    @SearchText NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    /*
        Student revenue analytics for questions like:
        ما هي مجموع الايرادات الطلبة لكل الاعوام الدراسية؟

        Required table/column from adminmodeluniversitiy:
        dbo.ReceiptDocTb.InputValue

        This default version returns one total across all rows.
        If ReceiptDocTb has an academic-year column, uncomment the grouped query below
        and replace AcademicYear with the real column name.
    */
    SELECT
        COUNT_BIG(*) AS ReceiptCount,
        SUM(ISNULL(InputValue, 0)) AS TotalStudentRevenue,
        AVG(CAST(ISNULL(InputValue, 0) AS DECIMAL(18, 2))) AS AverageReceiptValue,
        MIN(ISNULL(InputValue, 0)) AS MinimumReceiptValue,
        MAX(ISNULL(InputValue, 0)) AS MaximumReceiptValue
    FROM dbo.ReceiptDocTb;

    /*
    SELECT
        AcademicYear,
        COUNT_BIG(*) AS ReceiptCount,
        SUM(ISNULL(InputValue, 0)) AS TotalStudentRevenue,
        AVG(CAST(ISNULL(InputValue, 0) AS DECIMAL(18, 2))) AS AverageReceiptValue,
        MIN(ISNULL(InputValue, 0)) AS MinimumReceiptValue,
        MAX(ISNULL(InputValue, 0)) AS MaximumReceiptValue
    FROM dbo.ReceiptDocTb
    GROUP BY AcademicYear
    ORDER BY AcademicYear;
    */
END;
GO

CREATE OR ALTER PROCEDURE dbo.Chatbot_GetAccountRelatedExpenses
    @SearchText NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Search NVARCHAR(200) = LTRIM(RTRIM(ISNULL(@SearchText, N'')));

    /*
        Uses dbo.ExpencesAccDocSum:
        - Accounting category/code: FromAccountID
        - Accounting category/name: FromAccountName
        - Values: DebetValue or CreditValue

        Example:
        المصاريف المتعلقة بالصيانة السيارات 3314
        -> @SearchText should usually be 3314
    */
    SELECT
        MAX(FromAccountID) AS FromAccountID,
        MAX(FromAccountName) AS FromAccountName,
        COUNT_BIG(*) AS TransactionCount,
        MIN([date]) AS PeriodStart,
        MAX([date]) AS PeriodEnd,
        SUM(ISNULL(DebetValue, 0)) AS TotalDebetValue,
        SUM(ISNULL(CreditValue, 0)) AS TotalCreditValue,
        SUM(ISNULL(DebetValue, 0) + ISNULL(CreditValue, 0)) AS TotalAccountingValue,
        SUM(ISNULL(DebetValue, 0) - ISNULL(CreditValue, 0)) AS NetAccountingValue
    FROM dbo.ExpencesAccDocSum
    WHERE FromAccountID LIKE @Search + N'%'
       OR FromAccountName LIKE N'%' + @Search + N'%'
       OR DocTitl LIKE N'%' + @Search + N'%'
       OR CAST(DocDetails AS NVARCHAR(MAX)) LIKE N'%' + @Search + N'%';
END;
GO

CREATE OR ALTER PROCEDURE dbo.Chatbot_GetFixedAssetsTotal
    @SearchText NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NormalizedSearch NVARCHAR(200) = LOWER(LTRIM(RTRIM(ISNULL(@SearchText, N''))));
    DECLARE @StartYear INT = NULL;
    DECLARE @StartDate DATE = NULL;
    DECLARE @EndDate DATE = CAST(GETDATE() AS DATE);
    DECLARE @YearPosition INT = PATINDEX('%[12][0-9][0-9][0-9]%', @NormalizedSearch);

    IF @YearPosition > 0
    BEGIN
        SET @StartYear = TRY_CONVERT(INT, SUBSTRING(@NormalizedSearch, @YearPosition, 4));
        SET @StartDate = DATEFROMPARTS(@StartYear, 1, 1);
    END;

    SELECT
        N'1' AS AccountPrefix,
        N'Fixed assets / الموجودات الثابتة' AS AnalysisType,
        COUNT_BIG(*) AS TransactionCount,
        MIN([date]) AS PeriodStart,
        MAX([date]) AS PeriodEnd,
        SUM(ISNULL(DebetValue, 0)) AS TotalDebetValue,
        SUM(ISNULL(CreditValue, 0)) AS TotalCreditValue,
        SUM(ISNULL(DebetValue, 0) + ISNULL(CreditValue, 0)) AS TotalFixedAssetsValue,
        SUM(ISNULL(DebetValue, 0) - ISNULL(CreditValue, 0)) AS NetFixedAssetsValue
    FROM dbo.ExpencesAccDocSum
    WHERE FromAccountID LIKE N'1%'
      AND (@StartDate IS NULL OR ([date] >= @StartDate AND [date] < DATEADD(DAY, 1, @EndDate)));
END;
GO

CREATE OR ALTER PROCEDURE dbo.Chatbot_GetBuildingsTotal
    @SearchText NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NormalizedSearch NVARCHAR(200) = LOWER(LTRIM(RTRIM(ISNULL(@SearchText, N''))));
    DECLARE @StartYear INT = NULL;
    DECLARE @StartDate DATE = NULL;
    DECLARE @EndDate DATE = CAST(GETDATE() AS DATE);
    DECLARE @YearPosition INT = PATINDEX('%[12][0-9][0-9][0-9]%', @NormalizedSearch);

    IF @YearPosition > 0
    BEGIN
        SET @StartYear = TRY_CONVERT(INT, SUBSTRING(@NormalizedSearch, @YearPosition, 4));
        SET @StartDate = DATEFROMPARTS(@StartYear, 1, 1);
    END;

    SELECT
        N'112' AS AccountPrefix,
        N'Buildings / المباني' AS AnalysisType,
        COUNT_BIG(*) AS TransactionCount,
        MIN([date]) AS PeriodStart,
        MAX([date]) AS PeriodEnd,
        SUM(ISNULL(DebetValue, 0)) AS TotalDebetValue,
        SUM(ISNULL(CreditValue, 0)) AS TotalCreditValue,
        SUM(ISNULL(DebetValue, 0) + ISNULL(CreditValue, 0)) AS TotalBuildingsValue,
        SUM(ISNULL(DebetValue, 0) - ISNULL(CreditValue, 0)) AS NetBuildingsValue
    FROM dbo.ExpencesAccDocSum
    WHERE FromAccountID LIKE N'112%'
      AND (@StartDate IS NULL OR ([date] >= @StartDate AND [date] < DATEADD(DAY, 1, @EndDate)));
END;
GO

CREATE OR ALTER PROCEDURE dbo.Chatbot_GetAccountingExpensesTotal
    @SearchText NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Search NVARCHAR(200) = LTRIM(RTRIM(ISNULL(@SearchText, N'')));
    DECLARE @NormalizedSearch NVARCHAR(200) = LOWER(@Search);
    DECLARE @AccountCode NVARCHAR(50) = NULL;
    DECLARE @StartDate DATE = NULL;
    DECLARE @EndDate DATE = CAST(GETDATE() AS DATE);
    DECLARE @YearPosition INT = PATINDEX('%[12][0-9][0-9][0-9]%', @NormalizedSearch);
    DECLARE @CodePosition INT = PATINDEX('%[0-9][0-9][0-9]%', @Search);

    IF @YearPosition > 0
    BEGIN
        SET @StartDate = DATEFROMPARTS(TRY_CONVERT(INT, SUBSTRING(@NormalizedSearch, @YearPosition, 4)), 1, 1);
    END;

    IF @CodePosition > 0
    BEGIN
        SET @AccountCode = SUBSTRING(@Search, @CodePosition, 10);
        SET @AccountCode = LEFT(@AccountCode, PATINDEX('%[^0-9]%', @AccountCode + N'X') - 1);
    END;

    /*
        Rules:
        - All expenses: FromAccountID starts with 3.
        - Specific accounting category: FromAccountID starts with the extracted/provided code.
        - Specific person/name: match name text in FromAccountName, ToAccountName, DocTitl, DocDetails, AddedBy, DepartmentName.
    */
    SELECT
        COALESCE(@AccountCode, N'3') AS AccountPrefix,
        @Search AS SearchText,
        COUNT_BIG(*) AS TransactionCount,
        MIN([date]) AS PeriodStart,
        MAX([date]) AS PeriodEnd,
        SUM(ISNULL(DebetValue, 0)) AS TotalDebetValue,
        SUM(ISNULL(CreditValue, 0)) AS TotalCreditValue,
        SUM(ISNULL(DebetValue, 0) + ISNULL(CreditValue, 0)) AS TotalExpensesValue,
        SUM(ISNULL(DebetValue, 0) - ISNULL(CreditValue, 0)) AS NetExpensesValue
    FROM dbo.ExpencesAccDocSum
    WHERE FromAccountID LIKE COALESCE(@AccountCode, N'3') + N'%'
      AND (@StartDate IS NULL OR ([date] >= @StartDate AND [date] < DATEADD(DAY, 1, @EndDate)))
      AND (
            @AccountCode IS NOT NULL
            OR @Search IN (N'', N'all years', N'كل السنوات', N'لكل السنوات')
            OR FromAccountName LIKE N'%' + @Search + N'%'
            OR ToAccountName LIKE N'%' + @Search + N'%'
            OR DocTitl LIKE N'%' + @Search + N'%'
            OR CAST(DocDetails AS NVARCHAR(MAX)) LIKE N'%' + @Search + N'%'
            OR AddedBy LIKE N'%' + @Search + N'%'
            OR DepartmentName LIKE N'%' + @Search + N'%'
      );
END;
GO
