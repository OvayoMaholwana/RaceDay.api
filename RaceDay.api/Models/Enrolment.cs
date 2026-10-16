using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Enrolment
{
    [Key]
    public int EnrolmentId { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Confirmed, Cancelled

    public int ParticipantId { get; set; }
    public AppUser Participant { get; set; } = null!;

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public Result? Result { get; set; }
}