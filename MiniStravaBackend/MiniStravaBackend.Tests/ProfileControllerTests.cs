using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using MiniStravaBackend.Controllers;
using MiniStravaBackend.Models;
using MiniStrava.Data;

namespace MiniStravaBackend.Tests
{
    public class ProfileControllerTests
    {
        private AppDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private void AuthenticateController(ProfileController controller, int userId)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("uid", userId.ToString()),
            }, "mock"));

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        [Fact]
        public async Task GetProfile_ReturnsOk_WhenUserExists()
        {
            var context = GetDatabaseContext();
            var userId = 100;

            context.Users.Add(new User
            {
                UserId = userId,
                Email = "test@test.com",
                FirstName = "Jan",
                LastName = "Kowalski"
            });
            await context.SaveChangesAsync();

            var mockEnv = new Mock<IWebHostEnvironment>();
            var controller = new ProfileController(context, mockEnv.Object);

            AuthenticateController(controller, userId);

            var result = await controller.GetProfile();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task UpdateProfile_ChangesData_WhenValid()
        {
            var context = GetDatabaseContext();
            var userId = 200;

            var user = new User
            {
                UserId = userId,
                FirstName = "StareImie",
                Weight = 80
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var mockEnv = new Mock<IWebHostEnvironment>();
            var controller = new ProfileController(context, mockEnv.Object);
            AuthenticateController(controller, userId);

            var dto = new ProfileController.UpdateProfileDto
            {
                FirstName = "NoweImie",
                Weight = 75
            };

            var result = await controller.UpdateProfile(dto);

            Assert.IsType<OkObjectResult>(result);

            var dbUser = await context.Users.FindAsync(userId);
            Assert.Equal("NoweImie", dbUser.FirstName);
            Assert.Equal(75, dbUser.Weight);
        }
    }
}