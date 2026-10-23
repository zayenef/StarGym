using StarGymApp.Core.Models;
using StarGymApp.Core.Services;
using Xunit;

namespace StarGymApp.Core.Tests;

public class MonthlyKpiTests
{
    [Fact]
    public void GetMonthlyKpis_GroupsRevenueAndAttendanceByMonth()
    {
        var payments = new[]
        {
            new Payment { Amount = 250, Status = "Paid", PaymentDate = new DateTime(2025, 8, 2) },
            new Payment { Amount = 450, Status = "Paid", PaymentDate = new DateTime(2025, 8, 20) },
            new Payment { Amount = 700, Status = "Pending", PaymentDate = new DateTime(2025, 9, 1) }
        };
        var attendance = new[]
        {
            new Attendance { CheckInDate = new DateTime(2025, 8, 2) },
            new Attendance { CheckInDate = new DateTime(2025, 8, 3) },
            new Attendance { CheckInDate = new DateTime(2025, 9, 1) }
        };

        var result = new ReportingService().GetMonthlyKpis(payments, attendance);
        var august = result.Single(point => point.MonthLabel == "Aug 25");

        Assert.Equal(700, august.Revenue);
        Assert.Equal(2, august.Attendance);
        Assert.True(august.RevenueBarWidth > 0);
        Assert.True(august.AttendanceBarWidth > 0);
    }
}
