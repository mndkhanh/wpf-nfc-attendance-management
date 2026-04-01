using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using WPF.Models;

namespace WPF.Windows;

public partial class MemberFeeWindow : Window
{
    public class StudentSelection
    {
        public int StudentId { get; init; }
        public string DisplayName { get; init; } = string.Empty;
    }

    private class StudentOption : System.ComponentModel.INotifyPropertyChanged
    {
        private bool _isSelected;
        public int StudentId { get; init; }
        public string DisplayName { get; init; } = string.Empty;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }

    private List<StudentOption> _allStudents = new();

    public string SubmittedFeeName { get; private set; } = string.Empty;
    public decimal SubmittedAmount { get; private set; }
    public List<StudentSelection> SelectedStudents { get; private set; } = new();

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
            _allStudents = db.Students
                .AsNoTracking()
                .OrderBy(s => s.FullName)
                .Select(s => new StudentOption
                {
                    StudentId = s.StudentId,
                    DisplayName = $"{s.FullName} - {s.StudentCode}",
                })
                .ToList();

            StudentListBox.ItemsSource = _allStudents;
        }
        catch (System.Exception ex)
        {
            ShowValidationMessage($"Không tải được danh sách sinh viên.\n{ex.Message}");
        }
    }

    private void SearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_allStudents == null) return;

        string searchText = SearchTextBox.Text.Trim().ToLower();
        if (string.IsNullOrWhiteSpace(searchText))
        {
            StudentListBox.ItemsSource = _allStudents;
        }
        else
        {
            StudentListBox.ItemsSource = _allStudents
                .Where(s => s.DisplayName.ToLower().Contains(searchText))
                .ToList();
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

        var selectedItems = _allStudents.Where(s => s.IsSelected).ToList();
        if (selectedItems.Count == 0)
        {
            ShowValidationMessage("Vui lòng chọn ít nhất một sinh viên.");
            return;
        }

        SubmittedFeeName = feeName;
        SubmittedAmount = amount;
        SelectedStudents = selectedItems.Select(s => new StudentSelection
        {
            StudentId = s.StudentId,
            DisplayName = s.DisplayName
        }).ToList();

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
