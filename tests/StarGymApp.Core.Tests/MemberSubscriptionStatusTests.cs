using StarGymApp.Core.Models;
using Xunit;

namespace StarGymApp.Core.Tests;

public class MemberSubscriptionStatusTests
{
    [Fact]
    public void SubscriptionStatus_IsActive_ThroughExpiryDate()
    {
        var member = new Member
        {
            Status = "Active",
            MembershipExpiresOn = DateTime.Today
        };

        Assert.Equal("Active", member.SubscriptionStatus);
    }

    [Fact]
    public void SubscriptionStatus_IsExpired_AfterExpiryDate()
    {
        var member = new Member
        {
            Status = "Active",
            MembershipExpiresOn = DateTime.Today.AddDays(-1)
        };

        Assert.Equal("Expired", member.SubscriptionStatus);
    }

    [Fact]
    public void SubscriptionStatus_PreservesNonActiveMemberStatus_BeforeExpiry()
    {
        var member = new Member
        {
            Status = "Paused",
            MembershipExpiresOn = DateTime.Today.AddDays(7)
        };

        Assert.Equal("Paused", member.SubscriptionStatus);
    }

    [Fact]
    public void GetMembershipExpiry_UsesSelectedMonthDuration()
    {
        var startDate = new DateTime(2026, 1, 31);
        var member = new Member
        {
            JoinDate = startDate,
            MembershipDurationMonths = 1
        };
        var plan = new MembershipPlan { DurationDays = 90 };

        Assert.Equal(startDate.AddMonths(1), member.GetMembershipExpiry(plan));
    }

    [Fact]
    public void GetMembershipExpiry_FallsBackToPlanDurationForExistingMembers()
    {
        var startDate = new DateTime(2026, 1, 1);
        var member = new Member { JoinDate = startDate };
        var plan = new MembershipPlan { DurationDays = 60 };

        Assert.Equal(startDate.AddDays(60), member.GetMembershipExpiry(plan));
    }
}
