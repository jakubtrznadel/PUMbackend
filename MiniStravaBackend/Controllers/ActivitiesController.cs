using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniStrava.Data;
using MiniStravaBackend.Models;
using System.Security.Claims;
using System.Text.Json;

namespace MiniStravaBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ActivitiesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        public ActivitiesController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Activity>>> GetUserActivities()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var activities = await _context.Activities
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return Ok(activities);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Activity>> GetActivity(int id)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var activity = await _context.Activities.FirstOrDefaultAsync(a => a.ActivityId == id && a.UserId == userId);

            if (activity == null)
                return NotFound("Nie znaleziono aktywności.");

            return Ok(activity);
        }

        [HttpPost]
        public async Task<ActionResult> CreateActivity([FromBody] Activity activity)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            activity.UserId = userId;
            activity.CreatedAt = DateTime.UtcNow;

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            await UpdateUserStats(userId);

            return Ok(activity);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateActivity(int id, [FromBody] Activity updated)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
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
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> UploadPhoto(int activityId, [FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Nie wybrano pliku." });

            var userId = int.Parse(User.FindFirstValue("uid")!);

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
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var activity = await _context.Activities.FirstOrDefaultAsync(a => a.ActivityId == id && a.UserId == userId);

            if (activity == null)
                return NotFound("Nie znaleziono aktywności.");

            _context.Activities.Remove(activity);
            await _context.SaveChangesAsync();

            return Ok("Aktywność została usunięta.");
        }

        private async Task UpdateUserStats(int userId)
        {
            var userActivities = await _context.Activities
                .Where(a => a.UserId == userId)
                .ToListAsync();

            if (!userActivities.Any())
                return;

            var totalWorkouts = userActivities.Count;
            var totalDistance = userActivities.Sum(a => a.Distance);
            var averageSpeed = userActivities.Average(a => a.AverageSpeed).GetValueOrDefault();

            var stats = await _context.UserStats
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (stats == null)
            {
                stats = new UserStats
                {
                    UserId = userId,
                    TotalWorkouts = totalWorkouts,
                    TotalDistance = totalDistance,
                    AverageSpeed = averageSpeed,
                    LastUpdated = DateTime.UtcNow
                };
                _context.UserStats.Add(stats);
            }
            else
            {
                stats.TotalWorkouts = totalWorkouts;
                stats.TotalDistance = totalDistance;
                stats.AverageSpeed = averageSpeed;
                stats.LastUpdated = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

    }
}
