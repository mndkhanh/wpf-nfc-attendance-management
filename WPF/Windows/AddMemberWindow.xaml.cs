using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using WPF.Models;
using WPF.Services;

namespace WPF.Windows;

public partial class AddMemberWindow : Window
{
    private readonly int? _studentId;
    private string? _selectedPhotoSourceFilePath;
    private string? _currentStoredPhotoPath;
    private bool _removeExistingPhoto;

    public AddMemberWindow()
        : this(null)
    {
    }

    public AddMemberWindow(int? studentId)
    {
        InitializeComponent();
        _studentId = studentId;

        ConfigureWindowMode();

        if (_studentId.HasValue)
        {
            LoadStudentForEdit(_studentId.GetValueOrDefault());
        }
        else
        {
            UpdatePhotoPreview(null);
        }
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        string? newlyStoredPhotoPath = null;
        string? previousStoredPhotoPath = null;

        try
        {
            using var db = new WpfclubManagementDbContext();
            var isNewStudent = !_studentId.HasValue;
            var now = DateTime.Now;

            Student student;
            if (isNewStudent)
            {
                student = new Student();
                db.Students.Add(student);
            }
            else
            {
                var editingStudentId = _studentId.GetValueOrDefault();
                student = db.Students.FirstOrDefault(s => s.StudentId == editingStudentId)
                    ?? throw new InvalidOperationException("Không tìm thấy thành viên cần cập nhật.");
            }

            previousStoredPhotoPath = student.PhotoPath;

            var formData = new StudentFormData(
                FullNameTextBox.Text,
                StudentCodeTextBox.Text,
                NfcUidTextBox.Text,
                PhoneTextBox.Text,
                EmailTextBox.Text,
                (StatusComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                null);

            ValidateRequiredFields(formData);
            ValidateUniqueFields(db, formData, student.StudentId);

            var finalPhotoPath = previousStoredPhotoPath;
            if (!string.IsNullOrWhiteSpace(_selectedPhotoSourceFilePath))
            {
                newlyStoredPhotoPath = StudentPhotoStorage.SavePhoto(_selectedPhotoSourceFilePath, formData.StudentCode);
                finalPhotoPath = newlyStoredPhotoPath;
            }
            else if (_removeExistingPhoto)
            {
                finalPhotoPath = null;
            }

            StudentCrudHelper.ApplyForm(
                student,
                formData with { PhotoPath = finalPhotoPath },
                student.JoinedDate ?? DateOnly.FromDateTime(now),
                now,
                isNewStudent);

            db.SaveChanges();

            if (!string.IsNullOrWhiteSpace(newlyStoredPhotoPath) &&
                !string.IsNullOrWhiteSpace(previousStoredPhotoPath) &&
                !string.Equals(previousStoredPhotoPath, newlyStoredPhotoPath, StringComparison.OrdinalIgnoreCase))
            {
                StudentPhotoStorage.DeletePhoto(previousStoredPhotoPath);
            }
            else if (_removeExistingPhoto && !string.IsNullOrWhiteSpace(previousStoredPhotoPath))
            {
                StudentPhotoStorage.DeletePhoto(previousStoredPhotoPath);
            }

            DialogResult = true;
            Close();
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(ex.Message, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            if (!string.IsNullOrWhiteSpace(newlyStoredPhotoPath))
            {
                StudentPhotoStorage.DeletePhoto(newlyStoredPhotoPath);
            }

            MessageBox.Show($"Lỗi khi lưu sinh viên: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SelectPhotoButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Chọn ảnh sinh viên",
            Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp|All files|*.*",
            Multiselect = false,
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        _selectedPhotoSourceFilePath = dialog.FileName;
        _removeExistingPhoto = false;
        UpdatePhotoPreview(dialog.FileName);
    }

    private void RemovePhotoButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_selectedPhotoSourceFilePath))
        {
            _selectedPhotoSourceFilePath = null;

            if (!string.IsNullOrWhiteSpace(_currentStoredPhotoPath) && !_removeExistingPhoto)
            {
                UpdatePhotoPreview(StudentPhotoStorage.ResolvePhotoPath(_currentStoredPhotoPath));
            }
            else
            {
                UpdatePhotoPreview(null);
            }

            return;
        }

        _removeExistingPhoto = !string.IsNullOrWhiteSpace(_currentStoredPhotoPath);
        UpdatePhotoPreview(null);
    }

    private void ConfigureWindowMode()
    {
        var isEditMode = _studentId.HasValue;
        Title = isEditMode ? "Cập nhật thành viên" : "Đăng ký thành viên";
        WindowHeaderTextBlock.Text = isEditMode ? "Cập nhật thành viên" : "Đăng ký thành viên mới";
        ConfirmButton.Content = isEditMode ? "Lưu" : "Xác nhận";
    }

    private void LoadStudentForEdit(int studentId)
    {
        try
        {
            using var db = new WpfclubManagementDbContext();
            var student = db.Students
                .AsNoTracking()
                .FirstOrDefault(s => s.StudentId == studentId);

            if (student is null)
            {
                MessageBox.Show("Không tìm thấy thành viên cần chỉnh sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = false;
                Close();
                return;
            }

            FullNameTextBox.Text = student.FullName;
            StudentCodeTextBox.Text = student.StudentCode;
            NfcUidTextBox.Text = student.NfcUid ?? string.Empty;
            PhoneTextBox.Text = student.Phone ?? string.Empty;
            EmailTextBox.Text = student.Email ?? string.Empty;
            _currentStoredPhotoPath = student.PhotoPath;
            SelectStatus(student.Status);
            UpdatePhotoPreview(StudentPhotoStorage.ResolvePhotoPath(student.PhotoPath));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không tải được dữ liệu thành viên: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            DialogResult = false;
            Close();
        }
    }

    private void SelectStatus(string? status)
    {
        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? "Active" : status.Trim();

        foreach (var item in StatusComboBox.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Content?.ToString(), normalizedStatus, StringComparison.OrdinalIgnoreCase))
            {
                StatusComboBox.SelectedItem = item;
                return;
            }
        }

        StatusComboBox.SelectedIndex = 0;
    }

    private void ValidateRequiredFields(StudentFormData formData)
    {
        if (string.IsNullOrWhiteSpace(formData.FullName))
        {
            FullNameTextBox.Focus();
            throw new ArgumentException("Vui lòng nhập họ và tên.");
        }

        if (string.IsNullOrWhiteSpace(formData.StudentCode))
        {
            StudentCodeTextBox.Focus();
            throw new ArgumentException("Vui lòng nhập MSSV.");
        }
    }

    private void ValidateUniqueFields(WpfclubManagementDbContext db, StudentFormData formData, int currentStudentId)
    {
        var normalizedStudentCode = formData.StudentCode.Trim();
        var normalizedNfcUid = string.IsNullOrWhiteSpace(formData.NfcUid) ? null : formData.NfcUid.Trim();

        var duplicateStudentCode = db.Students.Any(s =>
            s.StudentId != currentStudentId &&
            s.StudentCode == normalizedStudentCode);

        if (duplicateStudentCode)
        {
            StudentCodeTextBox.Focus();
            StudentCodeTextBox.SelectAll();
            throw new ArgumentException("MSSV này đã tồn tại.");
        }

        if (!string.IsNullOrEmpty(normalizedNfcUid))
        {
            var duplicateNfcUid = db.Students.Any(s =>
                s.StudentId != currentStudentId &&
                s.NfcUid == normalizedNfcUid);

            if (duplicateNfcUid)
            {
                NfcUidTextBox.Focus();
                NfcUidTextBox.SelectAll();
                throw new ArgumentException("Mã NFC UID này đã được gán cho thành viên khác.");
            }
        }
    }

    private void UpdatePhotoPreview(string? absolutePhotoPath)
    {
        if (!string.IsNullOrWhiteSpace(absolutePhotoPath) && File.Exists(absolutePhotoPath))
        {
            StudentPhotoImage.Source = LoadBitmapImage(absolutePhotoPath);
            PhotoPlaceholderPanel.Visibility = Visibility.Collapsed;
            RemovePhotoButton.IsEnabled = true;
            return;
        }

        StudentPhotoImage.Source = null;
        PhotoPlaceholderPanel.Visibility = Visibility.Visible;
        RemovePhotoButton.IsEnabled =
            !string.IsNullOrWhiteSpace(_selectedPhotoSourceFilePath) ||
            (!string.IsNullOrWhiteSpace(_currentStoredPhotoPath) && !_removeExistingPhoto);
    }

    private static BitmapImage LoadBitmapImage(string absolutePhotoPath)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(absolutePhotoPath, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
}
