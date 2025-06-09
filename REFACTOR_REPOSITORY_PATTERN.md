# Refactor Database Logic với Repository Pattern

## Tổng quan

Đã thực hiện refactor để tách logic truy vấn database từ các Controller sang các Repository class, tuân theo Repository Pattern để tăng tính tái sử dụng và dễ bảo trì.

## Các Repository đã tạo

### 1. IEmployeeRepository & EmployeeRepository

**File**: `Repositories/IEmployeeRepository.cs`, `Repositories/EmployeeRepository.cs`

**Chức năng**:

- Quản lý các thao tác với bảng Employee
- GetByIdAsync, GetByUsernameAsync, GetByUsernameAndPasswordAsync
- GetAllAsync, GetByPositionAsync, GetNonManagersAsync
- GetCountAsync, IsEmailExistsAsync, IsUsernameExistsAsync
- CreateAsync, UpdateAsync, DeleteAsync

### 2. ITableRepository & TableRepository

**File**: `Repositories/ITableRepository.cs`, `Repositories/TableRepository.cs`

**Chức năng**:

- Quản lý các thao tác với bảng Table
- GetByIdAsync, GetAllAsync, GetWithActiveSessionsAsync
- GetByStatusAsync, GetCountAsync, GetCountByStatusAsync
- CreateAsync, UpdateAsync, DeleteAsync

### 3. IInvoiceRepository & InvoiceRepository

**File**: `Repositories/IInvoiceRepository.cs`, `Repositories/InvoiceRepository.cs`

**Chức năng**:

- Quản lý các thao tác với bảng Invoice
- GetByIdAsync, GetByIdWithDetailsAsync, GetBySessionIdAsync
- GetAllAsync, GetByStatusAsync, GetPendingWithActiveSessionsAsync
- GetByDateRangeAsync, GetTodayInvoicesAsync, GetRecentInvoicesAsync
- GetTotalRevenueByDateAsync, GetTotalRevenueByDateRangeAsync
- CreateAsync, UpdateAsync, DeleteAsync

### 4. IProductRepository & ProductRepository

**File**: `Repositories/IProductRepository.cs`, `Repositories/ProductRepository.cs`

**Chức năng**:

- Quản lý các thao tác với bảng Product
- GetByIdAsync, GetAllAsync, GetByTypeAsync
- GetFoodAndBeverageAsync, GetEquipmentAndSuppliesAsync
- GetAvailableAsync, GetAvailableFoodAndBeverageAsync
- GetCountAsync, GetLowStockCountAsync, GetTotalValueAsync
- CreateAsync, UpdateAsync, DeleteAsync

### 5. IOrderRepository & OrderRepository

**File**: `Repositories/IOrderRepository.cs`, `Repositories/OrderRepository.cs`

**Chức năng**:

- Quản lý các thao tác với bảng Order
- GetByIdAsync, GetByIdWithDetailsAsync, GetAllAsync
- GetBySessionIdAsync, GetByStatusAsync, GetPendingOrdersAsync
- GetPendingCountAsync, CreateAsync, UpdateAsync, DeleteAsync

### 6. ICustomerRepository & CustomerRepository

**File**: `Repositories/ICustomerRepository.cs`, `Repositories/CustomerRepository.cs`

**Chức năng**:

- Quản lý các thao tác với bảng Customer
- GetByIdAsync, GetByPhoneAsync, GetAllAsync
- SearchAsync, PhoneExistsAsync
- CreateAsync, UpdateAsync, DeleteAsync

## Controllers đã được refactor

### 1. AccountController

**Thay đổi**:

- Sử dụng `IEmployeeRepository` thay vì truy cập trực tiếp `_context.Employees`
- Login action: `GetByUsernameAndPasswordAsync()`
- Register action: `IsUsernameExistsAsync()`, `IsEmailExistsAsync()`, `CreateAsync()`

### 2. ProfileController

**Thay đổi**:

- Sử dụng `IEmployeeRepository` thay vì truy cập trực tiếp `_context.Employees`
- Index action: `GetByIdAsync()`
- UpdateProfile action: `IsEmailExistsAsync()`, `UpdateAsync()`
- ChangePassword action: `GetByIdAsync()`, `UpdateAsync()`

### 3. ManagerController (một phần)

**Thay đổi**:

- Thêm các Repository dependencies vào constructor
- Index action: Sử dụng các Repository methods cho dashboard statistics
- GetAvailableProducts action: `GetAvailableFoodAndBeverageAsync()`
- TableService action: `GetByIdAsync()`, `GetAvailableFoodAndBeverageAsync()`

### 4. CashierController (một phần)

**Thay đổi**:

- Thêm các Repository dependencies vào constructor
- Index action: Sử dụng Repository methods cho dashboard statistics
- ProcessPayment action: `GetByIdWithDetailsAsync()`, `UpdateAsync()`
- CompleteOrder action: `GetByIdAsync()`, `UpdateAsync()`

## Dependency Injection đã cập nhật

**File**: `Program.cs`

```csharp
// Register repositories
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<ITableRepository, TableRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
```

## Công việc cần hoàn thiện

### 1. Hoàn thiện refactor các Controller còn lại

- ServingController: Cần refactor các action còn sử dụng `_context` trực tiếp
- ManagerController: Cần refactor các action phức tạp khác
- Các Controller khác nếu có

### 2. Tạo thêm Repository cho các entity khác (nếu cần)

- SessionRepository
- ReservationRepository
- OrderDetailRepository
- PermissionRepository
- ShiftRepository, ShiftAssignmentRepository

### 3. Cải thiện Repository methods

- Thêm các method phức tạp hơn nếu cần
- Tối ưu hóa performance với proper indexing
- Thêm pagination cho các query trả về nhiều dữ liệu

### 4. Unit Testing

- Tạo unit tests cho các Repository
- Tạo unit tests cho các Controller đã refactor
- Sử dụng mock Repository trong tests

## Lợi ích đạt được

1. **Separation of Concerns**: Logic truy vấn DB được tách riêng khỏi Controller
2. **Reusability**: Các Repository method có thể tái sử dụng cho nhiều Controller
3. **Testability**: Dễ dàng mock Repository để unit test Controller
4. **Maintainability**: Dễ bảo trì và mở rộng logic database
5. **Consistency**: Cách thức truy vấn DB được chuẩn hóa

## Hướng dẫn sử dụng

### Khi thêm method mới vào Repository:

1. Thêm method vào Interface trước
2. Implement method trong Repository class
3. Sử dụng method trong Controller thông qua DI

### Khi tạo Repository mới:

1. Tạo Interface (ví dụ: ISessionRepository)
2. Tạo Implementation (ví dụ: SessionRepository)
3. Đăng ký trong Program.cs DI container
4. Inject vào Controller constructor khi cần sử dụng
