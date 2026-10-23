using System.Text.Json;

namespace StarGymApp.Core.Data;

public static class GymDataLoader
{
    public static async Task<GymDataSet> LoadAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        var data = JsonSerializer.Deserialize<GymDataSet>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return data ?? new GymDataSet();
    }
}

public class GymDataSet
{
    public List<MembershipPlanSeed>? MembershipPlans { get; set; } = new();
    public List<TrainerSeed>? Trainers { get; set; } = new();
    public List<MemberSeed>? Members { get; set; } = new();
    public List<PaymentSeed>? Payments { get; set; } = new();
    public List<AttendanceSeed>? Attendance { get; set; } = new();
}

public class MembershipPlanSeed
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class TrainerSeed
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class MemberSeed
{
    public int Id { get; set; }
    public string BadgeId { get; set; } = string.Empty;
    public string PhotoPath { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public DateTime JoinDate { get; set; }
    public int MembershipPlanId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? TrainerId { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class PaymentSeed
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class AttendanceSeed
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public DateTime CheckInDate { get; set; }
    public TimeSpan CheckInTime { get; set; }
    public TimeSpan? CheckOutTime { get; set; }
    public string Status { get; set; } = string.Empty;
}
