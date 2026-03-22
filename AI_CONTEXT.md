# Dự án Quản lý Điểm danh và Tài chính Câu lạc bộ (NFC & Firebase)

## 1. Tổng quan dự án (Project Overview)
Hệ thống phần mềm hỗ trợ quản lý toàn diện cho **một Câu lạc bộ (CLB) duy nhất**, bao gồm quản lý thành viên, theo dõi điểm danh bằng thẻ NFC và quản lý tài chính (thu chi). 
*Lưu ý cốt lõi: Ứng dụng hướng tới sự đơn giản tuyệt đối, mở lên là dùng, **không có hệ thống tài khoản**, **không yêu cầu đăng nhập (No Login)***.

## 2. Kiến trúc & Công nghệ (Tech Stack)
- **Desktop App (Main Application):** 
  - **Framework:** Windows Presentation Foundation (WPF) - .NET.
  - **Vai trò:** Quản lý toàn bộ core logic, danh sách sinh viên, và sổ sách tài chính. Xử lý nghiệp vụ điểm danh theo Buổi Tập (Session) và cung cấp giao diện quản trị cho Ban chủ nhiệm CLB duyệt thông tin trực tiếp (không yêu cầu đăng nhập).
- **Mobile App (NFC Scanner):**
  - **Framework:** Flutter (target nền tảng Android).
  - **Vai trò:** Đóng vai trò như một máy quét (scanner) di động: Tận dụng cảm biến NFC trên điện thoại Android để quét thẻ của sinh viên và đẩy mã thẻ lên đám mây.
- **Cơ sở dữ liệu chính (Main Database):**
  - **Hệ quản trị:** SQL Server (WPFClubManagementDB).
  - **Vai trò:** Nơi lưu trữ vĩnh viễn và an toàn toàn bộ dữ liệu: Thông tin sinh viên cùng mã NFC UID tương ứng, hệ thống Session điểm danh, và lịch sử giao dịch thu chi.
- **Cầu nối dữ liệu Realtime (Realtime Bridge):**
  - **Dịch vụ:** Firebase Realtime Database.
  - **Vai trò:** Làm trạm trung chuyển tín hiệu siêu tốc. Điện thoại Flutter ghi UID thẻ vừa quét lên Firebase; phần mềm WPF cài sẵn một listener để "hứng" luồng dữ liệu này ngay lập tức.

## 3. Các tính năng cốt lõi (Core Features)

### 3.1. Quản lý Tài chính (Financial Management)
Hệ thống số hóa quy trình quản lý quỹ của CLB:
- **Các khoản Chi (Expenses):** 
  - Mua trang thiết bị phục vụ tập luyện và thi đấu (áo giáp, thảm, học liệu...).
  - Chi trả thù lao/lương cho Huấn luyện viên (HLV).
- **Các khoản Thu (Incomes):**
  - Thu khác: Ghi nhận tiền tài trợ, nguồn kinh phí hỗ trợ từ các đơn vị, cá nhân.
  - **Phí CLB (Gắn với Sinh viên):** Nếu là thu phí sinh hoạt, giao dịch này sẽ được liên kết trực tiếp với mã sinh viên (`StudentID`) đóng tiền để quản lý chi tiết sinh viên nào đã hoàn thành đóng phí.

### 3.2. Các Luồng Điểm danh với Session (Attendance Flows)
Quy trình điểm danh được **quản lý theo từng Buổi Tập (Session)**.

**Luồng 1: Điểm danh tự động không chạm (Điện thoại Flutter -> Firebase -> Máy tính WPF)**
1. **Khởi tạo (WPF):** Quản lý mở WPF App, tạo buổi tập với Tên, Nội dung và ấn nút **"Bắt đầu buổi tập"**. Khi này WPF mở `SessionID` trong DB và bắt đầu thu tín hiệu NFC từ Firebase.
2. **Quét thẻ (Flutter):** Sinh viên quẹt thẻ NFC vào điện thoại Android.
3. **Truyền tin:** App Flutter đọc tín hiệu NFC UID và gửi đoạn mã này lên **Firebase Realtime Database**.
4. **Phản hồi & Xác thực (WPF thao tác ngầm):** Ứng dụng **WPF App** phát hiện có sinh viên quẹt thẻ thông qua mã UID, sau đó truy vấn trực tiếp vào **SQL Server** để xem mã UID này thuộc về sinh viên nào trong danh sách.
5. **Ghi nhận:** Nếu hợp lệ (KHÔNG trùng trong Session hiện tại), WPF thông báo thành công và lưu bản ghi vào `Attendance` với `SessionID` hiện tại.
6. **Kết thúc (WPF):** Hết giờ tập, quản lý ấn **"Kết thúc buổi tập"**. `SessionID` bị khóa, từ chối mọi thẻ quẹt vào.

**Luồng 2: Điểm danh thủ công trong Session (Manual Flow trên WPF)**
1. **Tình huống:** Trong Buổi Tập, sinh viên quên thẻ hoặc thiết bị NFC trục trặc.
2. **Thao tác:** Ban quản lý sử dụng trực tiếp **WPF App** tìm mã sinh viên và gán thẳng điểm danh vào `SessionID` đang chạy (hoặc đã kết thúc).
3. **Ghi nhận:** Lưu thẻ trực tiếp xuống **SQL Server** thông qua mã sinh viên. Nếu cần làm rõ là điểm danh tay thì quản lý có thể ghi chú vào trường `Note`.

## 4. Định hướng Cấu trúc Database (SQL Server cho 1 CLB)
- **Students Table:** `StudentID`, `FullName`, `NFC_UID`, `Status`, `JoinedDate`...
- **AttendanceSessions Table:** `SessionID`, `SessionName`, `Topic`, `StartTime`, `EndTime`, `Status`. (Lưu trữ Buổi Tập).
- **Attendance Table:** `AttendanceID`, `SessionID`, `StudentID`, `CheckInTime`, `Note` (Ghi chú trực tiếp, hệ thống không phân luồng IsManual nữa).
- **Financial_Transactions Table:** `TransactionID`, `StudentID` (Có thể NULL, liên kết tới Sinh Viên khi đóng phí), `Type` (Thu / Chi), `Category`, `Amount`, `TransactionDate`, `Description`.
*(Lưu ý: Không có bảng Users, không có hệ thống Authentication).*

## 5. Lưu ý Triển khai & Ngữ cảnh cho AI
- **Session-Based Attendance:** Toàn bộ tư duy điểm danh phụ thuộc vào Session (Buổi tập). Phải có hành động tạo buổi tập (Start Session) thì WPF mới bật listener hoặc chấp nhận quẹt NFC hợp lệ để ghi vào CSDL.
- **Không Đăng Nhập (No Login/No Users):** Ứng dụng được thiết kế mở-là-chạy (plug and play), bỏ qua hoàn toàn bước đăng nhập xác thực. Trong DB không có bảng Users và không có khái niệm phân quyền. Tất cả những thao tác trên ứng dụng (điểm danh, thu chi) là trực tiếp.
- **Single Club:** Mọi logic phân quyền theo ClubID, ClubName không tồn tại. Mọi luồng xử lý mặc định hướng tới thao tác trên tập sinh viên của 1 CLB.
- **Vai trò SQL Server vs Firebase:** Toàn bộ nghiệp vụ lưu trữ cấu trúc, truy vấn, làm báo cáo đều **chỉ thực hiện với SQL Server**. **Firebase** đóng vai trò là *bộ đêm tin nhắn (Message Broker)* giữ tín hiệu UID vừa quét.
- **Kiến trúc phần mềm:** WPF nên áp dụng **MVVM** bài bản và dùng ORM (Entity Framework). Nhớ sinh Entity model cho `AttendanceSessions` và thiết kế luồng khóa ngoại cẩn thận. App Flutter tối giản hết mức có thể gồm màn hình kết nối DB và màn chờ trigger sóng NFC.
