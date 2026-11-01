# Social Media

Dự án ứng dụng mạng xã hội (Social Media).

## Phiên bản công nghệ
- **Backend**: .NET 10 (ASP.NET Core Minimal API, C# 14 / .NET 10 SDK)
- **Frontend**: Angular 21 (Angular CLI 21.2.7, Node.js 24)

---

## Hướng dẫn chạy Backend

### 1. Yêu cầu môi trường
- .NET SDK 10.0+
- (Tùy chọn) Docker

### 2. Build backend
```bash
cd backend
dotnet build
```

### 3. Chạy backend
```bash
cd backend
dotnet run --project SocialMedia.Api
```
API sẽ lắng nghe tại cổng mặc định (ví dụ: `http://localhost:5154` hoặc cổng được cấu hình).
Kiểm tra endpoint sức khỏe:
```bash
curl http://localhost:5154/health
```
Phản hồi mẫu:
```json
{"status":"ok"}
```

### 4. Chạy kiểm thử Backend
```bash
cd backend
dotnet test
```

### 5. Build và chạy Backend với Docker
```bash
cd backend
docker build -t socialmedia-api .
docker run -d -p 8080:8080 socialmedia-api
```
Kiểm tra qua Docker container:
```bash
curl http://localhost:8080/health
```

---

## Hướng dẫn chạy Frontend

### 1. Yêu cầu môi trường
- Node.js 24+
- npm 11+
- Angular CLI 21

### 2. Cài đặt dependencies
```bash
cd frontend
npm install
```

### 3. Chạy frontend (Development Server)
```bash
cd frontend
npm start
# hoặc
ng serve
```
Mặc định ứng dụng chạy tại: `http://localhost:4200/`.

### 4. Build frontend
```bash
cd frontend
npm run build
# hoặc
ng build
```

### 5. Chạy kiểm thử Frontend
```bash
cd frontend
npm test
# hoặc chạy một lần không watch:
ng test --watch=false
```
