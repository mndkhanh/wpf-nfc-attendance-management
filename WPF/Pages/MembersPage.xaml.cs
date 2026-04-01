using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using WPF.Models;
using WPF.Windows;

namespace WPF.Pages;

public partial class MembersPage : UserControl
{
    public MembersPage()
    {
        InitializeComponent();
        LoadMembers();
    }

    private void LoadMembers()
    {
        try
        {
            using var db = new WpfclubManagementDbContext();
            var members = db.Students
                .AsNoTracking()
                .OrderByDescending(s => s.CreatedAt)
                .ToList();

            MembersGrid.ItemsSource = members;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không tải được danh sách thành viên: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddMemberButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddMemberWindow
        {
            Owner = Window.GetWindow(this),
        };

        if (dialog.ShowDialog() == true)
        {
            LoadMembers();
        }
    }
}
