using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniStrava.Data;
using MiniStrava.Models;
using System.Security.Claims;

namespace MiniStravaBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class ProfileController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProfileController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = int.Parse(User.FindFirstValue("uid")!);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return NotFound();

            return Ok(new
            {
                user.UserId,
                user.Email,
                user.FirstName,
                user.LastName,
                user.BirthDate,
                user.Gender,
                user.Height,
                user.Weight,
                user.AvatarUrl
            });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = int.Parse(User.FindFirstValue("uid")!);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return NotFound();

            if (!string.IsNullOrEmpty(dto.FirstName)) user.FirstName = dto.FirstName;
            if (!string.IsNullOrEmpty(dto.LastName)) user.LastName = dto.LastName;
            if (dto.BirthDate.HasValue) user.BirthDate = dto.BirthDate.Value;
            if (!string.IsNullOrEmpty(dto.Gender)) user.Gender = dto.Gender;
            if (dto.Height.HasValue) user.Height = dto.Height.Value;
            if (dto.Weight.HasValue) user.Weight = dto.Weight.Value;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Profil zaktualizowany.", user });
        }



        [HttpPost("avatar")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> UploadAvatar([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Nie wybrano pliku." });

            var userId = int.Parse(User.FindFirstValue("uid")!);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return NotFound();

            var uploadsFolder = Path.Combine(_env.WebRootPath, "avatars");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{userId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            user.AvatarUrl = $"/avatars/{fileName}";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Avatar został zaktualizowany.", avatarUrl = user.AvatarUrl });
        }
        public class UpdateProfileDto
        {
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public DateTime? BirthDate { get; set; }
            public string? Gender { get; set; }
            public float? Height { get; set; }
            public float? Weight { get; set; }
        }

    }
}
