using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using MiniStravaBackend.Controllers;
using MiniStravaBackend.Models;
using MiniStravaBackend.Services;
using MiniStrava.Data;

namespace MiniStravaBackend.Tests
{
    public class AuthControllerTests
    {
        private AppDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        private IConfiguration GetMockConfiguration()
        {
            var mockConfig = new Mock<IConfiguration>();

            mockConfig.Setup(c => c["Jwt:Key"]).Returns("BardzoTajnyKluczDoTestow123456789");
            mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("MiniStravaTest");

            var mockSmtpSection = new Mock<IConfigurationSection>();
            mockSmtpSection.Setup(s => s["Host"]).Returns("localhost");
            mockSmtpSection.Setup(s => s["User"]).Returns("test");
            mockSmtpSection.Setup(s => s["Pass"]).Returns("test");

            mockConfig.Setup(c => c.GetSection("Smtp")).Returns(mockSmtpSection.Object);

            return mockConfig.Object;
        }

        [Fact]
        public async Task Register_ReturnsOk_WhenUserIsNew()
        {
            var context = GetDatabaseContext();
            var config = GetMockConfiguration();
            var emailService = new EmailService(config);
            var controller = new AuthController(context, config, emailService);

            var dto = new RegisterDto
            {
                Email = "nowy@example.com",
                Password = "SuperHaslo123!"
            };

            var result = await controller.Register(dto);

            Assert.IsType<OkObjectResult>(result);

            var userInDb = await context.Users.FirstOrDefaultAsync(u => u.Email == "nowy@example.com");
            Assert.NotNull(userInDb);
            Assert.NotEqual("SuperHaslo123!", userInDb.PasswordHash);
        }

        [Fact]
        public async Task Register_ReturnsBadRequest_WhenUserAlreadyExists()
        {
            var context = GetDatabaseContext();
            context.Users.Add(new User
            {
                Email = "zajety@example.com",
                PasswordHash = "hash"
            });
            await context.SaveChangesAsync();

            var config = GetMockConfiguration();
            var emailService = new EmailService(config);
            var controller = new AuthController(context, config, emailService);

            var dto = new RegisterDto
            {
                Email = "zajety@example.com",
                Password = "InneHaslo"
            };

            var result = await controller.Register(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Login_ReturnsToken_WhenCredentialsAreCorrect()
        {
            var context = GetDatabaseContext();
            var password = "MojeHaslo123";
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            context.Users.Add(new User
            {
                Email = "login@example.com",
                PasswordHash = passwordHash,
                UserId = 1
            });
            await context.SaveChangesAsync();

            var config = GetMockConfiguration();
            var emailService = new EmailService(config);
            var controller = new AuthController(context, config, emailService);

            var dto = new LoginDto
            {
                Email = "login@example.com",
                Password = password
            };

            var result = await controller.Login(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
            Assert.Contains("token", json);
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_WhenPasswordIsWrong()
        {
            var context = GetDatabaseContext();
            var correctPassword = "DobreHaslo";

            context.Users.Add(new User
            {
                Email = "zlehaslo@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(correctPassword)
            });
            await context.SaveChangesAsync();

            var config = GetMockConfiguration();
            var emailService = new EmailService(config);
            var controller = new AuthController(context, config, emailService);

            var dto = new LoginDto
            {
                Email = "zlehaslo@example.com",
                Password = "ZleHaslo123"
            };

            var result = await controller.Login(dto);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_WhenUserDoesNotExist()
        {
            var context = GetDatabaseContext();
            var config = GetMockConfiguration();
            var emailService = new EmailService(config);
            var controller = new AuthController(context, config, emailService);

            var dto = new LoginDto
            {
                Email = "nieznany@example.com",
                Password = "Haslo"
            };

            var result = await controller.Login(dto);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }
    }
}