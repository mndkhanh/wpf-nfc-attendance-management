using System.Windows;

namespace WPF.Windows;

public partial class FinanceEntryWindow : Window
{
    public FinanceEntryWindow(string titleText)
    {
        InitializeComponent();
        DialogTitleText.Text = titleText;
        Title = titleText;
    }
}
