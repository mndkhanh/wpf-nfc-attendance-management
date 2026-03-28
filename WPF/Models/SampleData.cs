using System.Collections.Generic;

namespace WPF.Models;

public sealed class MemberItem
{
    public string StudentCode { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string NfcUid { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string JoinedDate { get; init; } = string.Empty;
}

public sealed class SessionItem
{
    public string SessionName { get; init; } = string.Empty;
    public string Topic { get; init; } = string.Empty;
    public string StartTime { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int AttendeeCount { get; init; }
}

public sealed class AttendanceRecord
{
    public string StudentCode { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string CheckInTime { get; init; } = string.Empty;
    public string Method { get; init; } = string.Empty;
    public string Note { get; init; } = string.Empty;
}

public sealed class FinanceItem
{
    public string TransactionDate { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string AmountDisplay { get; init; } = string.Empty;
    public string AmountBrush { get; init; } = string.Empty;
}

public sealed class DashboardMetric
{
    public string Title { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
}

public static class SampleData
{
    public static IReadOnlyList<MemberItem> GetMembers() =>
    [
        new() { StudentCode = "SE150111", FullName = "Nguyễn Văn An", NfcUid = "04:4B:2A:82:C7:5E:80", Phone = "0901 234 567", Status = "Đang hoạt động", JoinedDate = "05/09/2023" },
        new() { StudentCode = "SE150222", FullName = "Trần Thị Bích", NfcUid = "04:5C:3B:93:D8:6F:91", Phone = "0912 345 678", Status = "Đang hoạt động", JoinedDate = "10/09/2023" },
        new() { StudentCode = "SE150333", FullName = "Lê Hoàng Cường", NfcUid = "04:6D:4C:04:E9:70:02", Phone = "0923 456 789", Status = "Đang hoạt động", JoinedDate = "15/01/2024" },
        new() { StudentCode = "SS160444", FullName = "Phạm Nhật Duy", NfcUid = "04:7E:5D:15:FA:81:13", Phone = "0934 567 890", Status = "Mới đăng ký", JoinedDate = "20/02/2024" },
        new() { StudentCode = "SA160555", FullName = "Đỗ Phương Dung", NfcUid = "Chưa gắn thẻ", Phone = "0945 678 901", Status = "Tạm nghỉ", JoinedDate = "01/09/2022" },
    ];

    public static IReadOnlyList<SessionItem> GetSessions() =>
    [
        new() { SessionName = "Tập luyện buổi chiều", Topic = "Khởi động và kỹ thuật cơ bản", StartTime = "26/03/2026 18:00", Status = "Đang diễn ra", AttendeeCount = 18 },
        new() { SessionName = "Buổi nâng cao thứ 5", Topic = "Đối kháng và thể lực", StartTime = "24/03/2026 18:00", Status = "Hoàn thành", AttendeeCount = 22 },
        new() { SessionName = "Chuẩn bị thi đấu", Topic = "Tốc độ phản xạ", StartTime = "21/03/2026 18:00", Status = "Hoàn thành", AttendeeCount = 19 },
    ];

    public static IReadOnlyList<AttendanceRecord> GetAttendanceRecords() =>
    [
        new() { StudentCode = "SE150111", FullName = "Nguyễn Văn An", CheckInTime = "17:52", Method = "NFC", Note = "Đúng giờ" },
        new() { StudentCode = "SE150222", FullName = "Trần Thị Bích", CheckInTime = "18:05", Method = "Thủ công", Note = "Quên thẻ NFC" },
        new() { StudentCode = "SE150333", FullName = "Lê Hoàng Cường", CheckInTime = "17:58", Method = "NFC", Note = "-" },
        new() { StudentCode = "SS160444", FullName = "Phạm Nhật Duy", CheckInTime = "18:01", Method = "NFC", Note = "Đã xác thực" },
    ];

    public static IReadOnlyList<FinanceItem> GetFinanceItems() =>
    [
        new() { TransactionDate = "25/03/2026", Category = "Tài trợ", Description = "Tài trợ từ cựu thành viên", AmountDisplay = "+ 5.000.000", AmountBrush = "#2E9E46" },
        new() { TransactionDate = "24/03/2026", Category = "HLV", Description = "Thù lao huấn luyện viên", AmountDisplay = "- 3.000.000", AmountBrush = "#E24537" },
        new() { TransactionDate = "22/03/2026", Category = "Phí thành viên", Description = "Thu phí tháng 3 - Nguyễn Văn An", AmountDisplay = "+ 200.000", AmountBrush = "#2E9E46" },
        new() { TransactionDate = "18/03/2026", Category = "Trang thiết bị", Description = "Mua bảo hộ tập luyện", AmountDisplay = "- 1.500.000", AmountBrush = "#E24537" },
    ];

    public static IReadOnlyList<DashboardMetric> GetDashboardMetrics() =>
    [
        new() { Title = "Thành viên", Value = "34", Subtitle = "4 thành viên mới trong tháng" },
        new() { Title = "Buổi tập", Value = "12", Subtitle = "3 buổi đang lên lịch" },
        new() { Title = "Đã điểm danh", Value = "18", Subtitle = "Buổi chiều hôm nay" },
        new() { Title = "Quỹ CLB", Value = "₫ 8.200.000", Subtitle = "Cập nhật 26/03/2026" },
    ];
}
