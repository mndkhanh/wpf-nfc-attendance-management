using System.Windows;
using System.Windows.Controls;
using WPF.Models;
using WPF.Windows;

namespace WPF.Pages;

public partial class FinancePage : UserControl
{
    public FinancePage()
    {
        InitializeComponent();
        FinanceGrid.ItemsSource = SampleData.GetFinanceItems();
    }

    private void AddIncomeButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new FinanceEntryWindow("Tạo khoản thu")
        {
            Owner = Window.GetWindow(this),
        };

        dialog.ShowDialog();
    }

    private void AddExpenseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new FinanceEntryWindow("Tạo khoản chi")
        {
            Owner = Window.GetWindow(this),
        };

        dialog.ShowDialog();
    }

    private void AddMemberFeeButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new MemberFeeWindow
        {
            Owner = Window.GetWindow(this),
        };

        dialog.ShowDialog();
    }
}
