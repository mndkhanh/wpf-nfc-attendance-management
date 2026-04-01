using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using FlashCap;
using FlashCap.Devices;
using WPF.Models;
using WPF.Services;

namespace WPF.Windows;

public partial class AddMemberWindow : Window
{
    private readonly int? _studentId;
    private string? _selectedPhotoSourceFilePath;
    private string? _currentStoredPhotoPath;
    private bool _removeExistingPhoto;

    private CaptureDevice? _captureDevice;
    private bool _isCameraActive;

    private CancellationTokenSource? _nfcCancel;
    private const string FirebaseUrl = "https://wpf-nfc-attendance-management-default-rtdb.asia-southeast1.firebasedatabase.app";
    private readonly Brush _normalTextBrush = new SolidColorBrush(Color.FromRgb(53, 47, 47));
    private readonly Brush _accentBrush = new SolidColorBrush(Color.FromRgb(0, 120, 215));

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

        this.Closing += (s, e) => 
        {
            StopCamera();
            StopNfcScan();
        };
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

    private async void CameraModeButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var devices = new CaptureDevices();
            var firstDevice = devices.EnumerateDescriptors().FirstOrDefault();

            if (firstDevice == null)
            {
                MessageBox.Show("Không tìm thấy Camera nào trên thiết bị.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var characteristics = firstDevice.Characteristics.FirstOrDefault(c => c.PixelFormat == FlashCap.PixelFormats.JPEG)
                                  ?? firstDevice.Characteristics.FirstOrDefault(c => c.PixelFormat == FlashCap.PixelFormats.PNG)
                                  ?? firstDevice.Characteristics.OrderByDescending(c => c.Width).FirstOrDefault();

            if (characteristics == null)
            {
                MessageBox.Show("Không hỗ trợ định dạng Camera này.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _captureDevice = await firstDevice.OpenAsync(characteristics, OnFrameArrived);
            await _captureDevice.StartAsync();

            _isCameraActive = true;
            UpdateUiForCameraMode(true);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi khởi động Camera: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            StopCamera();
        }
    }

    private void CancelCameraButton_Click(object sender, RoutedEventArgs e)
    {
        StopCamera();
    }

    private void CaptureButton_Click(object sender, RoutedEventArgs e)
    {
        if (_captureDevice == null || CameraPreviewImage.Source is not BitmapSource bitmapSource) return;

        try
        {
            var encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

            var tempPath = Path.Combine(Path.GetTempPath(), $"capture_{Guid.NewGuid():N}.jpg");
            using (var stream = File.OpenWrite(tempPath))
            {
                encoder.Save(stream);
            }

            _selectedPhotoSourceFilePath = tempPath;
            _removeExistingPhoto = false;
            UpdatePhotoPreview(tempPath);
            StopCamera();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi chụp ảnh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnFrameArrived(PixelBufferScope scope)
    {
        if (!_isCameraActive) return;

        try
        {
            // IMPORTANT: Extract the data IMMEDIATELY before the scope is disposed
            var imageSource = scope.Buffer.ExtractImage();

            Dispatcher.BeginInvoke(() =>
            {
                if (!_isCameraActive) return;
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = new MemoryStream(imageSource);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    CameraPreviewImage.Source = bitmap;
                }
                catch { /* Ignore UI update errors during shutdown */ }
            });
        }
        catch { /* Ignore extraction errors during shutdown */ }
    }

    private void StopCamera()
    {
        _isCameraActive = false;
        _captureDevice?.Dispose();
        _captureDevice = null;
        Dispatcher.Invoke(() => UpdateUiForCameraMode(false));
    }

    private void UpdateUiForCameraMode(bool isActive)
    {
        CameraPreviewGrid.Visibility = isActive ? Visibility.Visible : Visibility.Collapsed;
        NormalPhotoGrid.Visibility = isActive ? Visibility.Collapsed : Visibility.Visible;
        
        SelectPhotoButton.IsEnabled = !isActive;
        CameraModeButton.IsEnabled = !isActive;
        RemovePhotoButton.IsEnabled = !isActive && (!string.IsNullOrWhiteSpace(_selectedPhotoSourceFilePath) || !string.IsNullOrWhiteSpace(_currentStoredPhotoPath));
        CancelCameraButton.Visibility = isActive ? Visibility.Visible : Visibility.Collapsed;
        
        ConfirmButton.IsEnabled = !isActive;
    }

    private void ScanNfcButton_Click(object sender, RoutedEventArgs e)
    {
        if (_nfcCancel != null)
        {
            StopNfcScan();
            return;
        }

        StartNfcScan();
    }

    private void EraseNfcButton_Click(object sender, RoutedEventArgs e)
    {
        StopNfcScan();
        NfcUidTextBox.Text = string.Empty;
    }

    private void StartNfcScan()
    {
        _nfcCancel = new CancellationTokenSource();
        Task.Run(() => ListenNfcSse(_nfcCancel.Token));
        
        NfcUidTextBox.Background = new SolidColorBrush(Color.FromRgb(232, 244, 253));
        NfcUidTextBox.Text = "Đang chờ thẻ NFC...";
        ScanNfcButton.Background = _accentBrush;
        ((TextBlock)ScanNfcButton.Content).Foreground = Brushes.White;
    }

    private void StopNfcScan()
    {
        _nfcCancel?.Cancel();
        _nfcCancel = null;

        Dispatcher.Invoke(() =>
        {
            NfcUidTextBox.Background = new SolidColorBrush(Color.FromRgb(240, 241, 253));
            ScanNfcButton.Background = Brushes.Transparent;
            ((TextBlock)ScanNfcButton.Content).Foreground = _normalTextBrush;
            
            if (NfcUidTextBox.Text == "Đang chờ thẻ NFC...")
            {
                NfcUidTextBox.Text = string.Empty;
            }
        });
    }

    private async Task ListenNfcSse(CancellationToken token)
    {
        using var http = new HttpClient();
        http.Timeout = Timeout.InfiniteTimeSpan;

        while (!token.IsCancellationRequested)
        {
            try
            {
                // Erase any old data before starting the new stream
                _ = await http.DeleteAsync($"{FirebaseUrl}/current_uid.json", token);

                using var request = new HttpRequestMessage(HttpMethod.Get, $"{FirebaseUrl}/current_uid.json");
                request.Headers.Add("Accept", "text/event-stream");

                using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
                using var stream = await response.Content.ReadAsStreamAsync(token);
                using var reader = new StreamReader(stream);

                string? eventType = null;

                while (!token.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null) break;

                    if (line.StartsWith("event:"))
                    {
                        eventType = line[6..].Trim();
                    }
                    else if (line.StartsWith("data:") && eventType == "put")
                    {
                        var raw = line[5..].Trim();

                        try
                        {
                            using var doc = JsonDocument.Parse(raw);
                            var dataEl = doc.RootElement.GetProperty("data");

                            string? uid = dataEl.ValueKind == JsonValueKind.String
                                ? dataEl.GetString()
                                : null;

                            if (!string.IsNullOrWhiteSpace(uid))
                            {
                                _ = http.DeleteAsync($"{FirebaseUrl}/current_uid.json", token);
                                Dispatcher.Invoke(() =>
                                {
                                    NfcUidTextBox.Text = uid!;
                                    System.Media.SystemSounds.Beep.Play();
                                    StopNfcScan();
                                });
                            }
                        }
                        catch { /* ignore malformed events */ }

                        eventType = null;
                    }
                }
            }
            catch (Exception) when (!token.IsCancellationRequested)
            {
                await Task.Delay(2000, token);
            }
        }
    }

    private void ConfigureWindowMode()
    {
        var isEditMode = _studentId.HasValue;
        Title = isEditMode ? "Cập nhật thành viên" : "Đăng ký thành viên";
        WindowHeaderTextBlock.Text = isEditMode ? "Cập nhật thành viên" : "Đăng ký thành viên mới";
        ConfirmButton.Content = isEditMode ? "Lưu" : "Xác nhận";

        if (isEditMode)
        {
            Width = 1100;
            FinanceColumnDef.Width = new GridLength(1, GridUnitType.Star);
            FinanceHistoryPanel.Visibility = Visibility.Visible;
        }
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

            LoadStudentTransactions(studentId);
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

    // ──────────────────────────────────────────────────────
    //  Financial transaction history
    // ──────────────────────────────────────────────────────

    private void LoadStudentTransactions(int studentId)
    {
        try
        {
            using var db = new WpfclubManagementDbContext();
            var transactions = db.FinancialTransactions
                .AsNoTracking()
                .Where(t => t.StudentId == studentId)
                .OrderByDescending(t => t.TransactionDate)
                .ToList();

            UnpaidTransactionsGrid.ItemsSource = transactions
                .Where(t => t.PaymentStatus == "Unpaid")
                .ToList();

            PaidTransactionsGrid.ItemsSource = transactions
                .Where(t => t.PaymentStatus != "Unpaid")
                .ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải lịch sử tài chính: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MarkAsPaidButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn) return;
        if (btn.DataContext is not FinancialTransaction tx) return;

        try
        {
            using var db = new WpfclubManagementDbContext();
            var dbTx = db.FinancialTransactions.Find(tx.TransactionId);
            if (dbTx == null) return;

            dbTx.PaymentStatus = "Paid";
            dbTx.TransactionDate = DateTime.Now;
            db.SaveChanges();

            if (_studentId.HasValue)
            {
                LoadStudentTransactions(_studentId.Value);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi cập nhật trạng thái: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
