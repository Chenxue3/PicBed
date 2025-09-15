using System.ComponentModel.DataAnnotations;

namespace PicBed.Models
{
    public class ImageRecord
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(500)]
        public string OriginalFileName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string FileExtension { get; set; } = string.Empty;
        
        public long FileSize { get; set; }
        
        public int Width { get; set; }
        
        public int Height { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string MimeType { get; set; } = string.Empty;
        
        public DateTime UploadTime { get; set; } = DateTime.UtcNow;
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        [MaxLength(100)]
        public string? Category { get; set; }
        
        public bool IsPublic { get; set; } = true;
    }
}
