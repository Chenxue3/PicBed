# PicBed - Technical Implementation Documentation

## Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [Technology Stack](#technology-stack)
3. [Project Structure](#project-structure)
4. [Database Design](#database-design)
5. [Authentication System](#authentication-system)
6. [Image Processing Pipeline](#image-processing-pipeline)
7. [API Design](#api-design)
8. [Frontend Implementation](#frontend-implementation)
9. [Security Implementation](#security-implementation)
10. [Testing Strategy](#testing-strategy)
11. [Deployment Architecture](#deployment-architecture)
12. [Performance Considerations](#performance-considerations)
13. [Monitoring and Logging](#monitoring-and-logging)
14. [Future Enhancements](#future-enhancements)

## Architecture Overview

### System Architecture
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Frontend      │    │   Backend API   │    │   Database      │
│   (HTML/JS)     │◄──►│   (ASP.NET)     │◄──►│   (SQLite)      │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                              │
                              ▼
                       ┌─────────────────┐
                       │   File System   │
                       │   (Images)      │
                       └─────────────────┘
```

### Key Components
- **Frontend**: Single-page application with Alpine.js
- **Backend**: ASP.NET Core Web API with RESTful endpoints
- **Database**: SQLite for metadata storage
- **File Storage**: Local file system for image files
- **Authentication**: Custom JWT-like token system
- **Image Processing**: SixLabors.ImageSharp for thumbnails

## Technology Stack

### Backend Technologies
- **.NET 8.0**: Latest LTS version for performance and features
- **ASP.NET Core**: Web API framework
- **Entity Framework Core**: ORM for database operations
- **SQLite**: Lightweight, serverless database
- **SixLabors.ImageSharp**: Cross-platform image processing
- **Swagger/OpenAPI**: API documentation

### Frontend Technologies
- **HTML5**: Semantic markup
- **Tailwind CSS**: Utility-first CSS framework
- **Alpine.js**: Lightweight JavaScript framework
- **Fetch API**: Modern HTTP client
- **Web APIs**: File API, Clipboard API

### Development Tools
- **Visual Studio Code**: Primary IDE
- **.NET CLI**: Command-line tools
- **Docker**: Containerization
- **Git**: Version control

## Project Structure

```
PicBed/
├── Controllers/                 # API Controllers
│   ├── AuthController.cs       # Authentication endpoints
│   └── ImagesController.cs     # Image management endpoints
├── Data/                       # Data Access Layer
│   └── PicBedDbContext.cs      # Entity Framework context
├── Middleware/                 # Custom middleware
│   └── AuthMiddleware.cs       # Authentication middleware
├── Models/                     # Data Models
│   ├── ImageRecord.cs          # Image entity
│   ├── User.cs                 # User entity
│   ├── LoginRequest.cs         # Login DTO
│   └── LoginResponse.cs        # Login response DTO
├── Services/                   # Business Logic
│   ├── IAuthService.cs         # Authentication interface
│   ├── AuthService.cs          # Authentication implementation
│   ├── IImageService.cs        # Image service interface
│   └── ImageService.cs         # Image service implementation
├── wwwroot/                    # Static Files
│   ├── index.html              # Frontend application
│   ├── uploads/                # Uploaded images
│   └── thumbnails/             # Generated thumbnails
├── Dockerfile                  # Container configuration
├── docker-compose.yml          # Multi-container setup
├── PicBed.csproj              # Project file
├── Program.cs                 # Application entry point
└── appsettings.json           # Configuration
```

## Database Design

### Entity Relationship Diagram
```
┌─────────────┐    ┌─────────────┐
│    Users    │    │   Images    │
├─────────────┤    ├─────────────┤
│ Id (PK)     │    │ Id (PK)     │
│ Username    │    │ FileName    │
│ PasswordHash│    │ OriginalName│
│ Email       │    │ Extension   │
│ CreatedAt   │    │ FileSize    │
│ LastLoginAt │    │ Width       │
│ IsActive    │    │ Height      │
└─────────────┘    │ MimeType    │
                   │ UploadTime  │
                   │ Description │
                   │ Category    │
                   │ IsPublic    │
                   └─────────────┘
```

### Database Schema

#### Users Table
```sql
CREATE TABLE "Users" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY AUTOINCREMENT,
    "Username" TEXT NOT NULL,
    "PasswordHash" TEXT NOT NULL,
    "Email" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    "LastLoginAt" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL
);

CREATE UNIQUE INDEX "IX_Users_Username" ON "Users" ("Username");
CREATE INDEX "IX_Users_Email" ON "Users" ("Email");
```

#### Images Table
```sql
CREATE TABLE "Images" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Images" PRIMARY KEY AUTOINCREMENT,
    "FileName" TEXT NOT NULL,
    "OriginalFileName" TEXT NOT NULL,
    "FileExtension" TEXT NOT NULL,
    "FileSize" INTEGER NOT NULL,
    "Width" INTEGER NOT NULL,
    "Height" INTEGER NOT NULL,
    "MimeType" TEXT NOT NULL,
    "UploadTime" TEXT NOT NULL,
    "Description" TEXT NULL,
    "Category" TEXT NULL,
    "IsPublic" INTEGER NOT NULL
);

CREATE UNIQUE INDEX "IX_Images_FileName" ON "Images" ("FileName");
CREATE INDEX "IX_Images_UploadTime" ON "Images" ("UploadTime");
CREATE INDEX "IX_Images_Category" ON "Images" ("Category");
CREATE INDEX "IX_Images_IsPublic" ON "Images" ("IsPublic");
```

## Authentication System

### Token-Based Authentication
The system implements a custom JWT-like token system for simplicity and control.

#### Token Structure
```json
{
  "userId": 1,
  "username": "admin",
  "issuedAt": 1703123456789,
  "expiresAt": 1703728256789
}
```

#### Token Generation Process
1. Create token payload with user information
2. Serialize to JSON
3. Base64 encode the JSON
4. Generate HMAC-SHA256 signature
5. Combine: `{base64Payload}.{signature}`

#### Implementation Details
```csharp
public string GenerateToken(User user)
{
    var tokenData = new
    {
        userId = user.Id,
        username = user.Username,
        issuedAt = DateTime.UtcNow.Ticks,
        expiresAt = DateTime.UtcNow.AddDays(7).Ticks
    };

    var json = JsonSerializer.Serialize(tokenData);
    var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    var signature = ComputeSignature(encoded);

    return $"{encoded}.{signature}";
}
```

#### Password Hashing
- Algorithm: SHA-256 with salt
- Salt: Application secret key
- Implementation: `HashPassword(password + secretKey)`

### Authentication Flow
1. User submits credentials
2. Server validates credentials
3. Server generates token
4. Client stores token in localStorage
5. Client includes token in subsequent requests
6. Server validates token on each request

## Image Processing Pipeline

### Upload Process
```
File Upload → Validation → Storage → Processing → Database
     ↓              ↓         ↓         ↓          ↓
  Form Data    Size/Type   Physical   Thumbnail   Metadata
```

### Image Processing Steps
1. **File Validation**
   - Check file size (max 10MB)
   - Validate file extension
   - Verify MIME type

2. **File Storage**
   - Generate unique filename (GUID)
   - Save to `uploads/` directory
   - Preserve original filename in database

3. **Image Processing**
   - Load image with ImageSharp
   - Extract dimensions
   - Generate thumbnail (200x200 max)
   - Save thumbnail to `thumbnails/` directory

4. **Database Storage**
   - Store metadata in SQLite
   - Create database record
   - Return image information

### Thumbnail Generation
```csharp
using (var image = await Image.LoadAsync(filePath))
{
    width = image.Width;
    height = image.Height;

    using (var thumbnail = image.Clone(x => x.Resize(new ResizeOptions
    {
        Size = new Size(200, 200),
        Mode = ResizeMode.Max
    })))
    {
        await thumbnail.SaveAsync(thumbnailFilePath);
    }
}
```

## API Design

### RESTful Endpoints

#### Authentication Endpoints
```
POST /api/auth/login
POST /api/auth/validate
```

#### Image Management Endpoints
```
GET    /api/images              # List images
POST   /api/images/upload       # Upload image
GET    /api/images/{id}         # Get image info
DELETE /api/images/{id}         # Delete image
GET    /api/images/file/{name}  # Get image file
GET    /api/images/thumbnail/{name} # Get thumbnail
```

### Request/Response Examples

#### Login Request
```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "admin"
}
```

#### Login Response
```json
{
  "success": true,
  "token": "eyJ1c2VySWQiOjEsInVzZXJuYW1lIjoiYWRtaW4i...",
  "user": {
    "id": 1,
    "username": "admin",
    "email": "admin@picbed.com"
  },
  "message": "Login successful"
}
```

#### Image Upload Request
```http
POST /api/images/upload
Authorization: Bearer {token}
Content-Type: multipart/form-data

file: [binary data]
description: "My image"
category: "photos"
```

#### Image List Response
```json
[
  {
    "id": 1,
    "fileName": "32ec3366-8bce-4f24-8eac-a7dec99cd52e.png",
    "originalFileName": "test.png",
    "fileExtension": ".png",
    "fileSize": 1024,
    "width": 800,
    "height": 600,
    "mimeType": "image/png",
    "uploadTime": "2024-01-01T12:00:00Z",
    "description": "Test image",
    "category": "test",
    "isPublic": true
  }
]
```

## Frontend Implementation

### Architecture Pattern
- **MVVM-like**: Alpine.js provides reactive data binding
- **Component-based**: Modular JavaScript functions
- **Event-driven**: User interactions trigger state changes

### State Management
```javascript
function picBedApp() {
    return {
        // Authentication state
        isLoggedIn: false,
        authToken: null,
        currentUser: null,
        
        // UI state
        showLogin: true,
        uploading: false,
        loading: false,
        
        // Data state
        images: [],
        selectedFiles: [],
        
        // Methods
        login() { /* ... */ },
        uploadFiles() { /* ... */ },
        loadImages() { /* ... */ }
    }
}
```

### File Upload Implementation
```javascript
handleDrop(event) {
    const files = Array.from(event.dataTransfer.files);
    this.addFiles(files);
}

async uploadFiles() {
    const uploadPromises = this.selectedFiles.map(async (fileData) => {
        const formData = new FormData();
        formData.append('file', fileData.file);
        
        const response = await fetch('/api/images/upload', {
            method: 'POST',
            headers: { 'Authorization': `Bearer ${this.authToken}` },
            body: formData
        });
        
        return response.json();
    });
    
    await Promise.all(uploadPromises);
}
```

### Responsive Design
- **Mobile-first**: Tailwind CSS responsive utilities
- **Grid Layout**: CSS Grid for image gallery
- **Flexible Components**: Adaptive UI elements

## Security Implementation

### Authentication Security
- **Token Expiration**: 7-day token lifetime
- **Secure Storage**: localStorage for client-side
- **Password Hashing**: SHA-256 with salt
- **Input Validation**: Server-side validation

### File Upload Security
- **File Type Validation**: Whitelist approach
- **Size Limits**: 10MB maximum
- **Unique Filenames**: GUID-based naming
- **Path Traversal Protection**: Sanitized paths

### API Security
- **CORS Configuration**: Configurable origins
- **Request Validation**: Model validation
- **Error Handling**: Secure error messages
- **Rate Limiting**: (Future enhancement)

### Data Protection
- **SQL Injection Prevention**: Entity Framework parameterized queries
- **XSS Protection**: Input sanitization
- **CSRF Protection**: Token-based validation

## Testing Strategy

### Unit Testing
```csharp
[Test]
public async Task UploadImage_ValidFile_ReturnsImageInfo()
{
    // Arrange
    var mockFile = CreateMockFile();
    var service = new ImageService(mockContext, mockConfig, mockEnv, mockLogger);
    
    // Act
    var result = await service.UploadImageAsync(mockFile);
    
    // Assert
    Assert.IsNotNull(result);
    Assert.AreEqual("test.jpg", result.OriginalFileName);
}
```

### Integration Testing
```csharp
[Test]
public async Task ImagesController_Upload_ReturnsOk()
{
    // Arrange
    var client = _factory.CreateClient();
    var formData = new MultipartFormDataContent();
    formData.Add(new ByteArrayContent(testImageBytes), "file", "test.jpg");
    
    // Act
    var response = await client.PostAsync("/api/images/upload", formData);
    
    // Assert
    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
}
```

### Frontend Testing
```javascript
// Jest test example
describe('File Upload', () => {
    test('should handle file selection', () => {
        const app = picBedApp();
        const mockFile = new File(['test'], 'test.jpg', { type: 'image/jpeg' });
        
        app.handleFileSelect({ target: { files: [mockFile] } });
        
        expect(app.selectedFiles).toHaveLength(1);
    });
});
```

### API Testing with Postman
- **Collection**: Complete API test suite
- **Environment Variables**: Dynamic configuration
- **Automated Tests**: CI/CD integration
- **Performance Tests**: Load testing scenarios

## Deployment Architecture

### Development Environment
```
Developer Machine
├── .NET 8.0 SDK
├── Visual Studio Code
├── SQLite Browser
└── Docker Desktop
```

### Production Environment
```
Production Server
├── Docker Container
│   ├── ASP.NET Core Runtime
│   ├── Application Code
│   └── SQLite Database
├── Nginx (Reverse Proxy)
├── SSL Certificate
└── File Storage
```

### Docker Configuration
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PicBed.csproj", "."]
RUN dotnet restore "PicBed.csproj"
COPY . .
RUN dotnet build "PicBed.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PicBed.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PicBed.dll"]
```

### Docker Compose Setup
```yaml
version: '3.8'
services:
  picbed:
    build: .
    ports:
      - "5000:80"
    volumes:
      - ./uploads:/app/wwwroot/uploads
      - ./thumbnails:/app/wwwroot/thumbnails
      - ./data:/app/data
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    restart: unless-stopped
```

### Nginx Configuration
```nginx
server {
    listen 80;
    server_name your-domain.com;
    
    location / {
        proxy_pass http://localhost:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
    
    location /api/images/file/ {
        proxy_pass http://localhost:5000;
        expires 1y;
        add_header Cache-Control "public, immutable";
    }
}
```

## Performance Considerations

### Database Optimization
- **Indexes**: Strategic indexing on frequently queried columns
- **Connection Pooling**: EF Core connection management
- **Query Optimization**: Efficient LINQ queries
- **Pagination**: Limit result sets

### File System Optimization
- **Directory Structure**: Organized file storage
- **Thumbnail Caching**: Pre-generated thumbnails
- **File Compression**: Image optimization
- **CDN Integration**: (Future enhancement)

### Memory Management
- **Streaming**: File stream processing
- **Disposal**: Proper resource cleanup
- **Caching**: In-memory caching strategies
- **Garbage Collection**: .NET GC optimization

### Network Optimization
- **Compression**: Gzip compression
- **Caching Headers**: Browser caching
- **CDN**: Content delivery network
- **HTTP/2**: Modern protocol support

## Monitoring and Logging

### Application Logging
```csharp
public class ImageService
{
    private readonly ILogger<ImageService> _logger;
    
    public async Task<ImageRecord> UploadImageAsync(IFormFile file)
    {
        _logger.LogInformation("Starting image upload for {FileName}", file.FileName);
        
        try
        {
            // Upload logic
            _logger.LogInformation("Successfully uploaded {FileName}", file.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload {FileName}", file.FileName);
            throw;
        }
    }
}
```

### Health Checks
```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PicBedDbContext>()
    .AddCheck("file-system", () => 
        Directory.Exists("wwwroot/uploads") ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy());

app.MapHealthChecks("/health");
```

### Metrics Collection
- **Application Metrics**: Custom counters and gauges
- **System Metrics**: CPU, memory, disk usage
- **Business Metrics**: Upload counts, user activity
- **Error Tracking**: Exception monitoring

## Future Enhancements

### Short-term Improvements
1. **User Management**: User registration and roles
2. **Image Editing**: Basic image manipulation
3. **Bulk Operations**: Batch upload/delete
4. **Search Functionality**: Full-text search
5. **API Rate Limiting**: Request throttling

### Medium-term Features
1. **Cloud Storage**: AWS S3, Azure Blob integration
2. **Image Optimization**: Automatic compression
3. **Advanced Authentication**: OAuth, 2FA
4. **Admin Dashboard**: Management interface
5. **Backup System**: Automated backups

### Long-term Vision
1. **Microservices**: Service decomposition
2. **Kubernetes**: Container orchestration
3. **Machine Learning**: Image recognition
4. **Mobile App**: Native mobile application
5. **Enterprise Features**: SSO, audit logs

### Technical Debt
1. **Security Hardening**: Enhanced security measures
2. **Performance Optimization**: Caching strategies
3. **Code Quality**: Improved test coverage
4. **Documentation**: API documentation
5. **Monitoring**: Comprehensive observability

## Conclusion

PicBed represents a modern, scalable approach to self-hosted image storage. The architecture balances simplicity with functionality, providing a solid foundation for future enhancements. The technology choices prioritize developer experience, performance, and maintainability while keeping the system lightweight and easy to deploy.

The modular design allows for incremental improvements and the containerized deployment ensures consistent environments across development and production. With proper monitoring and security measures, PicBed can serve as a reliable image storage solution for various use cases.
