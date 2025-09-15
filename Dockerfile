# Use the official .NET 8 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the official .NET 8 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PicBed.csproj", "."]
RUN dotnet restore "PicBed.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "PicBed.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PicBed.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create directories for uploads and thumbnails
RUN mkdir -p /app/wwwroot/uploads /app/wwwroot/thumbnails

ENTRYPOINT ["dotnet", "PicBed.dll"]
