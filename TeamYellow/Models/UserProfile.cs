using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Authentication;
using Microsoft.EntityFrameworkCore.Migrations.Operations;


namespace TeamYellow.Models;

public class UserProfile
{
    public int UserProfileId { get; set; }

    [Required, MaxLength(50)]
    public string FirstName { get; set; } = String.Empty;

    [Required, MaxLength(50)]
    public string LastName { get; set; } = String.Empty;
    [MaxLength(20)]
    public string? Phone { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string? ProfilePhotoUrl { get; set; }

    public int? UnitNumber { get; set; }

    [MaxLength(120)]
    public string? Street { get; set; }
    [MaxLength(80)]
    public string? City { get; set; }
    [MaxLength(2)]
    public string? Province { get; set; }
    [MaxLength(7)]
    public string? PostalCode { get; set; }

    [Required]
    public string FkUserId { get; set; } = String.Empty;
}
