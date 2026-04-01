using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WPF.Models;
using WPF.Windows;

namespace WPF.Pages;

public partial class FinancePage : UserControl
{
    private const string IncomeType = "Thu";
    private const string ExpenseType = "Chi";

    private readonly ObservableCollection<FinanceItem> _financeItems;
    private decimal _currentBalance;

    public FinancePage()
    {
        InitializeComponent();

        _financeItems = [];
        FinanceGrid.ItemsSource = _financeItems;
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        LoadFinanceData();
    }

    private void AddIncomeButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new FinanceEntryWindow("Tạo khoản thu")
        {
            Owner = Window.GetWindow(this),
        };

        if (dialog.ShowDialog() == true)
        {
            AddTransactionToDatabase(
                transactionType: IncomeType,
                category: "Khoản thu",
                description: dialog.SubmittedName,
                amount: dialog.SubmittedAmount,
                studentId: null);
        }
    }

    private void AddExpenseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new FinanceEntryWindow("Tạo khoản chi")
        {
            Owner = Window.GetWindow(this),
        };

        if (dialog.ShowDialog() == true)
        {
            AddTransactionToDatabase(
                transactionType: ExpenseType,
                category: "Khoản chi",
                description: dialog.SubmittedName,
                amount: dialog.SubmittedAmount,
                studentId: null);
        }
    }

    private void AddMemberFeeButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new MemberFeeWindow
        {
            Owner = Window.GetWindow(this),
        };

        if (dialog.ShowDialog() == true)
        {
            foreach (var student in dialog.SelectedStudents)
            {
                AddTransactionToDatabase(
                    transactionType: IncomeType,
                    category: "Phí thành viên",
                    description: $"{dialog.SubmittedFeeName} - {student.DisplayName}",
                    amount: dialog.SubmittedAmount,
                    transactionDate: null, // Fee creation date defaults to Now
                    studentId: student.StudentId,
                    paymentStatus: "Unpaid");
            }
        }
    }

    private void LoadFinanceData()
    {
        try
        {
            using var db = new WpfclubManagementDbContext();
            var transactions = db.FinancialTransactions
                .AsNoTracking()
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.TransactionId)
                .ToList();

            _financeItems.Clear();

            foreach (var transaction in transactions)
            {
                _financeItems.Add(CreateFinanceItem(transaction));
            }

            _currentBalance = transactions.Sum(GetSignedAmount);
            RefreshRecentTransactions();
            UpdateTotalBalance();
        }
        catch (Exception ex)
        {
            _financeItems.Clear();
            _currentBalance = 0m;
            RefreshRecentTransactions();
            UpdateTotalBalance();

            var owner = Window.GetWindow(this);
            if (owner != null)
            {
                MessageBox.Show(
                    owner,
                    $"Không tải được dữ liệu tài chính từ cơ sở dữ liệu.\n{ex.Message}",
                    "Lỗi dữ liệu tài chính",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            else
            {
                MessageBox.Show(
                    $"Không tải được dữ liệu tài chính từ cơ sở dữ liệu.\n{ex.Message}",
                    "Lỗi dữ liệu tài chính",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    private void AddTransactionToDatabase(string transactionType, string category, string description, decimal amount, int? studentId, DateTime? transactionDate = null, string paymentStatus = "Paid")
    {
        try
        {
            using var db = new WpfclubManagementDbContext();

            var transaction = new FinancialTransaction
            {
                StudentId = studentId,
                TransactionType = transactionType,
                Category = category,
                Amount = amount,
                TransactionDate = transactionDate ?? DateTime.Now,
                Description = description,
                PaymentStatus = paymentStatus,
            };

            db.FinancialTransactions.Add(transaction);
            db.SaveChanges();

            _financeItems.Insert(0, CreateFinanceItem(transaction));
            _currentBalance += GetSignedAmount(transaction);

            RefreshRecentTransactions();
            UpdateTotalBalance();
        }
        catch (Exception ex)
        {
            var owner = Window.GetWindow(this);
            if (owner != null)
            {
                MessageBox.Show(
                    owner,
                    $"Không lưu được giao dịch tài chính.\n{ex.Message}",
                    "Lỗi lưu dữ liệu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            else
            {
                MessageBox.Show(
                    $"Không lưu được giao dịch tài chính.\n{ex.Message}",
                    "Lỗi lưu dữ liệu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    private static FinanceItem CreateFinanceItem(FinancialTransaction transaction)
    {
        var isIncome = IsIncomeTransaction(transaction.TransactionType);
        var description = string.IsNullOrWhiteSpace(transaction.Description)
            ? transaction.Category
            : transaction.Description;

        var note = (transaction.StudentId != null && transaction.PaymentStatus == "Unpaid")
            ? "Sinh viên chưa hoàn thành nghĩa vụ tài chính"
            : string.Empty;

        return new FinanceItem
        {
            TransactionId   = transaction.TransactionId,
            TransactionDate = transaction.TransactionDate.ToString("dd/MM/yyyy"),
            Category        = transaction.Category,
            Description     = description,
            AmountDisplay   = FormatSignedAmount(transaction.Amount, isIncome),
            AmountBrush     = isIncome ? "#2E9E46" : "#E24537",
            PaymentStatus   = transaction.PaymentStatus,
            Note            = note,
        };
    }

    private static decimal GetSignedAmount(FinancialTransaction transaction)
    {
        return IsIncomeTransaction(transaction.TransactionType)
            ? transaction.Amount
            : -transaction.Amount;
    }

    private static bool IsIncomeTransaction(string? transactionType)
    {
        return string.Equals(transactionType?.Trim(), IncomeType, StringComparison.OrdinalIgnoreCase);
    }

    private void RefreshRecentTransactions()
    {
    }

    private void UpdateTotalBalance()
    {
        TotalBalanceRun.Text = FormatSignedAmount(Math.Abs(_currentBalance), _currentBalance >= 0);
        TotalBalanceRun.Foreground = _currentBalance >= 0
            ? (Brush)FindResource("SuccessBrush")
            : (Brush)FindResource("DangerBrush");
    }

    private static string FormatSignedAmount(decimal amount, bool isPositive)
    {
        var formattedAmount = amount.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", ".");
        return $"{(isPositive ? "+" : "-")} {formattedAmount}";
    }
    private void FinanceGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (FinanceGrid.SelectedItem is not FinanceItem selectedItem) return;

        var dialog = new FinanceEditWindow(selectedItem.TransactionId)
        {
            Owner = Window.GetWindow(this),
        };

        dialog.ShowDialog();

        if (dialog.DataChanged)
        {
            LoadFinanceData();
        }
    }
}
