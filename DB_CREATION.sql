USE [master];
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'WPFClubManagementDB')
BEGIN
    CREATE DATABASE [WPFClubManagementDB];
END
GO

USE [WPFClubManagementDB];
GO

IF OBJECT_ID('dbo.Financial_Transactions', 'U') IS NOT NULL DROP TABLE dbo.Financial_Transactions;
IF OBJECT_ID('dbo.Attendance', 'U') IS NOT NULL DROP TABLE dbo.Attendance;
IF OBJECT_ID('dbo.AttendanceSessions', 'U') IS NOT NULL DROP TABLE dbo.AttendanceSessions;
IF OBJECT_ID('dbo.Students', 'U') IS NOT NULL DROP TABLE dbo.Students;
GO

CREATE TABLE dbo.Students (
    StudentID INT IDENTITY(1,1) PRIMARY KEY,
    StudentCode VARCHAR(20) NOT NULL UNIQUE,
    FullName NVARCHAR(100) NOT NULL,
    NFC_UID VARCHAR(100) NULL,
    Phone VARCHAR(20) NULL,
    Email VARCHAR(100) NULL,
    PhotoPath NVARCHAR(260) NULL,
    [Status] NVARCHAR(50) DEFAULT N'Active',
    JoinedDate DATE DEFAULT GETDATE(),
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE()
);
GO

-- Filtered Index: Allow multiple NULLs but enforce uniqueness for non-null UIDs
CREATE UNIQUE NONCLUSTERED INDEX IX_Students_NFC_UID
    ON dbo.Students(NFC_UID)
    WHERE NFC_UID IS NOT NULL;
GO

CREATE TABLE dbo.AttendanceSessions (
    SessionID INT IDENTITY(1,1) PRIMARY KEY,
    SessionName NVARCHAR(255) NOT NULL,
    StartTime DATETIME NOT NULL,
    EndTime DATETIME NULL,
    [Status] NVARCHAR(50) DEFAULT N'Active',
    Topic NVARCHAR(255) NULL,
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

CREATE TABLE dbo.Attendance (
    AttendanceID INT IDENTITY(1,1) PRIMARY KEY,
    SessionID INT NOT NULL,
    StudentID INT NOT NULL,
    CheckInTime DATETIME NOT NULL DEFAULT GETDATE(),
    Note NVARCHAR(255) NULL,

    CONSTRAINT FK_Attendance_Sessions FOREIGN KEY (SessionID)
        REFERENCES dbo.AttendanceSessions(SessionID) ON DELETE CASCADE,
    CONSTRAINT FK_Attendance_Students FOREIGN KEY (StudentID)
        REFERENCES dbo.Students(StudentID) ON DELETE CASCADE
);
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_Attendance_Session_Student
    ON dbo.Attendance(SessionID, StudentID);
GO

CREATE TABLE dbo.Financial_Transactions (
    TransactionID INT IDENTITY(1,1) PRIMARY KEY,
    StudentID INT NULL,
    TransactionType NVARCHAR(20) NOT NULL,
    Category NVARCHAR(100) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    TransactionDate DATETIME NULL,
    Description NVARCHAR(500) NULL,
    PaymentMethod NVARCHAR(50) NULL,
    PaymentStatus NVARCHAR(20) NOT NULL DEFAULT N'Paid',
    CreatedAt DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_Financial_Students FOREIGN KEY (StudentID)
        REFERENCES dbo.Students(StudentID) ON DELETE SET NULL
);
GO

CREATE NONCLUSTERED INDEX IX_Financial_Date
    ON dbo.Financial_Transactions(TransactionDate);
CREATE NONCLUSTERED INDEX IX_Financial_Type_Category
    ON dbo.Financial_Transactions(TransactionType, Category);
CREATE NONCLUSTERED INDEX IX_Financial_StudentID
    ON dbo.Financial_Transactions(StudentID);
GO
