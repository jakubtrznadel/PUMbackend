using Microsoft.EntityFrameworkCore;
using MiniStrava.Models;
using MiniStravaBackend.Models;
using System.Collections.Generic;

namespace MiniStrava.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Admin> Admins { get; set; }
        public DbSet<User> Users { get; set; }

        public DbSet<Activity> Activities { get; set; }
        public DbSet<UserStats> UserStats { get; set; }
    }
}
