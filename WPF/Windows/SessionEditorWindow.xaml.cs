using System;
using System.Windows;
using WPF.Models;

namespace WPF.Windows;

public partial class SessionEditorWindow : Window
{
    public SessionEditorWindow()
    {
        InitializeComponent();
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SessionNameTextBox.Text))
        {
            MessageBox.Show("Vui lòng nhập tên buổi điểm danh.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            SessionNameTextBox.Focus();
            return;
        }

        try
        {
            using var db = new WpfclubManagementDbContext();

            var newSession = new AttendanceSession
            {
                SessionName = SessionNameTextBox.Text.Trim(),
                Topic = string.IsNullOrWhiteSpace(TopicTextBox.Text) ? null : TopicTextBox.Text.Trim(),
                StartTime = DateTime.Now,
                Status = "Active",
                CreatedAt = DateTime.Now
            };

            db.AttendanceSessions.Add(newSession);
            db.SaveChanges();

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi tạo buổi điểm danh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
