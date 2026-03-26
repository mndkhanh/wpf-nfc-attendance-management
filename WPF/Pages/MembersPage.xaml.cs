using System.Windows;
using System.Windows.Controls;
using WPF.Models;
using WPF.Windows;

namespace WPF.Pages;

public partial class MembersPage : UserControl
{
    public MembersPage()
    {
        InitializeComponent();
        MembersGrid.ItemsSource = SampleData.GetMembers();
    }

    private void AddMemberButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddMemberWindow
        {
            Owner = Window.GetWindow(this),
        };

        dialog.ShowDialog();
    }
}
