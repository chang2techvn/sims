using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SIMS.Models
{
    public class User : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(10)]
        public string? StudentCode { get; set; } // Mã sinh viên: BC00132

        public DateTime? DateOfBirth { get; set; } // Ngày sinh thay cho Age

        [StringLength(10)]
        public string? Gender { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(255)]
        public string? Avatar { get; set; }

        [Required]
        public string Role { get; set; } = "student"; // student, lecturer, admin

        // Navigation properties
        public virtual Student? Student { get; set; }
        public virtual Lecturer? Lecturer { get; set; }
        public virtual Admin? Admin { get; set; }
    }
}