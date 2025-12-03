namespace MiniStravaBackend.Models
{
    public class UserStats
    {
        public int UserStatsId { get; set; }
        public int UserId { get; set; }
        public int TotalWorkouts { get; set; }
        public double TotalDistance { get; set; }
        public double AverageSpeed { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public double MaxDistance { get; set; } 
        public double TotalDuration { get; set; }
        public double? FastestPace { get; set; } 
        public User User { get; set; } 
    }
}
