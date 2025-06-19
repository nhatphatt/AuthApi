# Auth API - ASP.NET Core Web API với JWT Authentication

Dự án ASP.NET Core Web API hoàn chỉnh sử dụng C# với JWT Authentication, Entity Framework Core và SQLite.

## 🚀 Tính năng

- ✅ .NET 9
- ✅ Kiến trúc rõ ràng: Models, DTOs, Data, Services, Interfaces, Controllers, Helpers
- ✅ Entity Framework Core với SQLite database
- ✅ JWT Authentication & Authorization
- ✅ Mã hóa mật khẩu bằng BCrypt
- ✅ API Register và Login
- ✅ Middleware xử lý JWT
- ✅ Swagger UI tích hợp
- ✅ Role-based authorization (User, Admin)
- ✅ CORS enabled

## 📁 Cấu trúc thư mục

```
AuthApi/
├── Controllers/          # API Controllers
├── Data/                # Database Context
├── DTOs/               # Data Transfer Objects
├── Helpers/            # Middleware và utilities
├── Interfaces/         # Service interfaces
├── Models/            # Database models
├── Services/          # Business logic services
├── Migrations/        # EF Core migrations
├── Program.cs         # Entry point
└── appsettings.json   # Cấu hình
```

## 🛠 Cài đặt và chạy

### 1. Clone và restore packages

```bash
git clone <repository-url>
cd AuthApi
dotnet restore
```

### 2. Tạo và cập nhật database

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 3. Chạy ứng dụng

```bash
dotnet run
```

Ứng dụng sẽ chạy tại: `https://localhost:5001` hoặc `http://localhost:5000`

## 📚 API Endpoints

### Authentication

#### POST /api/auth/register

Đăng ký người dùng mới

**Request Body:**

```json
{
  "username": "testuser",
  "password": "password123",
  "role": "User"
}
```

**Response:**

```json
{
  "success": true,
  "message": "User registered successfully",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "username": "testuser",
    "role": "User",
    "expiresAt": "2024-01-01T12:00:00.000Z"
  }
}
```

#### POST /api/auth/login

Đăng nhập

**Request Body:**

```json
{
  "username": "testuser",
  "password": "password123"
}
```

### Protected Endpoints (Yêu cầu JWT Token)

#### GET /api/protected/profile

Lấy thông tin profile người dùng hiện tại

**Headers:**

```
Authorization: Bearer <jwt-token>
```

#### GET /api/protected/user

Endpoint cho User và Admin

#### GET /api/protected/admin

Endpoint chỉ dành cho Admin

## 🧪 Cách test với Swagger UI

1. Chạy ứng dụng: `dotnet run`
2. Mở trình duyệt: `https://localhost:5001`
3. Swagger UI sẽ tự động mở

### Các bước test:

1. **Đăng ký user mới:**

   - Mở endpoint `POST /api/auth/register`
   - Nhập thông tin user
   - Nhận JWT token trong response

2. **Đăng nhập:**

   - Sử dụng `POST /api/auth/login`
   - Nhận JWT token

3. **Test protected endpoints:**
   - Click nút "Authorize" ở đầu trang Swagger
   - Nhập: `Bearer <jwt-token>`
   - Test các protected endpoints

## 🧪 Cách test với Postman

### 1. Import Collection

Tạo collection mới với các requests sau:

### 2. Register User

**POST** `https://localhost:5001/api/auth/register`

**Headers:**

```
Content-Type: application/json
```

**Body (raw JSON):**

```json
{
  "username": "testuser",
  "password": "password123",
  "role": "User"
}
```

### 3. Login

**POST** `https://localhost:5001/api/auth/login`

**Headers:**

```
Content-Type: application/json
```

**Body (raw JSON):**

```json
{
  "username": "testuser",
  "password": "password123"
}
```

**Lưu token từ response để sử dụng cho các request tiếp theo**

### 4. Test Protected Endpoint

**GET** `https://localhost:5001/api/protected/profile`

**Headers:**

```
Authorization: Bearer <jwt-token-from-login>
Content-Type: application/json
```

## ⚙️ Cấu hình

### JWT Settings (appsettings.json)

```json
{
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong123456789",
    "Issuer": "AuthApi",
    "Audience": "AuthApiUsers",
    "ExpiryInMinutes": 60
  }
}
```

### Database

- **Type:** SQLite
- **File:** `authapi.db` (tự động tạo)
- **Connection String:** `Data Source=authapi.db`

## 🔐 Security Features

1. **Password Hashing:** BCrypt với salt
2. **JWT Token:**
   - HMAC SHA256 signing
   - Configurable expiry
   - Role-based claims
3. **Authorization:**
   - Role-based access control
   - JWT middleware validation
4. **CORS:** Configured for development

## 🚨 Lỗi thường gặp và cách xử lý

### 1. Database connection error

```bash
dotnet ef database update
```

### 2. JWT token invalid

- Kiểm tra token format: `Bearer <token>`
- Kiểm tra token expiry
- Verify JWT secret key

### 3. Migration issues

```bash
dotnet ef migrations remove
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

## 📝 Lưu ý

- Thay đổi JWT SecretKey trong production
- Sử dụng HTTPS trong production
- Implement proper logging
- Add input validation
- Consider rate limiting

## 🔧 Technologies Used

- .NET 9
- ASP.NET Core Web API
- Entity Framework Core
- SQLite
- JWT Bearer Authentication
- BCrypt.Net
- Swagger/OpenAPI
- AutoMapper (có thể thêm)

## 📞 Hỗ trợ

Nếu gặp vấn đề, hãy kiểm tra:

1. .NET 9 SDK đã được cài đặt
2. Database file có quyền write
3. Port 5001/5000 không bị conflict
4. JWT token format đúng

Happy coding! 🎉
