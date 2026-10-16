using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Role
{
    [Key]
    public int RoleId { get; set; }
    public string Name { get; set; } = string.Empty; // "Organiser" or "Participant"

    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
}