using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CarRentalSystem_API.Models
{
    [Index(nameof(IdempotencyKey), IsUnique = true)] // Ensure that IdempotencyKey is unique
    public class Idempotency
    {
        public int ID { get; set; }
        [Required]
        [MaxLength(100)]
        public string IdempotencyKey { get; set; } = string.Empty;
        [Required]
        [MaxLength(256)]
        public string RequestHash { get; set; } = string.Empty;
        [Required]
        [MaxLength(25)]
        public string Status { get; set; } = "Started"; // Possible values: "Started", "Completed", "Failed"
        public int? ResponseCode { get; set; }
        public string ResponseBody { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
