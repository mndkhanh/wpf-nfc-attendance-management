using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using WPF.Models;
using WPF.Services;
using WPF.Windows;

namespace WPF.Pages;

public partial class MembersPage : UserControl
{
    private List<Student> _allMembers = [];

    public MembersPage()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        LoadMembers();
        UpdateSelectionState();
    }

    private void LoadMembers()
    {
        try
        {
            using var db = new WpfclubManagementDbContext();
            _allMembers = db.Students
                .AsNoTracking()
                .OrderByDescending(s => s.CreatedAt)
                .ToList();

            ApplyFilter();
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

    private void EditMemberButton_Click(object sender, RoutedEventArgs e)
    {
        OpenEditDialogForSelectedMember();
    }

    private void DeleteMemberButton_Click(object sender, RoutedEventArgs e)
    {
        if (MembersGrid.SelectedItem is not Student selectedMember)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Bạn có chắc chắn muốn xóa thành viên '{selectedMember.FullName}'?\nLưu ý: lịch sử điểm danh liên quan có thể bị xóa theo ràng buộc database.",
            "Xác nhận xóa",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            using var db = new WpfclubManagementDbContext();
            var student = db.Students.FirstOrDefault(s => s.StudentId == selectedMember.StudentId);
            if (student is null)
            {
                MessageBox.Show("Không tìm thấy thành viên cần xóa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadMembers();
                return;
            }

            db.Students.Remove(student);
            db.SaveChanges();
            LoadMembers();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không xóa được thành viên: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        SearchTextBox.Clear();
        LoadMembers();
    }

    private void MembersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateSelectionState();
    }

    private void MembersGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        OpenEditDialogForSelectedMember();
    }

    private void ApplyFilter()
    {
        var filteredMembers = StudentCrudHelper.FilterStudents(_allMembers, SearchTextBox.Text);
        MembersGrid.ItemsSource = filteredMembers;
        MemberCountTextBlock.Text = BuildMemberCountText(filteredMembers.Count, _allMembers.Count);

        if (MembersGrid.SelectedItem is Student selectedMember &&
            filteredMembers.All(member => member.StudentId != selectedMember.StudentId))
        {
            MembersGrid.SelectedItem = null;
        }

        UpdateSelectionState();
    }

    private void OpenEditDialogForSelectedMember()
    {
        if (MembersGrid.SelectedItem is not Student selectedMember)
        {
            return;
        }

        var dialog = new AddMemberWindow(selectedMember.StudentId)
        {
            Owner = Window.GetWindow(this),
        };

        if (dialog.ShowDialog() == true)
        {
            LoadMembers();
        }
    }

    private void UpdateSelectionState()
    {
        var selectedMember = MembersGrid.SelectedItem as Student;
        var hasSelection = selectedMember is not null;

        EditMemberButton.IsEnabled = hasSelection;
        DeleteMemberButton.IsEnabled = hasSelection;
        SelectionStatusTextBlock.Text = hasSelection
            ? $"Đang chọn: {selectedMember!.FullName} ({selectedMember.StudentCode})"
            : "Chọn 1 thành viên để sửa hoặc xóa";
    }

    private static string BuildMemberCountText(int filteredCount, int totalCount)
    {
        return filteredCount == totalCount
            ? $"{totalCount} thành viên hiện có trong câu lạc bộ"
            : $"Hiển thị {filteredCount}/{totalCount} thành viên";
    }
}
