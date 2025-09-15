namespace PicBed.Models
{
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? Message { get; set; }
        public User? User { get; set; }
    }
}
