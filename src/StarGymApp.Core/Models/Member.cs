namespace StarGymApp.Core.Models;

public class Member
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
    public int MembershipDurationMonths { get; set; }
    public decimal MembershipPrice { get; set; }
    public string MembershipTermLabel => MembershipDurationMonths is >= 1 and <= 12
        ? $"{MembershipDurationMonths} months"
        : "Plan term";
    public string Status { get; set; } = "Active";
    public int? TrainerId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime? MembershipExpiresOn { get; set; }
    public string SubscriptionStatus => MembershipExpiresOn is DateTime expiryDate && DateTime.Today > expiryDate.Date
        ? "Expired"
        : Status;

    public DateTime? GetMembershipExpiry(MembershipPlan? plan)
    {
        if (MembershipDurationMonths is >= 1 and <= 12)
        {
            return JoinDate.Date.AddMonths(MembershipDurationMonths);
        }

        return plan is null ? null : JoinDate.Date.AddDays(plan.DurationDays);
    }

    public int Age
    {
        get
        {
            var today = DateTime.Today;
            var age = today.Year - DateOfBirth.Year;
            return DateOfBirth.Date > today.AddYears(-age) ? age - 1 : age;
        }
    }
}
