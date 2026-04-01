using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using WPF.Models;
using WPF.Windows;

namespace WPF.Pages;

public partial class AttendancePage : UserControl
{
    public AttendancePage()
    {
        InitializeComponent();
        LoadSessions();
    }

    private void LoadSessions()
    {
        try
        {
            using var db = new WpfclubManagementDbContext();
            var sessions = db.AttendanceSessions
                .Include(s => s.Attendances)
                .OrderByDescending(s => s.StartTime)
                .ToList();

            SessionsGrid.ItemsSource = sessions;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không tải được danh sách buổi tập: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadAttendance(int sessionId)
    {
        try
        {
            using var db = new WpfclubManagementDbContext();
            var attendanceRecords = db.Attendances
                .Where(a => a.SessionId == sessionId)
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

            AttendanceGrid.ItemsSource = attendanceRecords;
            SessionStatsText.Text = $"Tổng số: {attendanceRecords.Count} sinh viên";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không tải được danh sách điểm danh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SessionsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SessionsGrid.SelectedItem is AttendanceSession selectedSession)
        {
            SelectedSessionNameText.Text = $"Buổi điểm danh: {selectedSession.SessionName}";
            SelectedSessionTopicText.Text = selectedSession.Topic ?? "Không có nội dung chi tiết";
            AttendeeListHeaderText.Text = $"Danh sách sinh viên đã điểm danh ({selectedSession.SessionName})";
            
            LoadAttendance(selectedSession.SessionId);

            // Show control buttons if session is active
            bool isActive = selectedSession.Status == "Active";
            FinishSessionButton.Visibility = isActive ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void StartSessionButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SessionEditorWindow
        {
            Owner = Window.GetWindow(this),
        };

        if (dialog.ShowDialog() == true && dialog.CreatedSession != null)
        {
            var oldWindow = Window.GetWindow(this);
            var executionWindow = new AttendanceExecutionWindow(dialog.CreatedSession);
            
            // Re-assign MainWindow BEFORE closing the old one
            Application.Current.MainWindow = executionWindow;
            executionWindow.Show();
            
            // Close the old one
            oldWindow?.Close();
        }
    }

    private void FinishSessionButton_Click(object sender, RoutedEventArgs e)
    {
        if (SessionsGrid.SelectedItem is AttendanceSession selectedSession)
        {
            var result = MessageBox.Show($"Bạn có chắc chắn muốn kết thúc buổi điểm danh '{selectedSession.SessionName}'?", 
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using var db = new WpfclubManagementDbContext();
                    var session = db.AttendanceSessions.Find(selectedSession.SessionId);
                    if (session != null)
                    {
                        session.Status = "Completed";
                        session.EndTime = DateTime.Now;
                        db.SaveChanges();
                        LoadSessions();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi kết thúc buổi tập: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
