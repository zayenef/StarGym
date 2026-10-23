namespace StarGymApp.Core.Models;

public class Attendance
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public DateTime CheckInDate { get; set; }
    public TimeSpan CheckInTime { get; set; }
    public TimeSpan? CheckOutTime { get; set; }
    public string Status { get; set; } = "Present";
}
