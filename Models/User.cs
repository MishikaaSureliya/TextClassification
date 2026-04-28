using System.ComponentModel.DataAnnotations;

namespace TextClassification.Models
{
    public class User : BaseEntity
    {
        public int Id { get; set; }

        [Required]
        [RegularExpression(@"^[A-Za-z]+$", ErrorMessage = "Name must contain only characters")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;
    }
}
