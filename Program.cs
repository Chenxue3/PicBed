using Microsoft.EntityFrameworkCore;
using PicBed.Data;
using PicBed.Services;
using PicBed.Middleware;
using PicBed.Models;
using Amazon.S3;

var builder = WebApplication.CreateBuilder(args);

// Configure port for Railway
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Entity Framework
builder.Services.AddDbContext<PicBedDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add AWS S3 services (optional for development)
var awsAccessKey = builder.Configuration["AWS:AccessKey"];
var awsSecretKey = builder.Configuration["AWS:SecretKey"];

if (!string.IsNullOrEmpty(awsAccessKey) && awsAccessKey != "test-access-key" &&
    !string.IsNullOrEmpty(awsSecretKey) && awsSecretKey != "test-secret-key")
{
    builder.Services.AddAWSService<IAmazonS3>();
}
else
{
    // Register a null S3 client for development
    builder.Services.AddSingleton<IAmazonS3>(provider => null!);
}

builder.Services.AddScoped<IS3StorageService, S3StorageService>();

// Add custom services
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseStaticFiles();

// Add authentication middleware
app.UseMiddleware<AuthMiddleware>();

app.UseAuthorization();
app.MapControllers();

// Add default route to serve index.html
app.MapFallbackToFile("index.html");

// Ensure database is created and seed default user
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PicBedDbContext>();
    
    try
    {
        // Ensure database is created
        context.Database.EnsureCreated();
        
        // Create admin user if it doesn't exist
        var adminUser = context.Users.FirstOrDefault(u => u.Username == "admin");
        if (adminUser == null)
        {
            // Get admin password from environment variable or use default
            var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "admin123";
            
            var defaultUser = new User
            {
                Username = "admin",
                PasswordHash = HashPassword(adminPassword),
                Email = "admin@picbed.com",
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
                IsActive = true
            };
            
            context.Users.Add(defaultUser);
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error initializing database");
        throw;
    }
}

// Helper method to hash password (same as in AuthService)
string HashPassword(string password)
{
    var secretKey = "PicBed-Secret-Key-2024";
    using var sha256 = System.Security.Cryptography.SHA256.Create();
    var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password + secretKey));
    return Convert.ToBase64String(hashedBytes);
}

app.Run();
