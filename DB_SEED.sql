USE [WPFClubManagementDB];
GO

DELETE FROM dbo.Financial_Transactions;
DELETE FROM dbo.Attendance;
DELETE FROM dbo.AttendanceSessions;
DELETE FROM dbo.Students;
GO

SET IDENTITY_INSERT dbo.Students ON;
INSERT INTO dbo.Students (StudentID, StudentCode, FullName, NFC_UID, Phone, Email, PhotoPath, [Status], JoinedDate)
VALUES
(1, 'SE150111', N'Nguyễn Văn An', '044B2A82C75E80', '0901234567', 'an.nv@gmail.com', NULL, N'Active', '2023-09-05'),
(2, 'SE150222', N'Trần Thị Bích', '045C3B93D86F91', '0912345678', 'bich.tt@gmail.com', NULL, N'Active', '2023-09-10'),
(3, 'SE150333', N'Lê Hoàng Cường', '046D4C04E97002', '0923456789', 'cuong.lh@gmail.com', NULL, N'Active', '2024-01-15'),
(4, 'SS160444', N'Phạm Nhật Duy', '047E5D15FA8113', '0934567890', 'duy.pn@gmail.com', NULL, N'Active', '2024-02-20'),
(5, 'SA160555', N'Đỗ Phương Dung', '048F6E260B9224', '0945678901', 'dung.dp@gmail.com', NULL, N'Inactive', '2022-09-01');
SET IDENTITY_INSERT dbo.Students OFF;
GO

SET IDENTITY_INSERT dbo.AttendanceSessions ON;
INSERT INTO dbo.AttendanceSessions (SessionID, SessionName, StartTime, EndTime, [Status], Topic)
VALUES
(1, N'Buổi tập cơ bản thứ 3', '2024-03-20 18:00:00', '2024-03-20 20:30:00', N'Completed', N'Khởi động, kỹ thuật cơ bản'),
(2, N'Buổi tập nâng cao thứ 5', '2024-03-22 18:00:00', '2024-03-22 20:30:00', N'Completed', N'Đối kháng, thể lực'),
(3, N'Buổi tập chuẩn bị thi đấu', '2024-03-25 18:00:00', NULL, N'Active', N'Chiến thuật thi đấu');
SET IDENTITY_INSERT dbo.AttendanceSessions OFF;
GO

INSERT INTO dbo.Attendance (SessionID, StudentID, CheckInTime, Note)
VALUES
(1, 1, '2024-03-20 17:55:00', NULL),
(1, 2, '2024-03-20 18:05:00', N'Đi muộn 5 phút'),
(1, 3, '2024-03-20 17:50:00', NULL),
(1, 4, '2024-03-20 17:58:00', NULL),
(2, 1, '2024-03-22 18:00:00', NULL),
(2, 2, '2024-03-22 17:45:00', NULL),
(2, 3, '2024-03-22 18:10:00', N'SV quên mang thẻ, điểm danh trong WPF'),
(3, 1, '2024-03-25 17:50:00', NULL),
(3, 4, '2024-03-25 18:01:00', NULL);
GO

INSERT INTO dbo.Financial_Transactions (StudentID, TransactionType, Category, Amount, TransactionDate, Description, PaymentMethod)
VALUES
(NULL, N'Chi', N'Thảm', 2500000, '2024-02-15', N'Mua 10 tấm thảm tập mới', N'Chuyển khoản'),
(NULL, N'Chi', N'Áo giáp', 1500000, '2024-03-01', N'Trang bị 3 bộ áo giáp đấu', N'Chuyển khoản'),
(NULL, N'Chi', N'HLV', 3000000, '2024-03-05', N'Thù lao HLV Tuấn tháng 2', N'Tiền mặt'),
(NULL, N'Thu', N'Tài trợ', 5000000, '2024-01-10', N'Cựu sinh viên tài trợ quỹ CLB', N'Chuyển khoản'),
(1, N'Thu', N'Phí sinh hoạt', 200000, '2024-03-10', N'SV Nguyễn Văn An nộp phí tháng 3', N'Tiền mặt'),
(2, N'Thu', N'Phí sinh hoạt', 200000, '2024-03-11', N'SV Trần Thị Bích nộp phí tháng 3', N'Chuyển khoản'),
(3, N'Thu', N'Phí sinh hoạt', 600000, '2024-03-20', N'SV Lê Hoàng Cường nộp phí quý 2', N'Chuyển khoản'),
(4, N'Thu', N'Phí sinh hoạt', 200000, '2024-03-22', N'SV Phạm Nhật Duy nộp phí tháng 3', N'Tiền mặt');
GO
