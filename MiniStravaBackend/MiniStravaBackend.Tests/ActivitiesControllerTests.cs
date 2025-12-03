using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using MiniStravaBackend.Controllers;
using MiniStravaBackend.Models;
using MiniStrava.Data;

namespace MiniStravaBackend.Tests
{
    public class ActivitiesControllerTests
    {
        private AppDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        private void AuthenticateController(ActivitiesController controller, int userId)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            }, "mock"));

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        [Fact]
        public async Task GetUserActivities_ReturnsOnlyUserActivities()
        {
            var context = GetDatabaseContext();
            var userId = 1;
            var otherUserId = 2;

            context.Activities.Add(new Activity { ActivityId = 1, UserId = userId, Name = "Bieg 1", CreatedAt = DateTime.UtcNow });
            context.Activities.Add(new Activity { ActivityId = 2, UserId = userId, Name = "Rower", CreatedAt = DateTime.UtcNow });
            context.Activities.Add(new Activity { ActivityId = 3, UserId = otherUserId, Name = "Inny user", CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var mockEnv = new Mock<IWebHostEnvironment>();
            var controller = new ActivitiesController(context, mockEnv.Object);
            AuthenticateController(controller, userId);

            var result = await controller.GetUserActivities();

            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var activities = Assert.IsAssignableFrom<IEnumerable<Activity>>(actionResult.Value);
            Assert.Equal(2, activities.Count());
        }

        [Fact]
        public async Task GetActivity_ReturnsNotFound_WhenActivityDoesNotExist()
        {
            var context = GetDatabaseContext();
            var userId = 1;

            var mockEnv = new Mock<IWebHostEnvironment>();
            var controller = new ActivitiesController(context, mockEnv.Object);
            AuthenticateController(controller, userId);

            var result = await controller.GetActivity(999);

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreateActivity_AddsActivityAndUpdatesStats()
        {
            var context = GetDatabaseContext();
            var userId = 10;

            var mockEnv = new Mock<IWebHostEnvironment>();
            var controller = new ActivitiesController(context, mockEnv.Object);
            AuthenticateController(controller, userId);

            var newActivity = new Activity
            {
                Name = "Test Run",
                Distance = 5.0,
                Duration = 30.0,
                AverageSpeed = 10.0
            };

            var result = await controller.CreateActivity(newActivity);

            Assert.IsType<CreatedAtActionResult>(result.Result);

            var dbActivity = await context.Activities.FirstOrDefaultAsync(a => a.UserId == userId);
            Assert.NotNull(dbActivity);
            Assert.Equal("Test Run", dbActivity.Name);

            var stats = await context.UserStats.FirstOrDefaultAsync(s => s.UserId == userId);
            Assert.NotNull(stats);
            Assert.Equal(1, stats.TotalWorkouts);
            Assert.Equal(5.0, stats.TotalDistance);
        }

        [Fact]
        public async Task DeleteActivity_RemovesFromDb_WhenOwnedByUser()
        {
            var context = GetDatabaseContext();
            var userId = 5;
            var activityId = 100;

            context.Activities.Add(new Activity { ActivityId = activityId, UserId = userId, Name = "Do usunięcia" });
            await context.SaveChangesAsync();

            var mockEnv = new Mock<IWebHostEnvironment>();
            var controller = new ActivitiesController(context, mockEnv.Object);
            AuthenticateController(controller, userId);

            var result = await controller.DeleteActivity(activityId);

            Assert.IsType<OkObjectResult>(result);

            var deletedActivity = await context.Activities.FindAsync(activityId);
            Assert.Null(deletedActivity);
        }
    }
}