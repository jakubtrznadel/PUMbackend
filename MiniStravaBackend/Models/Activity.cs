using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiniStravaBackend.Models
{
    public class Activity
    {
        [Key]
        public int ActivityId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty; 

        [Required]
        public double Duration { get; set; } 

        [Required]
        public double Distance { get; set; } 

        public double? Pace { get; set; } 

        public double? AverageSpeed { get; set; } 

        public string? GpsTrack { get; set; } 

        public string? Note { get; set; }

        public string? PhotoUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}
