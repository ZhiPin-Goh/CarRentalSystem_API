using System.ComponentModel.DataAnnotations.Schema;

namespace CarRentalSystem_API.Models
{
    public class TokenActivity
    {
        public int ID { get; set; }
        public string Token { get; set; }
        public string Message { get; set; }
        public string Role { get; set; }
        public DateTime Time { get; set; }
        public string AllowAccessToken { get; set; }  
        public string AllowRefreshToken { get; set; }
        public int? UserID { get; set; }
        [ForeignKey("UserID")]
        public virtual User? User { get; set; }
    }
}
