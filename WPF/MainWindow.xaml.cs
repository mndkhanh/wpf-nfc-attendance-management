using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WPF.Pages;

namespace WPF;

public partial class MainWindow : Window
{
    private readonly DashboardPage _dashboardPage;
    private readonly MembersPage _membersPage;
    private readonly AttendancePage _attendancePage;
    private readonly FinancePage _financePage;
    private readonly SettingsPage _settingsPage;

    public MainWindow()
    {
        InitializeComponent();

        _dashboardPage = new DashboardPage();
        _membersPage = new MembersPage();
        _attendancePage = new AttendancePage();
        _financePage = new FinancePage();
        _settingsPage = new SettingsPage();

        ShowPage("Members");
    }

    private void AttendanceNavButton_Click(object sender, RoutedEventArgs e) => ShowPage("Attendance");

    private void MembersNavButton_Click(object sender, RoutedEventArgs e) => ShowPage("Members");

    private void FinanceNavButton_Click(object sender, RoutedEventArgs e) => ShowPage("Finance");

    private void DashboardNavButton_Click(object sender, RoutedEventArgs e) => ShowPage("Dashboard");

    private void SettingsNavButton_Click(object sender, RoutedEventArgs e) => ShowPage("Settings");

    private void ShowPage(string pageKey)
    {
        MainContentHost.Content = pageKey switch
        {
            "Attendance" => _attendancePage,
            "Finance" => _financePage,
            "Dashboard" => _dashboardPage,
            "Settings" => _settingsPage,
            _ => _membersPage,
        };

        UpdateNavigationState(pageKey);
    }

    private void UpdateNavigationState(string activePage)
    {
        UpdateButtonState(AttendanceNavButton, activePage == "Attendance");
        UpdateButtonState(MembersNavButton, activePage == "Members");
        UpdateButtonState(FinanceNavButton, activePage == "Finance");
        UpdateButtonState(DashboardNavButton, activePage == "Dashboard");
        UpdateButtonState(SettingsNavButton, activePage == "Settings");
    }

    private void UpdateButtonState(Button button, bool isActive)
    {
        button.Background = isActive
            ? (Brush)FindResource("SidebarActiveBrush")
            : Brushes.Transparent;
    }
}
