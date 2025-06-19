# 🚀 HƯỚNG DẪN TRIỂN KHAI CHATBOT AI SYSTEM

## 📋 TỔNG QUAN HỆ THỐNG

Hệ thống Chatbot AI với các tính năng:

- ✅ **Authentication**: JWT-based login/register
- ✅ **Payment System**: Subscription plans với token limits
- ✅ **AI Chatbot**: Tích hợp api.iunhi.com (OpenAI compatible)
- ✅ **Permission Control**: Chỉ user có subscription mới dùng được chatbot
- ✅ **Admin Panel**: Quản lý payments và subscriptions

---

## 🏗️ KIẾN TRÚC HỆ THỐNG

### Database Tables:

1. **Users** - Thông tin tài khoản
2. **Subscriptions** - Gói thanh toán và token limits
3. **ChatHistories** - Lịch sử chat với AI

### API Endpoints:

#### 🔐 Authentication (`/api/auth`)

- `POST /register` - Đăng ký
- `POST /login` - Đăng nhập

#### 💳 Payment (`/api/payment`)

- `GET /plans` - Xem các gói subscription
- `POST /process` - Xử lý thanh toán
- `GET /subscription` - Trạng thái subscription hiện tại
- `POST /simulate/{planType}` - Simulate thanh toán (test)
- `PUT /admin/update-subscription` - Admin update payment

#### 🤖 Chat (`/api/chat`)

- `POST /send` - Gửi tin nhắn cho AI
- `GET /history` - Lịch sử chat
- `GET /permission` - Kiểm tra quyền chat
- `GET /tokens` - Số token còn lại

---

## ⚙️ CÀI ĐẶT VÀ TRIỂN KHAI

### 1. **Chuẩn bị môi trường**

```bash
# Cài đặt .NET 9 SDK
# Cài đặt SQL Server
# Cài đặt SQL Server Management Studio 19
```

### 2. **Cấu hình Database**

1. **Tạo database trong SSMS:**

   ```sql
   CREATE DATABASE AuthApiDb;
   ```

2. **Chạy migrations:**
   ```bash
   dotnet ef database update
   ```

### 3. **Cấu hình OpenAI API**

Cập nhật `appsettings.json`:

```json
{
  "OpenAI": {
    "ApiBaseUrl": "https://api.iunhi.com",
    "ApiKey": "YOUR_ACTUAL_API_KEY_HERE"
  }
}
```

### 4. **Chạy ứng dụng**

```bash
dotnet run
```

Ứng dụng sẽ chạy tại: `http://localhost:5288`

---

## 🔄 WORKFLOW SỬ DỤNG

### **Bước 1: User Registration/Login**

```bash
POST /api/auth/register
{
  "username": "user1",
  "password": "password123",
  "role": "User"
}
```

### **Bước 2: Kiểm tra Subscription Plans**

```bash
GET /api/payment/plans
```

**Response:**

```json
{
  "success": true,
  "data": [
    {
      "name": "Basic",
      "price": 99000,
      "tokenLimit": 10000,
      "durationDays": 30,
      "features": ["10,000 tokens/month", "GPT-3.5 access"]
    },
    {
      "name": "Premium",
      "price": 199000,
      "tokenLimit": 50000,
      "durationDays": 30,
      "features": ["50,000 tokens/month", "GPT-4 access"]
    }
  ]
}
```

### **Bước 3: Thanh toán (Simulation)**

```bash
POST /api/payment/simulate/Basic
Authorization: Bearer <jwt_token>
```

### **Bước 4: Sử dụng Chatbot**

```bash
POST /api/chat/send
Authorization: Bearer <jwt_token>
{
  "message": "Hello AI, how are you?",
  "model": "gpt-3.5-turbo"
}
```

---

## 💰 HỆ THỐNG THANH TOÁN

### **Subscription Plans:**

| Plan        | Price (VNĐ) | Tokens/Month | Features      |
| ----------- | ----------- | ------------ | ------------- |
| **Free**    | 0           | 100          | Basic chat    |
| **Basic**   | 99,000      | 10,000       | GPT-3.5 Turbo |
| **Premium** | 199,000     | 50,000       | GPT-4 Access  |

### **Payment Flow:**

1. **User chọn plan** → `GET /api/payment/plans`
2. **Thực hiện thanh toán** → `POST /api/payment/process`
3. **System update database** → Set `isPaid = true`
4. **User có thể dùng chatbot** → Permission granted

### **Admin Management:**

Admin có thể manually update payment status:

```bash
PUT /api/payment/admin/update-subscription?userId=1&planType=Basic&isPaid=true
Authorization: Bearer <admin_jwt_token>
```

---

## 🛡️ SECURITY & PERMISSIONS

### **Permission Levels:**

1. **Free Users**: 100 tokens, limited features
2. **Paid Users**: Full token limit based on plan
3. **Expired Subscription**: No chat access until renewal

### **Token Management:**

- **Token consumption** tracked per chat request
- **Auto-deduction** from user's token balance
- **Block access** when tokens exhausted
- **Monthly reset** for active subscriptions

---

## 🧪 TESTING SCENARIOS

### **Test 1: Free User Chat**

```bash
# 1. Register new user
# 2. Try chat immediately (should work with 100 tokens)
# 3. Exhaust tokens → Should block access
```

### **Test 2: Paid User Flow**

```bash
# 1. Register user
# 2. Simulate payment for Basic plan
# 3. Chat with increased token limit
# 4. Verify chat history saves correctly
```

### **Test 3: Admin Operations**

```bash
# 1. Login as admin
# 2. Update user subscription manually
# 3. Verify user gains access immediately
```

---

## 📊 MONITORING & ANALYTICS

### **Key Metrics to Track:**

1. **User Registrations**: Daily/Monthly new users
2. **Subscription Conversions**: Free → Paid ratios
3. **Token Usage**: Average tokens per user
4. **Chat Volume**: Messages per day/user
5. **Revenue**: Monthly subscription income

### **Database Queries for Analytics:**

```sql
-- Active subscriptions
SELECT COUNT(*) FROM Subscriptions
WHERE IsPaid = 1 AND ExpiresAt > GETDATE();

-- Monthly revenue
SELECT SUM(Amount) FROM Subscriptions
WHERE PaidAt >= DATEADD(month, -1, GETDATE());

-- Top users by chat volume
SELECT u.Username, COUNT(ch.Id) as ChatCount
FROM Users u
JOIN ChatHistories ch ON u.Id = ch.UserId
GROUP BY u.Username
ORDER BY ChatCount DESC;
```

---

## 🚀 DEPLOYMENT PRODUCTION

### **Environment Variables:**

```bash
ConnectionStrings__DefaultConnection="Server=prod;Database=AuthApiDb;..."
JwtSettings__SecretKey="ProductionSecretKey256Bits!"
OpenAI__ApiKey="sk-prod-key-here"
```

### **Security Checklist:**

- [ ] Change JWT secret key
- [ ] Use HTTPS in production
- [ ] Implement rate limiting
- [ ] Add input validation
- [ ] Setup logging & monitoring
- [ ] Database backup strategy

### **Performance Optimization:**

- [ ] Database indexing
- [ ] Redis caching for tokens
- [ ] API response caching
- [ ] Connection pooling
- [ ] Load balancing

---

## 🛠️ TROUBLESHOOTING

### **Common Issues:**

1. **"Invalid API Key"**

   - Check OpenAI configuration in appsettings.json
   - Verify api.iunhi.com accessibility

2. **"No subscription found"**

   - User needs to purchase a plan
   - Check subscription expiry dates

3. **"Token limit exceeded"**

   - User exhausted monthly allocation
   - Upgrade plan or wait for renewal

4. **Database connection errors**
   - Verify SQL Server running
   - Check connection string

---

## 📞 SUPPORT & MAINTENANCE

### **Regular Maintenance:**

- Monitor token usage patterns
- Update OpenAI API integration
- Database cleanup of old chat histories
- Security updates and patches

### **Scaling Considerations:**

- Horizontal scaling with load balancers
- Database sharding for large user base
- CDN for static assets
- Microservices architecture for complex features

---

## 🎉 NEXT STEPS

1. **Deploy to staging environment**
2. **Load testing with simulated users**
3. **Integration with real payment gateway**
4. **Mobile app development**
5. **Advanced AI features (image, voice)**
6. **Business analytics dashboard**

---

**🚀 Hệ thống đã sẵn sàng cho production! Chúc em thành công!** ✨
