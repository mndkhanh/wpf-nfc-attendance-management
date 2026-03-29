using System.Globalization;
using System.Linq;
using System.Windows;

namespace WPF.Windows;

public partial class FinanceEntryWindow : Window
{
    public string SubmittedName { get; private set; } = string.Empty;
    public decimal SubmittedAmount { get; private set; }

    public FinanceEntryWindow(string titleText)
    {
        InitializeComponent();
        Title = titleText;
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        var entryName = EntryNameTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(entryName))
        {
            ShowValidationMessage("Vui lòng nhập tên khoản thu/ chi.");
            EntryNameTextBox.Focus();
            return;
        }

        if (!TryParseAmount(AmountTextBox.Text, out var amount) || amount <= 0)
        {
            ShowValidationMessage("Vui lòng nhập số tiền hợp lệ.");
            AmountTextBox.Focus();
            AmountTextBox.SelectAll();
            return;
        }

        SubmittedName = entryName;
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
