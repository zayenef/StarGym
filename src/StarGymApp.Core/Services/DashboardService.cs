using StarGymApp.Core.Models;

namespace StarGymApp.Core.Services;

public class DashboardService
{
    public DashboardSummary GetSummary(IEnumerable<Member> members, IEnumerable<MembershipPlan> plans, IEnumerable<Payment> payments)
    {
        var totalMembers = members.Count();
        var memberRecords = members.ToList();
        var activeMembers = memberRecords.Count(member => member.SubscriptionStatus == "Active");
        var expiredMembers = memberRecords.Count(member => member.SubscriptionStatus == "Expired");
        var pendingPayments = payments.Count(p => p.Status == "Pending");
        var monthlyRevenue = payments
            .Where(p => p.PaymentDate >= DateTime.Today.AddDays(-30))
            .Sum(p => p.Amount);

        return new DashboardSummary
        {
            TotalMembers = totalMembers,
            ActiveMembers = activeMembers,
            ExpiredMembers = expiredMembers,
            PendingPayments = pendingPayments,
            MonthlyRevenue = monthlyRevenue
        };
    }
}

public class DashboardSummary
{
    public int TotalMembers { get; set; }
    public int ActiveMembers { get; set; }
    public int ExpiredMembers { get; set; }
    public int PendingPayments { get; set; }
    public decimal MonthlyRevenue { get; set; }
}
