# Render 部署指南

## 🚀 使用 Render 免费部署 PicBed

Render 是一个优秀的免费部署平台，支持 .NET 应用程序。

### 第一步：准备部署

1. **确保代码已推送到 GitHub**
   ```bash
   git add .
   git commit -m "Prepare for Render deployment"
   git push origin main
   ```

### 第二步：在 Render 上部署

1. **访问 [Render](https://render.com)**
2. **使用 GitHub 账号登录**
3. **点击 "New +" 按钮**
4. **选择 "Web Service"**

### 第三步：配置服务

1. **连接仓库**：
   - 选择 "Build and deploy from a Git repository"
   - 点击 "Connect account" 连接 GitHub
   - 选择 `Chenxue3/PicBed` 仓库

2. **基本设置**：
   - **Name**: `picbed` (或你喜欢的名称)
   - **Environment**: `Docker`
   - **Region**: 选择离你最近的区域
   - **Branch**: `main`
   - **Root Directory**: 留空
   - **Dockerfile Path**: `./Dockerfile`

3. **环境变量**：
   在 "Environment Variables" 部分添加：
   ```
   ASPNETCORE_ENVIRONMENT=Production
   ASPNETCORE_URLS=http://0.0.0.0:$PORT
   ConnectionStrings__DefaultConnection=Data Source=picbed.db
   ImageSettings__MaxFileSize=10485760
   ImageSettings__UploadPath=uploads
   ImageSettings__ThumbnailPath=thumbnails
   ImageSettings__AllowedExtensions__0=.jpg
   ImageSettings__AllowedExtensions__1=.jpeg
   ImageSettings__AllowedExtensions__2=.png
   ImageSettings__AllowedExtensions__3=.gif
   ImageSettings__AllowedExtensions__4=.webp
   ADMIN_PASSWORD=your_secure_admin_password_here
   AWS__AccessKey=your_aws_access_key
   AWS__SecretKey=your_aws_secret_key
   AWS__Region=us-east-1
   AWS__S3BucketName=your-s3-bucket-name
   ```

4. **高级设置**：
   - **Auto-Deploy**: 开启（代码推送时自动部署）
   - **Pull Request Previews**: 可选开启

### 第四步：部署

1. **点击 "Create Web Service"**
2. **等待构建完成**（大约 5-10 分钟）
3. **查看部署日志**确保没有错误

### 第五步：访问应用

1. **部署完成后，Render 会提供一个 URL**
2. **格式类似**: `https://picbed-xxxx.onrender.com`
3. **使用 admin 账户登录**：
   - 用户名: `admin`
   - 密码: 你设置的环境变量 `ADMIN_PASSWORD` 的值
   - 如果没有设置环境变量，默认密码是 `admin123`

## ☁️ AWS S3 配置

### 重要说明
PicBed 现在使用 AWS S3 进行图片存储，确保数据持久化。部署前需要：

1. **创建 AWS S3 存储桶**
2. **设置 IAM 用户和权限**
3. **配置环境变量**

详细设置步骤请参考：[AWS S3 设置指南](./AWS_S3_SETUP.md)

### 必需的环境变量
- `AWS__AccessKey`: AWS 访问密钥 ID
- `AWS__SecretKey`: AWS 秘密访问密钥  
- `AWS__Region`: AWS 区域 (如 us-east-1)
- `AWS__S3BucketName`: S3 存储桶名称

## 🔒 安全说明

### Admin 密码设置
- **重要**: 请务必设置一个强密码作为 `ADMIN_PASSWORD` 环境变量
- **不要使用默认密码**: 默认密码 `admin123` 仅用于开发环境
- **生产环境**: 必须设置复杂的环境变量密码

### 密码建议
- 至少 12 个字符
- 包含大小写字母、数字和特殊字符
- 例如: `MySecure@Pass123!`

## 🔧 故障排除

### 常见问题

1. **构建失败**：
   - 检查 Dockerfile 是否正确
   - 确保所有依赖都在 PicBed.csproj 中

2. **应用无法启动**：
   - 检查环境变量是否正确设置
   - 查看 Render 的部署日志

3. **数据库问题**：
   - 免费计划中数据会在应用重启时丢失
   - 这是正常现象，适合测试和演示

### 查看日志

1. 在 Render 控制台中点击你的服务
2. 点击 "Logs" 标签
3. 查看实时日志和错误信息

## 📊 Render 免费计划限制

- **750 小时/月**（足够小型应用使用）
- **512MB RAM**
- **应用会在 15 分钟无活动后休眠**
- **数据不持久化**（重启后丢失）

## 🎯 优化建议

1. **启用健康检查**：
   - 在 Render 设置中添加健康检查路径 `/`

2. **自定义域名**：
   - 免费计划支持自定义域名
   - 在 "Settings" > "Custom Domains" 中配置

3. **监控**：
   - 使用 Render 的内置监控功能
   - 设置告警通知

## 🚀 升级选项

如果需要更好的性能：
- **Starter Plan**: $7/月
- **Standard Plan**: $25/月
- 包含持久化存储和更多资源
