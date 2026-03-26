using System.Windows.Controls;
using WPF.Models;

namespace WPF.Pages;

public partial class DashboardPage : UserControl
{
    public DashboardPage()
    {
        InitializeComponent();
        MetricItemsControl.ItemsSource = SampleData.GetDashboardMetrics();
        DashboardSessionsGrid.ItemsSource = SampleData.GetSessions();
    }
}
