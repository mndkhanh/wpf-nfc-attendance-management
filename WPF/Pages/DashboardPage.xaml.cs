using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using WPF.Models;

namespace WPF.Pages;

public partial class DashboardPage : UserControl
{
    public DashboardPage()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        LoadDashboardData();
    }

    private void LoadDashboardData()
    {
        try
        {
            using var db = new WpfclubManagementDbContext();

            // Calculate Metrics
            var totalStudents = db.Students.Count();
            var activeSessions = db.AttendanceSessions.Count(s => s.Status == "Active");
            
            var totalIncome = db.FinancialTransactions
                .Where(t => t.TransactionType == "Thu")
                .Sum(t => (double)t.Amount);
            
            var totalExpense = db.FinancialTransactions
                .Where(t => t.TransactionType == "Chi")
                .Sum(t => (double)t.Amount);

            var balance = totalIncome - totalExpense;

            var metrics = new List<object>
            {
                new { Title = "Tổng số hội viên", Value = totalStudents.ToString(), Subtitle = "Sinh viên đang tham gia" },
                new { Title = "Buổi tập đang mở", Value = activeSessions.ToString(), Subtitle = "Chờ quét thẻ NFC" },
                new { Title = "Số dư quỹ (VND)", Value = balance.ToString("N0"), Subtitle = "Tổng thu - Tổng chi" },
                new { Title = "Giao dịch tháng", Value = db.FinancialTransactions.Count(t => t.TransactionDate.Month == DateTime.Now.Month).ToString(), Subtitle = "Trong tháng này" }
            };

            MetricItemsControl.ItemsSource = metrics;

            // Load Latest Sessions
            var latestSessions = db.AttendanceSessions
                .OrderByDescending(s => s.StartTime)
                .Take(5)
                .ToList();

            DashboardSessionsGrid.ItemsSource = latestSessions;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không tải được dữ liệu Dashboard: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
