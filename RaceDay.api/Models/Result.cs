using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Result
{
    [Key]
    public int ResultId { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }

    public int EnrolmentId { get; set; }
    public Enrolment Enrolment { get; set; } = null!;
}
