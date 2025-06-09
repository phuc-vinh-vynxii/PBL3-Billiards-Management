# Hệ thống Quản lý Phòng Bi-a

## Chức năng Quản lý Ca Làm Việc (Mới thêm)

### Mô tả

Chức năng quản lý ca làm việc cho phép Manager quản lý lịch làm việc của nhân viên theo tuần, tạo các ca làm việc và phân công nhân viên vào từng ca cụ thể.

### Tính năng chính:

1. **Quản lý Ca làm việc:**

   - Tạo ca làm việc mới với tên, giờ bắt đầu, giờ kết thúc
   - Chỉnh sửa thông tin ca làm việc
   - Xóa ca làm việc (soft delete)

2. **Lịch tuần:**

   - Hiển thị lịch làm việc theo tuần (từ Thứ 2 đến Chủ nhật)
   - Điều hướng giữa các tuần
   - Xem tổng quan ca làm việc của tất cả nhân viên

3. **Phân công ca:**

   - Phân công nhân viên vào ca làm việc cụ thể cho từng ngày
   - Hủy phân công ca làm việc
   - Phân công hàng loạt cho nhiều ngày

4. **Giao diện:**
   - Bảng lịch trực quan hiển thị ca làm việc theo tuần
   - Màu sắc khác nhau cho ca đã phân công và ca trống
   - Thao tác nhanh assign/unassign bằng nút click

### Cấu trúc Database:

#### Bảng Shifts

- ShiftId (PK)
- ShiftName (tên ca)
- StartTime (giờ bắt đầu)
- EndTime (giờ kết thúc)
- Description (mô tả)
- IsActive (trạng thái)
- CreatedAt (ngày tạo)

#### Bảng ShiftAssignments

- ShiftAssignmentId (PK)
- EmployeeId (FK to Employee)
- ShiftId (FK to Shift)
- AssignedDate (ngày được phân công)
- DayOfWeek (thứ trong tuần)
- Notes (ghi chú)
- IsActive (trạng thái)
- CreatedAt (ngày tạo)
- CreatedBy (FK to Employee - người phân công)

### Cấu trúc Code:

1. **Models:**

   - `Models/Entities/Shift.cs` - Entity cho ca làm việc
   - `Models/Entities/ShiftAssignment.cs` - Entity cho phân công ca
   - `Models/ViewModels/ShiftManagementViewModel.cs` - ViewModel cho trang quản lý

2. **Services:**

   - `Services/IShiftService.cs` - Interface
   - `Services/ShiftService.cs` - Business logic

3. **Controllers:**

   - `Controllers/Manager/ManagerController.cs` - Thêm các action methods

4. **Views:**
   - `Views/Manager/ShiftManagement.cshtml` - Giao diện quản lý ca

### Cách sử dụng:

1. **Truy cập:** Manager Dashboard → Quản lý ca làm việc
2. **Tạo ca:** Click "Tạo ca mới" → Điền thông tin → Lưu
3. **Phân công:** Click nút "+" trên ô trống để phân công nhân viên
4. **Hủy phân công:** Click nút "x" trên ca đã phân công
5. **Chỉnh sửa ca:** Click icon "Pencil" trên card ca làm việc
6. **Xóa ca:** Click icon "Trash" trên card ca làm việc

### Migration:

```bash
dotnet ef migrations add AddShiftManagement
dotnet ef database update
```
