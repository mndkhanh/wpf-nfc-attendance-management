using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using WPF.Models;

namespace WPF.Windows;

public partial class MemberFeeWindow : Window
{
    private sealed class StudentOption
    {
        public int StudentId { get; init; }
        public string DisplayName { get; init; } = string.Empty;
    }

    public string SubmittedFeeName { get; private set; } = string.Empty;
    public string SubmittedStudentDisplay { get; private set; } = string.Empty;
    public int SubmittedStudentId { get; private set; }
    public decimal SubmittedAmount { get; private set; }

    public MemberFeeWindow()
    {
        InitializeComponent();
        LoadStudents();
    }

    private void LoadStudents()
    {
        try
        {
            using var db = new WpfclubManagementDbContext();
            List<StudentOption> students = db.Students
                .AsNoTracking()
                .OrderBy(s => s.FullName)
                .Select(s => new StudentOption
                {
                    StudentId = s.StudentId,
                    DisplayName = $"{s.FullName} - {s.StudentCode}",
                })
                .ToList();

            StudentComboBox.ItemsSource = students;
            StudentComboBox.SelectedIndex = -1;
        }
        catch (System.Exception ex)
        {
            ShowValidationMessage($"Không tải được danh sách sinh viên.\n{ex.Message}");
        }
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        var feeName = FeeNameTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(feeName))
        {
            ShowValidationMessage("Vui lòng nhập tên khoản phí.");
            FeeNameTextBox.Focus();
            return;
        }

        if (!TryParseAmount(FeeAmountTextBox.Text, out var amount) || amount <= 0)
        {
            ShowValidationMessage("Vui lòng nhập số tiền hợp lệ.");
            FeeAmountTextBox.Focus();
            FeeAmountTextBox.SelectAll();
            return;
        }

        if (StudentComboBox.SelectedItem is not StudentOption selectedStudent ||
            string.IsNullOrWhiteSpace(selectedStudent.DisplayName))
        {
            ShowValidationMessage("Vui lòng chọn sinh viên.");
            StudentComboBox.Focus();
            return;
        }

        SubmittedFeeName = feeName;
        SubmittedStudentDisplay = selectedStudent.DisplayName;
        SubmittedStudentId = selectedStudent.StudentId;
        SubmittedAmount = amount;
        DialogResult = true;
    }

    private void ShowValidationMessage(string message)
    {
        MessageBox.Show(this, message, Title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private static bool TryParseAmount(string input, out decimal amount)
    {
        var digitsOnly = new string(input.Where(char.IsDigit).ToArray());
        return decimal.TryParse(digitsOnly, NumberStyles.Integer, CultureInfo.InvariantCulture, out amount);
    }
}
