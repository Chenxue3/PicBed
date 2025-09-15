# PicBed - Self-hosted Image Storage

A modern, self-hosted image storage and management application built with ASP.NET Core 8.0.

## Features

- 🖼️ **Image Upload & Management** - Upload, view, and manage images with drag-and-drop support
- 🔐 **Authentication** - JWT-based authentication system
- 📱 **Responsive Design** - Modern UI built with Tailwind CSS
- 🖼️ **Thumbnail Generation** - Automatic thumbnail creation for uploaded images
- 📊 **Image Metadata** - Track file size, dimensions, upload time, and more
- 🏷️ **Categorization** - Organize images with categories and descriptions
- 🔍 **Search & Filter** - Find images quickly with built-in search functionality
- 🐳 **Docker Support** - Easy deployment with Docker containers
- ☁️ **Cloud Ready** - Deploy to Railway, Heroku, or any cloud platform

## Quick Start

### Local Development

1. **Clone the repository**
   ```bash
   git clone https://github.com/Chenxue3/PicBed.git
   cd PicBed
   ```

2. **Run the application**
   ```bash
   dotnet run --urls "http://localhost:5001"
   ```

3. **Access the application**
   - Open your browser and go to `http://localhost:5001`
   - Login with default credentials:
     - Username: `admin`
     - Password: `0507cptbtptp`

### Docker Deployment

1. **Build and run with Docker**
   ```bash
   docker build -t picbed .
   docker run -p 8080:8080 picbed
   ```

2. **Access the application**
   - Open your browser and go to `http://localhost:8080`

## Technology Stack

- **Backend**: ASP.NET Core 8.0
- **Database**: SQLite with Entity Framework Core
- **Frontend**: HTML, CSS (Tailwind), JavaScript (Alpine.js)
- **Image Processing**: SixLabors.ImageSharp
- **Authentication**: JWT tokens
- **Containerization**: Docker

## API Endpoints

### Authentication
- `POST /api/auth/login` - User login
- `POST /api/auth/validate` - Validate JWT token

### Images
- `GET /api/images` - Get all images (paginated)
- `POST /api/images/upload` - Upload new image
- `GET /api/images/{id}` - Get image details
- `GET /api/images/file/{fileName}` - Download image file
- `GET /api/images/thumb/{fileName}` - Get thumbnail
- `DELETE /api/images/{id}` - Delete image

## Configuration

### Environment Variables

- `PORT` - Application port (default: 8080)
- `ASPNETCORE_ENVIRONMENT` - Environment (Development/Production)
- `ConnectionStrings__DefaultConnection` - Database connection string

### Image Settings

- `ImageSettings__MaxFileSize` - Maximum file size in bytes (default: 10MB)
- `ImageSettings__UploadPath` - Upload directory (default: uploads)
- `ImageSettings__ThumbnailPath` - Thumbnail directory (default: thumbnails)
- `ImageSettings__AllowedExtensions` - Allowed file extensions

## Deployment

### Railway (Recommended)

See [RAILWAY_DEPLOYMENT.md](./RAILWAY_DEPLOYMENT.md) for detailed deployment instructions.

### Other Platforms

This application can be deployed to any platform that supports .NET 8.0:
- Heroku
- DigitalOcean App Platform
- Azure App Service
- AWS Elastic Beanstalk
- Google Cloud Run

## Security Features

- JWT-based authentication
- Password hashing with SHA256
- Input validation and sanitization
- File type validation
- File size limits
- CORS configuration

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

If you encounter any issues or have questions, please open an issue on GitHub.

## Changelog

### v1.0.0
- Initial release
- Image upload and management
- JWT authentication
- Responsive web interface
- Docker support
- Railway deployment ready
