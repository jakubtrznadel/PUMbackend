using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using MiniStrava.Controllers;
using MiniStrava.Data;
using MiniStravaBackend.Models;
using MiniStrava.Models;

namespace MiniStravaBackend.Tests
{
    public class AdminControllerTests
    {
        private AppDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public void Index_ReturnsView_WithStatistics()
        {
            var context = GetDatabaseContext();
            context.Users.Add(new User { UserId = 1, Email = "u1@a.pl" });
            context.Users.Add(new User { UserId = 2, Email = "u2@a.pl" });
            context.Activities.Add(new Activity { ActivityId = 1, Distance = 10.0 });
            context.SaveChanges();

            var mockLogger = new Mock<ILogger<AdminController>>();
            var controller = new AdminController(context, mockLogger.Object);

            var result = controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.Equal(2, viewResult.ViewData["UserCount"]);
            Assert.Equal(10.0, viewResult.ViewData["TotalDistance"]);
        }

        [Fact]
        public async Task Users_ReturnsView_WithListOfUsers()
        {
            var context = GetDatabaseContext();
            context.Users.Add(new User { UserId = 1, Email = "kasia@test.pl", FirstName = "Kasia", CreatedAt = DateTime.UtcNow });
            context.Users.Add(new User { UserId = 2, Email = "tomek@test.pl", FirstName = "Tomek", CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var mockLogger = new Mock<ILogger<AdminController>>();
            var controller = new AdminController(context, mockLogger.Object);

            var result = await controller.Users(null, null, null, null);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<User>>(viewResult.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task DeleteUser_RemovesUserAndRedirects()
        {
            var context = GetDatabaseContext();
            var userId = 99;
            context.Users.Add(new User { UserId = userId, Email = "delete@me.pl" });
            await context.SaveChangesAsync();

            var mockLogger = new Mock<ILogger<AdminController>>();
            var controller = new AdminController(context, mockLogger.Object);
            controller.TempData = new Mock<ITempDataDictionary>().Object;

            var result = await controller.DeleteUser(userId);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Users", redirectResult.ActionName);

            var deletedUser = await context.Users.FindAsync(userId);
            Assert.Null(deletedUser);
        }
    }
}