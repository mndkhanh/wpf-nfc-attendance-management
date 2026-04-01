using System;
using System.Windows;
using System.Windows.Controls;
using WPF.Models;

namespace WPF.Windows;

public partial class AddMemberWindow : Window
{
    public AddMemberWindow()
    {
        InitializeComponent();
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
        {
            MessageBox.Show("Vui lòng nhập họ và tên.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            FullNameTextBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(StudentCodeTextBox.Text))
        {
            MessageBox.Show("Vui lòng nhập MSSV.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            StudentCodeTextBox.Focus();
            return;
        }

        try
        {
            using var db = new WpfclubManagementDbContext();
            
            var newStudent = new Student
            {
                FullName = FullNameTextBox.Text.Trim(),
                StudentCode = StudentCodeTextBox.Text.Trim(),
                NfcUid = string.IsNullOrWhiteSpace(NfcUidTextBox.Text) ? null : NfcUidTextBox.Text.Trim(),
                Phone = string.IsNullOrWhiteSpace(PhoneTextBox.Text) ? null : PhoneTextBox.Text.Trim(),
                Email = string.IsNullOrWhiteSpace(EmailTextBox.Text) ? null : EmailTextBox.Text.Trim(),
                Status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Active",
                JoinedDate = DateOnly.FromDateTime(DateTime.Now),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            db.Students.Add(newStudent);
            db.SaveChanges();

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi lưu sinh viên: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
