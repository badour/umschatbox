IF DB_ID(N'ExpensesDev') IS NULL
BEGIN
    CREATE DATABASE ExpensesDev;
END;
GO

USE ExpensesDev;
GO

IF OBJECT_ID(N'dbo.People', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.People
    (
        PersonId INT NOT NULL PRIMARY KEY,
        DisplayName NVARCHAR(200) NOT NULL,
        EmployeeNumber NVARCHAR(50) NOT NULL,
        UserName NVARCHAR(100) NOT NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.Expenses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Expenses
    (
        ExpenseId INT NOT NULL PRIMARY KEY,
        PersonId INT NOT NULL,
        InvoiceNumber NVARCHAR(50) NOT NULL,
        ExpenseCode NVARCHAR(50) NOT NULL,
        ExpenseDescription NVARCHAR(500) NOT NULL,
        Amount DECIMAL(18, 2) NOT NULL,
        CurrencyCode NVARCHAR(3) NOT NULL,
        ExpenseDate DATE NOT NULL,
        Status NVARCHAR(50) NOT NULL,
        CONSTRAINT FK_Expenses_People FOREIGN KEY (PersonId) REFERENCES dbo.People (PersonId)
    );
END;
GO

IF OBJECT_ID(N'dbo.ExpenseFiles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ExpenseFiles
    (
        FileId INT NOT NULL PRIMARY KEY,
        InvoiceNumber NVARCHAR(50) NOT NULL,
        ExpenseCode NVARCHAR(50) NOT NULL,
        FileName NVARCHAR(260) NOT NULL,
        FileUrl NVARCHAR(500) NOT NULL,
        UploadedOn DATETIME2 NOT NULL
    );
END;
GO

MERGE dbo.People AS target
USING
(
    VALUES
        (1, N'Aisha Khan', N'E1001', N'akhan'),
        (2, N'Jordan Lee', N'E1002', N'jlee')
) AS source (PersonId, DisplayName, EmployeeNumber, UserName)
ON target.PersonId = source.PersonId
WHEN NOT MATCHED THEN
    INSERT (PersonId, DisplayName, EmployeeNumber, UserName)
    VALUES (source.PersonId, source.DisplayName, source.EmployeeNumber, source.UserName);
GO

MERGE dbo.Expenses AS target
USING
(
    VALUES
        (1, 1, N'INV-10045', N'TRAVEL', N'Flight to client site', 420.50, N'USD', CAST('2026-05-10' AS DATE), N'Approved'),
        (2, 1, N'INV-10045', N'MEALS', N'Team dinner', 86.25, N'USD', CAST('2026-05-11' AS DATE), N'Approved'),
        (3, 2, N'INV-20010', N'OFFICE', N'Supplies', 55.00, N'USD', CAST('2026-05-12' AS DATE), N'Pending')
) AS source (ExpenseId, PersonId, InvoiceNumber, ExpenseCode, ExpenseDescription, Amount, CurrencyCode, ExpenseDate, Status)
ON target.ExpenseId = source.ExpenseId
WHEN NOT MATCHED THEN
    INSERT (ExpenseId, PersonId, InvoiceNumber, ExpenseCode, ExpenseDescription, Amount, CurrencyCode, ExpenseDate, Status)
    VALUES (source.ExpenseId, source.PersonId, source.InvoiceNumber, source.ExpenseCode, source.ExpenseDescription, source.Amount, source.CurrencyCode, source.ExpenseDate, source.Status);
GO

MERGE dbo.ExpenseFiles AS target
USING
(
    VALUES
        (1, N'INV-10045', N'TRAVEL', N'receipt-flight.pdf', N'https://example.com/files/receipt-flight.pdf', CAST('2026-05-10T10:15:00' AS DATETIME2)),
        (2, N'INV-10045', N'MEALS', N'receipt-dinner.pdf', N'/expenses/receipts/receipt-dinner.pdf', CAST('2026-05-11T18:40:00' AS DATETIME2))
) AS source (FileId, InvoiceNumber, ExpenseCode, FileName, FileUrl, UploadedOn)
ON target.FileId = source.FileId
WHEN NOT MATCHED THEN
    INSERT (FileId, InvoiceNumber, ExpenseCode, FileName, FileUrl, UploadedOn)
    VALUES (source.FileId, source.InvoiceNumber, source.ExpenseCode, source.FileName, source.FileUrl, source.UploadedOn);
GO
