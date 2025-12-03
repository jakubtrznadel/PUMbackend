using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniStrava.Data;
using MiniStravaBackend.Models;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
namespace MiniStravaBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ActivitiesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ActivitiesController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private class GpsPoint
        {
            public double lat { get; set; }
            public double lon { get; set; }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Activity>>> GetUserActivities()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var activities = await _context.Activities
              .Where(a => a.UserId == userId)
              .OrderByDescending(a => a.CreatedAt)
              .ToListAsync();

            return Ok(activities);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Activity>> GetActivity(int id)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var activity = await _context.Activities.FirstOrDefaultAsync(a => a.ActivityId == id && a.UserId == userId);

            if (activity == null)
                return NotFound("Nie znaleziono aktywności.");

            return Ok(activity);
        }

        [HttpPost]
        public async Task<ActionResult<Activity>> CreateActivity([FromBody] Activity activity)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            activity.UserId = userId;
            activity.CreatedAt = DateTime.UtcNow;

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            await UpdateUserStats(userId);

            return CreatedAtAction(nameof(GetActivity), new { id = activity.ActivityId }, activity);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateActivity(int id, [FromBody] Activity updated)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var activity = await _context.Activities.FirstOrDefaultAsync(a => a.ActivityId == id && a.UserId == userId);

            if (activity == null)
                return NotFound("Nie znaleziono aktywności.");

            activity.Name = updated.Name;
            activity.Type = updated.Type;
            activity.Duration = updated.Duration;
            activity.Distance = updated.Distance;
            activity.Pace = updated.Pace;
            activity.AverageSpeed = updated.AverageSpeed;
            activity.GpsTrack = updated.GpsTrack;
            activity.Note = updated.Note;

            await _context.SaveChangesAsync();
            await UpdateUserStats(userId);
            return Ok(activity);
        }

        [HttpPost("{activityId}/photo")]
        public async Task<IActionResult> UploadPhoto(int activityId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Nie wybrano pliku." });

            if (!TryGetUserId(out var userId)) return Unauthorized();

            var activity = await _context.Activities
              .FirstOrDefaultAsync(a => a.ActivityId == activityId && a.UserId == userId);

            if (activity == null)
                return NotFound(new { message = "Nie znaleziono aktywności lub nie należy do użytkownika." });

            var uploadsFolder = Path.Combine(_env.WebRootPath, "activities");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{activityId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            activity.PhotoUrl = $"/activities/{fileName}";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Zdjęcie zostało dodane do aktywności.", photoUrl = activity.PhotoUrl });
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteActivity(int id)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var activity = await _context.Activities.FirstOrDefaultAsync(a => a.ActivityId == id && a.UserId == userId);

            if (activity == null)
                return NotFound("Nie znaleziono aktywności.");

            _context.Activities.Remove(activity);
            await _context.SaveChangesAsync();
            await UpdateUserStats(userId);

            return Ok("Aktywność została usunięta.");
        }

        [HttpGet("stats")]
        public async Task<ActionResult<UserStats>> GetUserStats()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            await UpdateUserStats(userId);

            var stats = await _context.UserStats
              .FirstOrDefaultAsync(s => s.UserId == userId);

            if (stats == null)
            {
                return Ok(new UserStats
                {
                    UserId = userId,
                    TotalWorkouts = 0,
                    TotalDistance = 0.0,
                    AverageSpeed = 0.0,
                    MaxDistance = 0.0,
                    TotalDuration = 0.0,
                    FastestPace = null,
                    LastUpdated = DateTime.UtcNow
                });
            }

            return Ok(stats);
        }


        private async Task UpdateUserStats(int userId)
        {
            var userActivities = await _context.Activities
              .Where(a => a.UserId == userId)
              .ToListAsync();

            var stats = await _context.UserStats
              .FirstOrDefaultAsync(s => s.UserId == userId);

            if (stats == null)
            {
                stats = new UserStats { UserId = userId };
                _context.UserStats.Add(stats);
            }

            if (!userActivities.Any())
            {
                stats.TotalWorkouts = 0;
                stats.TotalDistance = 0;
                stats.AverageSpeed = 0;
                stats.MaxDistance = 0;
                stats.TotalDuration = 0;
                stats.FastestPace = null;
            }
            else
            {
                stats.TotalWorkouts = userActivities.Count;
                stats.TotalDistance = userActivities.Sum(a => a.Distance);

                var activitiesWithSpeed = userActivities.Where(a => a.AverageSpeed.HasValue && a.AverageSpeed > 0).ToList();
                if (activitiesWithSpeed.Any())
                    stats.AverageSpeed = activitiesWithSpeed.Average(a => a.AverageSpeed.Value);
                else
                    stats.AverageSpeed = 0;

                stats.MaxDistance = userActivities.Max(a => a.Distance);
                stats.TotalDuration = userActivities.Sum(a => a.Duration);

                var activitiesWithPace = userActivities.Where(a => a.Pace.HasValue && a.Pace > 0).ToList();
                if (activitiesWithPace.Any())
                    stats.FastestPace = activitiesWithPace.Min(a => a.Pace.Value);
                else
                    stats.FastestPace = null;
            }

            stats.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }


        [HttpGet("{id}/export/gpx")]
        public async Task<IActionResult> ExportToGpx(int id)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var activity = await _context.Activities
                .FirstOrDefaultAsync(a => a.ActivityId == id && a.UserId == userId);

            if (activity == null)
                return NotFound("Nie znaleziono aktywności.");

            if (string.IsNullOrEmpty(activity.GpsTrack))
                return BadRequest("Ta aktywność nie zawiera śladu GPS.");

            List<GpsPoint> trackPoints;
            try
            {
                trackPoints = JsonSerializer.Deserialize<List<GpsPoint>>(activity.GpsTrack, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                return BadRequest($"Błąd parsowania śladu GPS: {ex.Message}");
            }

            if (trackPoints == null || !trackPoints.Any())
                return BadRequest("Ślad GPS jest pusty.");

            var gpx = new StringBuilder();
            gpx.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            gpx.AppendLine("<gpx xmlns=\"http://www.topografix.com/gpx/1/1\" version=\"1.1\" creator=\"Sport+ App\">");

            gpx.AppendLine("  <metadata>");
            gpx.AppendLine($"    <name>{System.Security.SecurityElement.Escape(activity.Name)}</name>");
            gpx.AppendLine($"    <time>{activity.CreatedAt.ToUniversalTime():yyyy-MM-ddTHH:mm:ssZ}</time>");
            gpx.AppendLine("  </metadata>");

            gpx.AppendLine("  <trk>");
            gpx.AppendLine($"    <name>{System.Security.SecurityElement.Escape(activity.Name)}</name>");
            gpx.AppendLine("    <trkseg>");

            foreach (var point in trackPoints)
            {
                gpx.AppendLine($"      <trkpt lat=\"{point.lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" lon=\"{point.lon.ToString(System.Globalization.CultureInfo.InvariantCulture)}\"></trkpt>");
            }

            gpx.AppendLine("    </trkseg>");
            gpx.AppendLine("  </trk>");
            gpx.AppendLine("</gpx>");

            var bytes = Encoding.UTF8.GetBytes(gpx.ToString());
            return File(bytes, "application/gpx+xml", $"activity_{id}.gpx");
        }

        [HttpPost("recalculate-stats/{userId}")]
        public async Task<IActionResult> RecalculateStats(int userId)
        {
            await UpdateUserStats(userId);
            return Ok("Statystyki zostały przeliczone.");
        }

        [HttpGet("ranking")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<UserStatsRankingDto>>> GetRanking([FromQuery] string sortBy = "TotalWorkouts")
        {
            var allUserIds = await _context.Users.Select(u => u.UserId).ToListAsync();
            foreach (var userId in allUserIds)
            {
                await UpdateUserStats(userId);
            }

            var allStats = await _context.UserStats.Include(s => s.User).ToListAsync();

            IEnumerable<UserStats> sorted = sortBy.ToLower() switch
            {
                "totalworkouts" => allStats.OrderByDescending(s => s.TotalWorkouts),
                "totaldistance" => allStats.OrderByDescending(s => s.TotalDistance),
                "totalduration" => allStats.OrderByDescending(s => s.TotalDuration),
                "fastestpace" => allStats.Where(s => s.FastestPace.HasValue).OrderBy(s => s.FastestPace.Value),
                "averagespeed" => allStats.OrderByDescending(s => s.AverageSpeed),
                _ => allStats.OrderByDescending(s => s.TotalWorkouts)
            };

            var result = sorted.Select(s => new UserStatsRankingDto(s)).ToList();

            return Ok(result);
        }

        public class UserStatsRankingDto
        {
            public int UserId { get; set; }
            public string Email { get; set; } = string.Empty;
            public int TotalWorkouts { get; set; }
            public double TotalDistance { get; set; }
            public double TotalDuration { get; set; }
            public double? FastestPace { get; set; }
            public double AverageSpeed { get; set; }

            public UserStatsRankingDto() { }

            public UserStatsRankingDto(UserStats stats)
            {
                UserId = stats.UserId;
                Email = stats.User?.Email ?? "Unknown";
                TotalWorkouts = stats.TotalWorkouts;
                TotalDistance = stats.TotalDistance;
                TotalDuration = stats.TotalDuration;
                FastestPace = stats.FastestPace;
                AverageSpeed = stats.AverageSpeed;
            }
        }



        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim, out userId))
            {
                return false;
            }
            return true;
        }
    }
}