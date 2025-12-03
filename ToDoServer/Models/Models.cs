using System.ComponentModel.DataAnnotations;

namespace TodoApi.Models;

public class User
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    public ICollection<Item> Items { get; set; } = new List<Item>();
}

public class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
}


public class CreateItemDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string? Name { get; set; }
}

public class UpdateItemDto
{
    [StringLength(100, MinimumLength = 1)]
    public string? Name { get; set; }
    public bool IsComplete { get; set; }
}

public class RegisterDto
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string? Username { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string? Password { get; set; }
}

public class LoginDto
{
    [Required]
    public string? Username { get; set; }

    [Required]
    public string? Password { get; set; }
}
