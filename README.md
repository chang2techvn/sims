# SIMS - Student Information Management System

Hệ thống quản lý thông tin sinh viên hiện đại được xây dựng với ASP.NET Core 8.0, Entity Framework Core và SQL Server.

## 🌟 Tính năng chính

### 👨‍🎓 Sinh viên (Students)
- Đăng ký tài khoản và đăng nhập
- Xem danh sách môn học đã đăng ký
- Tìm kiếm và đăng ký môn học mới
- Quản lý thông tin cá nhân

### 👨‍🏫 Giảng viên (Lecturers)
- Xem danh sách môn học được phân công
- Quản lý danh sách sinh viên trong lớp
- Theo dõi thống kê lớp học
- Cập nhật thông tin cá nhân

### 👨‍💼 Quản trị viên (Administrators)
- Quản lý người dùng (sinh viên, giảng viên, admin)
- Quản lý khoa, chuyên ngành
- Quản lý học kỳ và môn học
- Quản lý khóa học và phân công giảng viên
- Gán sinh viên vào các khóa học

## 🎨 Thiết kế và UX

- **Giao diện hiện đại** với theme màu cam chuyên nghiệp
- **Sidebar navigation** với menu phân quyền theo vai trò
- **Responsive design** tương thích mọi thiết bị
- **Dashboard thống kê** trực quan cho từng vai trò
- **Animations và transitions** mượt mà
- **Form validation** thời gian thực

## 🏗️ Kiến trúc hệ thống

```
SIMS/
├── Controllers/           # Controllers xử lý logic nghiệp vụ
│   ├── AccountController  # Xác thực và quản lý tài khoản
│   ├── AdminController    # Chức năng quản trị
│   ├── HomeController     # Dashboard và trang chủ
│   ├── StudentController  # Chức năng sinh viên
│   └── LecturerController # Chức năng giảng viên
├── Models/               # Data models và ViewModels
│   ├── Academic.cs       # Course, Subject, Semester
│   ├── User.cs           # User authentication
│   ├── UserRoles.cs      # Student, Lecturer, Admin
│   └── ViewModels/       # DTOs cho Views
├── Views/                # Razor Views
│   ├── Account/          # Login, Register, Profile
│   ├── Admin/            # Admin management views
│   ├── Student/          # Student functionality views
│   ├── Lecturer/         # Lecturer functionality views
│   └── Shared/           # Layout và shared views
├── Data/                 # Database Context
└── wwwroot/              # Static files (CSS, JS, Images)
```

## 🔧 Cấu hình và cài đặt

### Yêu cầu hệ thống
- .NET 8.0 SDK
- SQL Server hoặc SQL Server LocalDB
- Visual Studio 2022 hoặc VS Code

### Hướng dẫn cài đặt

1. **Clone repository**
   ```bash
   git clone [repository-url]
   cd SIMS
   ```

2. **Restore packages**
   ```bash
   cd SIMS
   dotnet restore
   ```

3. **Cấu hình database**
   - Mở file `SIMS/appsettings.json`
   - Cập nhật connection string:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SIMSDb;Trusted_Connection=true;MultipleActiveResultSets=true"
     }
   }
   ```

4. **Cài đặt Entity Framework Tools**
   ```bash
   dotnet tool install --global dotnet-ef
   ```

5. **Tạo database**
   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

6. **Chạy ứng dụng**
   ```bash
   dotnet run
   ```

7. **Mở browser và truy cập**: `https://localhost:5001` hoặc `http://localhost:5000`

## 📊 Database Schema

### Bảng chính
- **Users**: Thông tin người dùng (AspNetUsers)
- **Students**: Thông tin sinh viên
- **Lecturers**: Thông tin giảng viên  
- **Admins**: Thông tin quản trị viên
- **Departments**: Khoa
- **Majors**: Chuyên ngành
- **Subjects**: Môn học
- **Semesters**: Học kỳ
- **Courses**: Khóa học
- **StudentCourses**: Đăng ký môn học

### Quan hệ
- User 1:1 Student/Lecturer/Admin
- Department 1:N Major
- Major 1:N Student
- Major 1:N Course
- Lecturer 1:N Course
- Student N:N Course (through StudentCourse)

## 🔐 Bảo mật

- **ASP.NET Identity**: Xác thực và phân quyền
- **Role-based authorization**: Phân quyền theo vai trò
- **Password hashing**: Mã hóa mật khẩu
- **CSRF protection**: Bảo vệ chống tấn công CSRF
- **Input validation**: Kiểm tra dữ liệu đầu vào

## 🎯 Tài khoản mặc định

Sau khi chạy migration, bạn có thể tạo tài khoản admin đầu tiên thông qua trang đăng ký.

### Vai trò hệ thống:
- **admin**: Toàn quyền quản trị
- **lecturer**: Quản lý lớp học được phân công
- **student**: Đăng ký và theo dõi môn học

## 🚀 Tính năng nâng cao

- **Real-time validation**: Kiểm tra form theo thời gian thực
- **Auto-save**: Tự động lưu thay đổi
- **Search và filter**: Tìm kiếm trong bảng dữ liệu
- **Export data**: Xuất dữ liệu CSV
- **Responsive sidebar**: Menu bên trái tương thích mobile
- **Toast notifications**: Thông báo trực quan
- **Loading states**: Hiệu ứng loading chuyên nghiệp

## 📱 Tương thích

- **Desktop**: Windows, macOS, Linux
- **Mobile**: iOS, Android (responsive)
- **Browsers**: Chrome, Firefox, Safari, Edge

## 🛠️ Công nghệ sử dụng

- **Backend**: ASP.NET Core 8.0, Entity Framework Core
- **Database**: SQL Server
- **Frontend**: Razor Pages, Bootstrap 5, jQuery
- **Authentication**: ASP.NET Identity
- **Icons**: Font Awesome 6
- **Fonts**: Google Fonts (Inter)

## 📝 Ghi chú phát triển

- Tuân thủ SOLID principles
- Clean code architecture
- Repository pattern có thể được thêm vào
- Unit testing có thể được mở rộng
- Logging và monitoring có thể được thêm vào

## 🤝 Đóng góp

1. Fork repository
2. Tạo feature branch
3. Commit changes
4. Push to branch
5. Tạo Pull Request

## 📄 Giấy phép

MIT License - xem file LICENSE để biết chi tiết.

## 📞 Hỗ trợ

Nếu gặp vấn đề trong quá trình cài đặt hoặc sử dụng, vui lòng tạo issue trong repository.

---

**SIMS** - Hệ thống quản lý thông tin sinh viên hiện đại và chuyên nghiệp! 🎓✨