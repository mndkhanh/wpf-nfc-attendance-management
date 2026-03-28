using System.Windows;
using System.Windows.Controls;
using WPF.Models;
using WPF.Windows;

namespace WPF.Pages;

public partial class AttendancePage : UserControl
{
    public AttendancePage()
    {
        InitializeComponent();
        SessionsGrid.ItemsSource = SampleData.GetSessions();
        AttendanceGrid.ItemsSource = SampleData.GetAttendanceRecords();
    }

    private void StartSessionButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SessionEditorWindow
        {
            Owner = Window.GetWindow(this),
        };

        dialog.ShowDialog();
    }
}
