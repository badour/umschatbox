/*
    Accounting analytics chatbot procedures

    These procedures are scoped to the user's latest request and use only:
    dbo.ExpencesAccDocSum

    Relevant columns:
    - [date]
    - DocTitl
    - DocDetails
    - DebetValue
    - CreditValue
    - FromAccountID
    - FromAccountName
    - ToAccountName
    - AddedBy
    - YearName
    - DepartmentName
*/

CREATE OR ALTER PROCEDURE dbo.Chatbot_GetAccountRelatedExpenses
    @SearchText NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Search NVARCHAR(200) = LTRIM(RTRIM(ISNULL(@SearchText, N'')));

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
    DECLARE @StartDate DATE = NULL;
    DECLARE @EndDate DATE = CAST(GETDATE() AS DATE);
    DECLARE @YearPosition INT = PATINDEX('%[12][0-9][0-9][0-9]%', @NormalizedSearch);

    IF @YearPosition > 0
    BEGIN
        SET @StartDate = DATEFROMPARTS(TRY_CONVERT(INT, SUBSTRING(@NormalizedSearch, @YearPosition, 4)), 1, 1);
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
    DECLARE @StartDate DATE = NULL;
    DECLARE @EndDate DATE = CAST(GETDATE() AS DATE);
    DECLARE @YearPosition INT = PATINDEX('%[12][0-9][0-9][0-9]%', @NormalizedSearch);

    IF @YearPosition > 0
    BEGIN
        SET @StartDate = DATEFROMPARTS(TRY_CONVERT(INT, SUBSTRING(@NormalizedSearch, @YearPosition, 4)), 1, 1);
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
