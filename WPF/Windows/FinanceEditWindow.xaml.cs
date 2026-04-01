using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using WPF.Models;

namespace WPF.Windows;

public partial class FinanceEditWindow : Window
{
    private readonly int _transactionId;

    /// <summary>Set to true after a successful save or delete so the caller can reload.</summary>
    public bool DataChanged { get; private set; }

    public FinanceEditWindow(int transactionId)
    {
        InitializeComponent();
        _transactionId = transactionId;
        LoadTransaction();
    }

    // ────────────────────────────────────────────────
    //  Load
    // ────────────────────────────────────────────────

    private void LoadTransaction()
    {
        try
        {
            using var db = new WpfclubManagementDbContext();
            var tx = db.FinancialTransactions
                .Include(t => t.Student)
                .FirstOrDefault(t => t.TransactionId == _transactionId);

            if (tx == null)
            {
                MessageBox.Show("Không tìm thấy giao dịch.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                DialogResult = false;
                Close();
                return;
            }

            // TransactionType ComboBox
            SelectComboByContent(TypeComboBox, tx.TransactionType);

            // Date
            TransactionDatePicker.SelectedDate = tx.TransactionDate.Date;

            // Category, Description, Amount
            CategoryTextBox.Text    = tx.Category;
            DescriptionTextBox.Text = tx.Description ?? string.Empty;
            AmountTextBox.Text      = ((long)tx.Amount).ToString();

            // PaymentMethod ComboBox
            SelectComboByContent(PaymentMethodComboBox, tx.PaymentMethod);

            // PaymentStatus (only visible for student transactions)
            if (tx.StudentId != null)
            {
                SelectComboByContent(PaymentStatusComboBox, tx.PaymentStatus);
                PaymentStatusPanel.Visibility = Visibility.Visible;
            }
            else
            {
                PaymentStatusPanel.Visibility = Visibility.Collapsed;
            }

            // Student (read-only, only visible when linked)
            if (tx.Student != null)
            {
                StudentInfoText.Text = $"{tx.Student.FullName}  —  {tx.Student.StudentCode}";
                StudentInfoPanel.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi tải giao dịch: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
            DialogResult = false;
            Close();
        }
    }

    // ────────────────────────────────────────────────
    //  Save
    // ────────────────────────────────────────────────

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryBuildUpdateFromForm(out var type, out var category, out var description,
                out var amount, out var date, out var paymentMethod, out var paymentStatus))
        {
            return;
        }

        try
        {
            using var db = new WpfclubManagementDbContext();
            var tx = db.FinancialTransactions.Find(_transactionId);

            if (tx == null)
            {
                MessageBox.Show("Giao dịch không còn tồn tại.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            tx.TransactionType  = type;
            tx.Category         = category;
            tx.Description      = string.IsNullOrWhiteSpace(description) ? null : description;
            tx.Amount           = amount;
            tx.TransactionDate  = date;
            tx.PaymentMethod    = string.IsNullOrWhiteSpace(paymentMethod) ? null : paymentMethod;
            tx.PaymentStatus    = paymentStatus;

            db.SaveChanges();
            DataChanged = true;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi lưu giao dịch: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ────────────────────────────────────────────────
    //  Delete
    // ────────────────────────────────────────────────

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "Bạn có chắc chắn muốn xóa giao dịch này không?",
            "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            using var db = new WpfclubManagementDbContext();
            var tx = db.FinancialTransactions.Find(_transactionId);
            if (tx != null)
            {
                db.FinancialTransactions.Remove(tx);
                db.SaveChanges();
            }

            DataChanged = true;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi xóa giao dịch: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    // ────────────────────────────────────────────────
    //  Validation helpers
    // ────────────────────────────────────────────────

    private bool TryBuildUpdateFromForm(
        out string type, out string category, out string description,
        out decimal amount, out DateTime date, out string? paymentMethod, out string paymentStatus)
    {
        type = string.Empty; category = string.Empty; description = string.Empty;
        amount = 0; date = DateTime.Now; paymentMethod = null; paymentStatus = "Paid";

        // Type
        if (TypeComboBox.SelectedItem is not ComboBoxItem typeItem ||
            string.IsNullOrWhiteSpace(typeItem.Content?.ToString()))
        {
            ShowValidation("Vui lòng chọn loại giao dịch (Thu / Chi).");
            TypeComboBox.Focus();
            return false;
        }
        type = typeItem.Content!.ToString()!.Trim();

        // Category
        category = CategoryTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(category))
        {
            ShowValidation("Vui lòng nhập danh mục.");
            CategoryTextBox.Focus();
            return false;
        }

        // Description (optional)
        description = DescriptionTextBox.Text.Trim();

        // Amount
        var digitsOnly = new string(AmountTextBox.Text.Where(char.IsDigit).ToArray());
        if (!decimal.TryParse(digitsOnly, NumberStyles.Integer, CultureInfo.InvariantCulture, out amount) || amount <= 0)
        {
            ShowValidation("Vui lòng nhập số tiền hợp lệ.");
            AmountTextBox.Focus();
            AmountTextBox.SelectAll();
            return false;
        }

        // Date
        if (TransactionDatePicker.SelectedDate is not DateTime pickedDate)
        {
            ShowValidation("Vui lòng chọn ngày giao dịch.");
            TransactionDatePicker.Focus();
            return false;
        }
        date = pickedDate;

        // PaymentMethod (optional)
        paymentMethod = (PaymentMethodComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString()?.Trim();

        // PaymentStatus
        paymentStatus = (PaymentStatusComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString()?.Trim() ?? "Paid";

        return true;
    }

    private void ShowValidation(string message)
        => MessageBox.Show(this, message, Title, MessageBoxButton.OK, MessageBoxImage.Warning);

    private static void SelectComboByContent(ComboBox combo, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        foreach (var item in combo.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Content?.ToString(), value.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedItem = item;
                return;
            }
        }
    }
}
