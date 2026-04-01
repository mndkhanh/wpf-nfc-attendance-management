-- ==========================================================
-- SCRIPT TẠO CƠ SỞ DỮ LIỆU: QUẢN LÝ CÂU LẠC BỘ (1 CLB)
-- Tên Database: WPFClubManagementDB
-- Hệ quản trị: SQL Server
-- Lưu ý cốt lõi: Hệ thống cực kỳ đơn giản, KHÔNG CÓ TÀI KHOẢN ĐĂNG NHẬP, KHÔNG CÓ BẢNG USERS.
-- ==========================================================

USE [master];
GO

-- 1. Tạo Database (Nếu chưa tồn tại)
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'WPFClubManagementDB')
BEGIN
    CREATE DATABASE [WPFClubManagementDB];
END
GO

USE [WPFClubManagementDB];
GO

-- ==========================================================
-- 2. Tạo Table: Students (Quản lý sinh viên / hội viên CLB)
-- ==========================================================
IF OBJECT_ID('dbo.Students', 'U') IS NOT NULL DROP TABLE dbo.Students;
CREATE TABLE dbo.Students (
    StudentID INT IDENTITY(1,1) PRIMARY KEY,
    StudentCode VARCHAR(20) NOT NULL UNIQUE,  -- Mã số sinh viên (VD: SE123456)
    FullName NVARCHAR(100) NOT NULL,
    NFC_UID VARCHAR(100) NULL UNIQUE,         -- Mã thẻ NFC (Trống nếu chưa phát thẻ)
    Phone VARCHAR(20) NULL,
    Email VARCHAR(100) NULL,
    [Status] NVARCHAR(50) DEFAULT N'Active',  -- Trạng thái: Active, Inactive, ...
    JoinedDate DATE DEFAULT GETDATE(),
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE()
);
GO

-- ==========================================================
-- 3. Tạo Table: AttendanceSessions (Quản lý các buổi tập / sự kiện)
-- ==========================================================
IF OBJECT_ID('dbo.AttendanceSessions', 'U') IS NOT NULL DROP TABLE dbo.AttendanceSessions;
CREATE TABLE dbo.AttendanceSessions (
    SessionID INT IDENTITY(1,1) PRIMARY KEY,
    SessionName NVARCHAR(255) NOT NULL,       -- Tên buổi tập, ví dụ. "Buổi tập thường lệ 22/03"
    StartTime DATETIME NOT NULL,              -- Thời gian "Bắt đầu buổi tập"
    EndTime DATETIME NULL,                    -- Thời gian "Kết thúc buổi tập", NULL nếu đang diễn ra
    [Status] NVARCHAR(50) DEFAULT N'Active',  -- 'Active' (Đang quét NFC), 'Completed' (Kết thúc)
    Topic NVARCHAR(255) NULL,                 -- Nội dung buổi điểm danh / buổi tập
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- ==========================================================
-- 4. Tạo Table: Attendance (Chi tiết lịch sử điểm danh của sinh viên vào buổi tập)
-- ==========================================================
IF OBJECT_ID('dbo.Attendance', 'U') IS NOT NULL DROP TABLE dbo.Attendance;
CREATE TABLE dbo.Attendance (
    AttendanceID INT IDENTITY(1,1) PRIMARY KEY,
    SessionID INT NOT NULL,
    StudentID INT NOT NULL,
    CheckInTime DATETIME NOT NULL DEFAULT GETDATE(),
    Note NVARCHAR(255) NULL,                  -- Ghi chú bổ sung
    
    CONSTRAINT FK_Attendance_Sessions FOREIGN KEY (SessionID) 
        REFERENCES dbo.AttendanceSessions(SessionID) ON DELETE CASCADE,
    CONSTRAINT FK_Attendance_Students FOREIGN KEY (StudentID) 
        REFERENCES dbo.Students(StudentID) ON DELETE CASCADE
);
GO

-- Đảm bảo 1 sinh viên chỉ điểm danh 1 lần trong 1 buổi tập
CREATE UNIQUE NONCLUSTERED INDEX UQ_Attendance_Session_Student ON dbo.Attendance(SessionID, StudentID);
GO

-- ==========================================================
-- 5. Tạo Table: Financial_Transactions (Sổ quỹ Thu / Chi)
-- ==========================================================
IF OBJECT_ID('dbo.Financial_Transactions', 'U') IS NOT NULL DROP TABLE dbo.Financial_Transactions;
CREATE TABLE dbo.Financial_Transactions (
    TransactionID INT IDENTITY(1,1) PRIMARY KEY,
    StudentID INT NULL,                       -- Thu/Chi gắn với Sinh viên nào (VD: Thu Học phí)
    TransactionType NVARCHAR(20) NOT NULL,    -- Phân loại: 'Thu' hoặc 'Chi'
    Category NVARCHAR(100) NOT NULL,          -- Danh mục: 'Áo giáp', 'Thảm', 'HLV', 'Phí sinh hoạt', 'Tài trợ'
    Amount DECIMAL(18,2) NOT NULL,            -- Số tiền giao dịch
    TransactionDate DATETIME NOT NULL DEFAULT GETDATE(),
    Description NVARCHAR(500) NULL,           -- Diễn giải chi tiết
    PaymentMethod NVARCHAR(50) NULL,          -- Hình thức nộp: 'Tiền mặt', 'Chuyển khoản'
    -- (Đã lược bỏ cột CreatedBy vì hệ thống mở là chạy, không có đăng nhập hay phân quyền)
    CreatedAt DATETIME DEFAULT GETDATE(),
    
    CONSTRAINT FK_Financial_Students FOREIGN KEY (StudentID) 
        REFERENCES dbo.Students(StudentID) ON DELETE SET NULL
);
GO

-- Tạo index để tính toán thống kê thu/chi theo tháng/năm nhanh hơn
CREATE NONCLUSTERED INDEX IX_Financial_Date ON dbo.Financial_Transactions(TransactionDate);
CREATE NONCLUSTERED INDEX IX_Financial_Type_Category ON dbo.Financial_Transactions(TransactionType, Category);
CREATE NONCLUSTERED INDEX IX_Financial_StudentID ON dbo.Financial_Transactions(StudentID);
GO
