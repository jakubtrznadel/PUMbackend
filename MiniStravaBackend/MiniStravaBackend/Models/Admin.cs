using System.ComponentModel.DataAnnotations.Schema;

namespace MiniStrava.Models
{
    public class Admin
    {
        public int Id { get; set; }
        public string Username { get; set; } = "Admin";
        public string? PasswordHash { get; set; }
        public bool IsPasswordSet { get; set; }

        [NotMapped]
        public bool HasPassword => !string.IsNullOrEmpty(PasswordHash);

    }
}