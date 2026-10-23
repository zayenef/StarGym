using StarGymApp.Core.Models;
using StarGymApp.Core.Services;
using Xunit;

namespace StarGymApp.Core.Tests;

public class ReportingServiceTests
{
    [Fact]
    public void GetAttendanceKpis_UsesProvidedDateForDayAndMonthBoundaries()
    {
        var referenceDate = new DateTime(2026, 10, 7, 12, 0, 0);
        var attendance = new[]
        {
            new Attendance { MemberId = 1, CheckInDate = referenceDate.Date, CheckInTime = TimeSpan.FromHours(11), CheckOutTime = null },
            new Attendance { MemberId = 2, CheckInDate = referenceDate.Date, CheckInTime = TimeSpan.FromHours(11), CheckOutTime = TimeSpan.FromHours(11.5) },
            new Attendance { MemberId = 3, CheckInDate = new DateTime(2026, 10, 1) },
            new Attendance { MemberId = 4, CheckInDate = new DateTime(2026, 9, 30) }
        };

        var kpis = new ReportingService().GetAttendanceKpis(attendance, referenceDate);

        Assert.Equal(1, kpis.MembersInGymNow);
        Assert.Equal(2, kpis.TodaysCheckIns);
        Assert.Equal(3, kpis.MonthlyAttendance);
    }

    [Fact]
    public void GetAttendanceKpis_ExpiresVisitorsAfterTwoHoursAndCountsEachMemberOnce()
    {
        var referenceTime = new DateTime(2026, 10, 7, 12, 0, 0);
        var attendance = new[]
        {
            new Attendance { Id = 1, MemberId = 1, CheckInDate = referenceTime.Date, CheckInTime = TimeSpan.FromHours(10), CheckOutTime = null },
            new Attendance { Id = 2, MemberId = 1, CheckInDate = referenceTime.Date, CheckInTime = TimeSpan.FromHours(11), CheckOutTime = null },
            new Attendance { Id = 3, MemberId = 2, CheckInDate = referenceTime.Date, CheckInTime = TimeSpan.FromHours(9) + TimeSpan.FromMinutes(59), CheckOutTime = null },
            new Attendance { Id = 4, MemberId = 3, CheckInDate = referenceTime.Date.AddDays(-1), CheckInTime = TimeSpan.FromHours(23), CheckOutTime = null },
            new Attendance { Id = 5, MemberId = 4, CheckInDate = referenceTime.Date, CheckInTime = TimeSpan.FromHours(11), CheckOutTime = TimeSpan.FromHours(11.5) },
            new Attendance { Id = 6, MemberId = 5, CheckInDate = referenceTime.Date, CheckInTime = TimeSpan.FromHours(10), CheckOutTime = null }
        };

        var kpis = new ReportingService().GetAttendanceKpis(attendance, referenceTime);

        Assert.Equal(2, kpis.MembersInGymNow);
        Assert.Equal(5, kpis.TodaysCheckIns);
    }

    [Fact]
    public void GetSummary_CalculatesOperationalReports()
    {
        var members = new[]
        {
            new Member { MembershipPlanId = 1, JoinDate = DateTime.Today.AddDays(-31) },
            new Member { MembershipPlanId = 1, JoinDate = DateTime.Today.AddDays(-2) }
        };
        var plans = new[] { new MembershipPlan { Id = 1, DurationDays = 30 } };
        var payments = new[]
        {
            new Payment { Amount = 200, Status = "Paid" },
            new Payment { Amount = 100, Status = "Pending" }
        };
        var attendance = new[] { new Attendance { CheckInDate = DateTime.Today } };

        var summary = new ReportingService().GetSummary(members, plans, payments, attendance);

        Assert.Equal(200, summary.PaidRevenue);
        Assert.Equal(1, summary.PendingPayments);
        Assert.Equal(1, summary.ExpiringMembers);
        Assert.Equal(1, summary.AttendanceThisMonth);
    }
}
