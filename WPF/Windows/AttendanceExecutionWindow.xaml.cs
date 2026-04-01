using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using WPF.Models;

namespace WPF.Windows;

public partial class AttendanceExecutionWindow : Window
{
    private readonly int _sessionId;
    private readonly string _sessionName;
    private CancellationTokenSource? _sseCancel;

    private const string FirebaseUrl =
        "https://wpf-nfc-attendance-management-default-rtdb.asia-southeast1.firebasedatabase.app";

    public AttendanceExecutionWindow(AttendanceSession session)
    {
        InitializeComponent();

        _sessionId = session.SessionId;
        _sessionName = session.SessionName;

        SessionTitleText.Text = $"Buổi điểm danh: {_sessionName}";

        StartFirebaseListener();
        LoadAttendance();

        this.Closed += (s, e) => _sseCancel?.Cancel();
    }

    private void StartFirebaseListener()
    {
        _sseCancel = new CancellationTokenSource();
        Task.Run(() => ListenSse(_sseCancel.Token));
        NfcStatusText.Text = "✅ Đã kết nối Firebase. Đặt thẻ NFC vào điện thoại...";
    }

    private async Task ListenSse(CancellationToken token)
    {
        using var http = new HttpClient();
        http.Timeout = Timeout.InfiniteTimeSpan;

        while (!token.IsCancellationRequested)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{FirebaseUrl}/current_uid.json");
                request.Headers.Add("Accept", "text/event-stream");

                using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
                using var stream = await response.Content.ReadAsStreamAsync(token);
                using var reader = new StreamReader(stream);

                string? eventType = null;

                while (!token.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null) break;

                    if (line.StartsWith("event:"))
                    {
                        eventType = line[6..].Trim();
                    }
                    else if (line.StartsWith("data:") && eventType == "put")
                    {
                        var raw = line[5..].Trim();

                        try
                        {
                            using var doc = JsonDocument.Parse(raw);
                            var dataEl = doc.RootElement.GetProperty("data");

                            string? uid = dataEl.ValueKind == JsonValueKind.String
                                ? dataEl.GetString()
                                : null;

                            if (!string.IsNullOrWhiteSpace(uid))
                            {
                                _ = http.DeleteAsync($"{FirebaseUrl}/current_uid.json", token);
                                Dispatcher.Invoke(() => ProcessNfcScan(uid!));
                            }
                        }
                        catch { /* ignore malformed events */ }

                        eventType = null;
                    }
                }
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            {
                Dispatcher.Invoke(() => NfcStatusText.Text = $"⚠️ Mất kết nối, thử lại... ({ex.Message})");
                await Task.Delay(3000, token);
            }
        }
    }

    private async void ProcessNfcScan(string uid)
    {
        try
        {
            using var db = new WpfclubManagementDbContext();

            var student = await db.Students.FirstOrDefaultAsync(s => s.NfcUid == uid);

            if (student == null)
            {
                System.Media.SystemSounds.Hand.Play();
                StudentNameText.Text = "Thẻ chưa đăng ký";
                StudentCodeText.Text = "UID: " + uid;
                NfcStatusText.Text = $"❌ Không tìm thấy sinh viên với UID: {uid}";
                return;
            }

            StudentNameText.Text = student.FullName;
            StudentCodeText.Text = student.StudentCode;
            NfcUidText.Text = "UID: " + student.NfcUid;

            bool alreadyCheckedIn = await db.Attendances
                .AnyAsync(a => a.SessionId == _sessionId && a.StudentId == student.StudentId);

            if (!alreadyCheckedIn)
            {
                db.Attendances.Add(new Attendance
                {
                    SessionId = _sessionId,
                    StudentId = student.StudentId,
                    CheckInTime = DateTime.Now,
                    Note = "Quét qua NFC"
                });

                await db.SaveChangesAsync();
                System.Media.SystemSounds.Beep.Play();
                LoadAttendance();

                NfcStatusText.Text = $"✅ Đã điểm danh: {student.FullName} ({student.StudentCode})";
            }
            else
            {
                NfcStatusText.Text = $"ℹ️ {student.FullName} đã điểm danh rồi.";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi xử lý điểm danh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadAttendance()
    {
        try
        {
            using var db = new WpfclubManagementDbContext();

            var records = db.Attendances
                .Where(a => a.SessionId == _sessionId)
                .Include(a => a.Student)
                .OrderByDescending(a => a.CheckInTime)
                .Select(a => new
                {
                    a.Student.StudentCode,
                    a.Student.FullName,
                    a.CheckInTime,
                    a.Note
                })
                .ToList();

            LiveAttendanceGrid.ItemsSource = records;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void FinishSessionButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            $"Bạn có chắc chắn muốn kết thúc buổi điểm danh '{_sessionName}'?",
            "Xác nhận kết thúc", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            using var db = new WpfclubManagementDbContext();
            var session = db.AttendanceSessions.Find(_sessionId);
            if (session != null)
            {
                session.Status = "Completed";
                session.EndTime = DateTime.Now;
                db.SaveChanges();
            }

            var mainWindow = new MainWindow();
            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();
            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi kết thúc buổi tập: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
