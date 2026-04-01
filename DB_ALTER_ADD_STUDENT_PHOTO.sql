USE [WPFClubManagementDB];
GO

IF COL_LENGTH('dbo.Students', 'PhotoPath') IS NULL
BEGIN
    ALTER TABLE dbo.Students
    ADD PhotoPath NVARCHAR(260) NULL;
END
GO
