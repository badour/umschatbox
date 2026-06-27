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
    - ToAccountName
    - AddedBy
    - YearName
    - DepartmentName

    Account lookup rules:
    - User account code -> FromAccountID
    - User account name -> ToAccountName
    - User descriptive text -> DocDetails and DocTitl
    - Values -> DebetValue and CreditValue
    - Academic-year grouping marker -> |academic_years
      Academic year starts on September 1 and ends on August 31.
*/

CREATE OR ALTER PROCEDURE dbo.Chatbot_GetAccountRelatedExpenses
    @SearchText NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Search NVARCHAR(200) = LTRIM(RTRIM(ISNULL(@SearchText, N'')));

    SELECT
        MAX(FromAccountID) AS FromAccountID,
        MAX(ToAccountName) AS ToAccountName,
        COUNT_BIG(*) AS TransactionCount,
        MIN([date]) AS PeriodStart,
        MAX([date]) AS PeriodEnd,
        SUM(ISNULL(DebetValue, 0)) AS TotalDebetValue,
        SUM(ISNULL(CreditValue, 0)) AS TotalCreditValue,
        SUM(ISNULL(DebetValue, 0) + ISNULL(CreditValue, 0)) AS TotalAccountingValue,
        SUM(ISNULL(DebetValue, 0) - ISNULL(CreditValue, 0)) AS NetAccountingValue,
        AVG(CAST(ISNULL(DebetValue, 0) + ISNULL(CreditValue, 0) AS DECIMAL(18, 2))) AS AverageAccountingValue,
        MIN(ISNULL(DebetValue, 0) + ISNULL(CreditValue, 0)) AS MinimumAccountingValue,
        MAX(ISNULL(DebetValue, 0) + ISNULL(CreditValue, 0)) AS MaximumAccountingValue,
        SQRT(ABS(CONVERT(FLOAT, SUM(ISNULL(DebetValue, 0) + ISNULL(CreditValue, 0))))) AS SquareRootTotalValue
    FROM dbo.ExpencesAccDocSum
    WHERE FromAccountID LIKE @Search + N'%'
       OR ToAccountName LIKE N'%' + @Search + N'%'
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
        SUM(ISNULL(DebetValue, 0) - ISNULL(CreditValue, 0)) AS NetFixedAssetsValue,
        AVG(CAST(ISNULL(DebetValue, 0) + ISNULL(CreditValue, 0) AS DECIMAL(18, 2))) AS AverageFixedAssetsValue,
        MIN(ISNULL(DebetValue, 0) + ISNULL(CreditValue, 0)) AS MinimumFixedAssetsValue,
        MAX(ISNULL(DebetValue, 0) + ISNULL(CreditValue, 0)) AS MaximumFixedAssetsValue,
        SQRT(ABS(CONVERT(FLOAT, SUM(ISNULL(DebetValue, 0) + ISNULL(CreditValue, 0))))) AS SquareRootTotalValue
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
        SUM(ISNULL(DebetValue, 0) - ISNULL(CreditValue, 0)) AS NetBuildingsValue,
        AVG(CAST(ISNULL(DebetValue, 0) + ISNULL(CreditValue, 0) AS DECIMAL(18, 2))) AS AverageBuildingsValue,
        MIN(ISNULL(DebetValue, 0) + ISNULL(CreditValue, 0)) AS MinimumBuildingsValue,
        MAX(ISNULL(DebetValue, 0) + ISNULL(CreditValue, 0)) AS MaximumBuildingsValue,
        SQRT(ABS(CONVERT(FLOAT, SUM(ISNULL(DebetValue, 0) + ISNULL(CreditValue, 0))))) AS SquareRootTotalValue
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
    DECLARE @HasSpecificText BIT = 0;
    DECLARE @GroupByAcademicYear BIT = 0;

    IF CHARINDEX(N'|academic_years', @Search) > 0
    BEGIN
        SET @GroupByAcademicYear = 1;
        SET @Search = REPLACE(@Search, N'|academic_years', N'');
        SET @NormalizedSearch = LOWER(@Search);
    END;

    IF @YearPosition > 0
    BEGIN
        SET @StartDate = DATEFROMPARTS(TRY_CONVERT(INT, SUBSTRING(@NormalizedSearch, @YearPosition, 4)), 1, 1);
    END;

    IF @CodePosition > 0
    BEGIN
        SET @AccountCode = SUBSTRING(@Search, @CodePosition, 10);
        SET @AccountCode = LEFT(@AccountCode, PATINDEX('%[^0-9]%', @AccountCode + N'X') - 1);
    END;

    IF @AccountCode IS NULL
       AND @Search NOT IN (N'', N'all years', N'كل السنوات', N'لكل السنوات', N'كل الاعوام', N'لكل الاعوام', N'كل الأعوام', N'لكل الأعوام')
    BEGIN
        SET @HasSpecificText = 1;
    END;

    ;WITH FilteredRows AS
    (
        SELECT
            *,
            CASE
                WHEN MONTH([date]) >= 9
                    THEN CONVERT(NVARCHAR(4), YEAR([date])) + N'-' + CONVERT(NVARCHAR(4), YEAR([date]) + 1)
                ELSE CONVERT(NVARCHAR(4), YEAR([date]) - 1) + N'-' + CONVERT(NVARCHAR(4), YEAR([date]))
            END AS AcademicYear
        FROM dbo.ExpencesAccDocSum
        WHERE (
                (@AccountCode IS NOT NULL AND FromAccountID LIKE @AccountCode + N'%')
                OR (@AccountCode IS NULL AND @HasSpecificText = 0 AND FromAccountID LIKE N'3%')
                OR (@AccountCode IS NULL AND @HasSpecificText = 1)
          )
          AND (@StartDate IS NULL OR ([date] >= @StartDate AND [date] < DATEADD(DAY, 1, @EndDate)))
          AND (
                @AccountCode IS NOT NULL
                OR @HasSpecificText = 0
                OR ToAccountName LIKE N'%' + @Search + N'%'
                OR DocTitl LIKE N'%' + @Search + N'%'
                OR CAST(DocDetails AS NVARCHAR(MAX)) LIKE N'%' + @Search + N'%'
                OR AddedBy LIKE N'%' + @Search + N'%'
                OR DepartmentName LIKE N'%' + @Search + N'%'
          )
    )
    SELECT
        CASE WHEN @GroupByAcademicYear = 1 THEN AcademicYear ELSE NULL END AS AcademicYear,
        CASE
            WHEN @AccountCode IS NOT NULL THEN @AccountCode
            WHEN @HasSpecificText = 0 THEN N'3'
            ELSE N'text search'
        END AS AccountPrefix,
        @Search AS SearchText,
        COUNT_BIG(*) AS TransactionCount,
        MIN([date]) AS PeriodStart,
        MAX([date]) AS PeriodEnd,
        SUM(ISNULL(DebetValue, 0)) AS TotalDebetValue,
        SUM(ISNULL(CreditValue, 0)) AS TotalCreditValue,
        SUM(ISNULL(DebetValue, 0) + ISNULL(CreditValue, 0)) AS TotalExpensesValue,
        SUM(ISNULL(DebetValue, 0) - ISNULL(CreditValue, 0)) AS NetExpensesValue,
        AVG(CAST(ISNULL(DebetValue, 0) + ISNULL(CreditValue, 0) AS DECIMAL(18, 2))) AS AverageExpensesValue,
        MIN(ISNULL(DebetValue, 0) + ISNULL(CreditValue, 0)) AS MinimumExpensesValue,
        MAX(ISNULL(DebetValue, 0) + ISNULL(CreditValue, 0)) AS MaximumExpensesValue,
        SQRT(ABS(CONVERT(FLOAT, SUM(ISNULL(DebetValue, 0) + ISNULL(CreditValue, 0))))) AS SquareRootTotalValue
    FROM FilteredRows
    GROUP BY CASE WHEN @GroupByAcademicYear = 1 THEN AcademicYear ELSE NULL END
    ORDER BY AcademicYear;
END;
GO
