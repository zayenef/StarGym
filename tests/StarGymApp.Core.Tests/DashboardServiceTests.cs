using StarGymApp.Core.Data;
using StarGymApp.Core.Models;
using StarGymApp.Core.Services;
using Xunit;

namespace StarGymApp.Core.Tests;

public class DashboardServiceTests
{
    [Fact]
    public void GetSummary_CountsMembersAndPendingPayments()
    {
        var members = new[]
        {
            new Member { Status = "Active" },
            new Member { Status = "Expired" },
            new Member { Status = "Active" }
        };
        var payments = new[]
        {
            new Payment { Status = "Pending" },
            new Payment { Status = "Paid" }
        };

        var summary = new DashboardService().GetSummary(members, Array.Empty<MembershipPlan>(), payments);

        Assert.Equal(3, summary.TotalMembers);
        Assert.Equal(2, summary.ActiveMembers);
        Assert.Equal(1, summary.ExpiredMembers);
        Assert.Equal(1, summary.PendingPayments);
    }

    [Fact]
    public void GetSummary_UsesCalculatedSubscriptionExpiry()
    {
        var members = new[]
        {
            new Member { Status = "Active", MembershipExpiresOn = DateTime.Today.AddDays(-1) },
            new Member { Status = "Active", MembershipExpiresOn = DateTime.Today.AddDays(1) }
        };

        var summary = new DashboardService().GetSummary(members, Array.Empty<MembershipPlan>(), Array.Empty<Payment>());

        Assert.Equal(1, summary.ActiveMembers);
        Assert.Equal(1, summary.ExpiredMembers);
    }

    [Fact]
    public void GetSummary_OnlyIncludesPaymentsFromLastThirtyDays()
    {
        var payments = new[]
        {
            new Payment { Amount = 100, PaymentDate = DateTime.Today.AddDays(-5) },
            new Payment { Amount = 250, PaymentDate = DateTime.Today.AddDays(-31) }
        };

        var summary = new DashboardService().GetSummary(
            Array.Empty<Member>(),
            Array.Empty<MembershipPlan>(),
            payments);

        Assert.Equal(100, summary.MonthlyRevenue);
    }

    [Fact]
    public void LargeDatasetGenerator_CapsDatasetAtThreeHundredFiftyMembers()
    {
        var data = new GymDataSet();

        LargeDatasetGenerator.EnsureSize(data, 1_000);

        Assert.NotNull(data.Members);
        Assert.NotNull(data.Payments);
        Assert.NotNull(data.Attendance);
        Assert.True(data.Members.Count <= LargeDatasetGenerator.MaxRecords);
        Assert.True(data.Payments.Count <= LargeDatasetGenerator.MaxRecords);
        Assert.True(data.Attendance.Count <= LargeDatasetGenerator.MaxRecords);
        Assert.Equal(350, data.Members.Count);
    }
}
