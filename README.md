# 📸 Phố Studio — Hệ Thống Quản Lý Studio Nhiếp Ảnh

Ứng dụng web quản lý studio chụp ảnh chuyên nghiệp xây dựng trên **ASP.NET Core 9 MVC**, hỗ trợ đặt lịch, giao album cloud, quản lý thợ chụp và khách hàng.

---

## 🚀 Tính Năng Chính

### 👤 Khách Hàng
- Đặt lịch chụp ảnh, chọn gói dịch vụ
- Xem trạng thái lịch hẹn theo thời gian thực
- Nhận album ảnh qua OTP bảo mật
- Xem album nội bộ (upload server) hoặc Google Drive
- Chọn ảnh yêu thích (tim) và gửi yêu cầu retouch cho studio
- Chương trình khách hàng thân thiết (loyalty points)

### 📷 Thợ Chụp (Photographer)
- Dashboard lịch làm việc cá nhân
- Đánh dấu hoàn tất buổi chụp
- Upload album ảnh trực tiếp lên server hoặc giao qua Google Drive
- Quản lý album đã bàn giao, xem mã OTP

### 🛠️ Admin
- Dashboard doanh thu với biểu đồ 12 tháng (Online vs Tiền mặt)
- Quản lý nhân sự, gói dịch vụ, đặt lịch
- Xuất báo cáo Excel theo tháng/năm
- Quản lý coupon, refund, thông báo
- Kiểm toán tài khoản

---

## 🛠️ Công Nghệ Sử Dụng

| Layer | Công nghệ |
|---|---|
| Backend | ASP.NET Core 9 MVC |
| Database | SQLite + Entity Framework Core 9 |
| Auth | Cookie Authentication + JWT (API) |
| Frontend | Bootstrap 5.3, Chart.js, AOS |
| Fonts | Playfair Display, Poppins, Inter |

---

## ⚙️ Cài Đặt & Chạy Local

### Yêu cầu
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Visual Studio 2022+ hoặc VS Code

### Các bước

```bash
# 1. Clone repo
git clone https://github.com/imvoka3701/PhoStudioWebsite-final-new.git
cd PhoStudioWebsite-final-new

# 2. Restore packages
dotnet restore

# 3. Tạo database và apply migrations
dotnet ef database update

# 4. Chạy ứng dụng
dotnet run
```

Truy cập: `http://localhost:5074`

---

## 🔑 Tài Khoản Demo

| Role | Username | Password |
|---|---|---|
| Admin | `admin` | `Admin@123` |
| Photographer | `photographer01` | `Photographer@123` |
| Customer | `customer01` | `Customer@123` |

---

## 📁 Cấu Trúc Dự Án

```
PhoStudioMVC/
├── Controllers/          # MVC + API Controllers
│   └── Api/              # REST API endpoints
├── Data/                 # ApplicationDbContext
├── Migrations/           # EF Core migrations
├── Models/
│   ├── Entities/         # Domain models
│   ├── Enums/            # Enumerations
│   └── ViewModels/       # View-specific models
├── Services/             # Business logic services
├── Views/                # Razor views
│   ├── Admin/
│   ├── Client/
│   ├── Photographer/
│   └── Shared/
└── wwwroot/              # Static assets
    ├── assets/css/
    ├── assets/js/
    └── uploads/albums/   # User-uploaded photos (gitignored)
```

---

## 📝 Lưu Ý

- File `phostudio.db` được tạo tự động khi chạy lần đầu (gitignored)
- Thư mục `wwwroot/uploads/` được tạo tự động khi photographer upload ảnh
- Để deploy production, thay SQLite bằng SQL Server và cập nhật connection string trong `appsettings.json`
