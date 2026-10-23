using StarGymApp.Core.Helpers;
using StarGymApp.Core.Models;
using System.Globalization;

namespace StarGymApp.Core.Services;

public sealed class ReportingService
{
    private static readonly TimeSpan ExpectedVisitDuration = TimeSpan.FromHours(2);

    public (int MembersInGymNow, int TodaysCheckIns, int MonthlyAttendance) GetAttendanceKpis(IEnumerable<Attendance> attendance, DateTime referenceTime)
    {
        var records = attendance.ToList();
        var date = referenceTime.Date;
        var recordsToday = records.Where(record => record.CheckInDate.Date == date).ToList();
        var membersInGymNow = records
            .GroupBy(record => record.MemberId)
            .Select(group => group
                .OrderByDescending(record => record.CheckInDate.Date + record.CheckInTime)
                .ThenByDescending(record => record.Id)
                .First())
            .Count(record =>
            {
                var checkedInAt = record.CheckInDate.Date + record.CheckInTime;
                var elapsed = referenceTime - checkedInAt;
                return record.CheckOutTime is null
                    && elapsed >= TimeSpan.Zero
                    && elapsed <= ExpectedVisitDuration;
            });

        return (
            MembersInGymNow: membersInGymNow,
            TodaysCheckIns: recordsToday.Count,
            MonthlyAttendance: records.Count(record => record.CheckInDate.Year == date.Year && record.CheckInDate.Month == date.Month));
    }

    public ReportSummary GetSummary(IEnumerable<Member> members, IEnumerable<MembershipPlan> plans, IEnumerable<Payment> payments, IEnumerable<Attendance> attendance)
    {
        var planById = plans.ToDictionary(plan => plan.Id);
        var memberList = members.ToList();
        var paymentList = payments.ToList();
        var attendanceList = attendance.ToList();

        return new ReportSummary
        {
            PaidRevenue = paymentList.Where(payment => payment.Status == "Paid").Sum(payment => payment.Amount),
            PendingPayments = paymentList.Count(payment => payment.Status is "Pending" or "Overdue"),
            ExpiringMembers = memberList.Count(member =>
                member.GetMembershipExpiry(planById.GetValueOrDefault(member.MembershipPlanId)) is DateTime expiry
                && DateTime.Today > expiry.Date),
            AttendanceThisMonth = attendanceList.Count(record => record.CheckInDate.Year == DateTime.Today.Year && record.CheckInDate.Month == DateTime.Today.Month)
        };
    }

    public IReadOnlyList<MonthlyKpiPoint> GetMonthlyKpis(IEnumerable<Payment> payments, IEnumerable<Attendance> attendance)
    {
        var paymentList = payments.ToList();
        var attendanceList = attendance.ToList();
        var dates = paymentList.Select(payment => payment.PaymentDate.Date)
            .Concat(attendanceList.Select(record => record.CheckInDate.Date))
            .ToList();

        if (dates.Count == 0)
        {
            return Array.Empty<MonthlyKpiPoint>();
        }

        var firstMonth = new DateTime(dates.Min().Year, dates.Min().Month, 1);
        var lastMonth = new DateTime(dates.Max().Year, dates.Max().Month, 1);
        var months = Enumerable.Range(0, ((lastMonth.Year - firstMonth.Year) * 12) + lastMonth.Month - firstMonth.Month + 1)
            .Select(offset => firstMonth.AddMonths(offset))
            .TakeLast(12)
            .ToList();
        var maxRevenue = Math.Max(1, months.Max(month => paymentList.Where(payment => payment.Status == "Paid" && payment.PaymentDate.Year == month.Year && payment.PaymentDate.Month == month.Month).Sum(payment => payment.Amount)));
        var maxAttendance = Math.Max(1, months.Max(month => attendanceList.Count(record => record.CheckInDate.Year == month.Year && record.CheckInDate.Month == month.Month)));

        return months.Select(month =>
        {
            var revenue = paymentList.Where(payment => payment.Status == "Paid" && payment.PaymentDate.Year == month.Year && payment.PaymentDate.Month == month.Month).Sum(payment => payment.Amount);
            var attendanceCount = attendanceList.Count(record => record.CheckInDate.Year == month.Year && record.CheckInDate.Month == month.Month);
            return new MonthlyKpiPoint
            {
                MonthLabel = month.ToString("MMM yy", CultureInfo.InvariantCulture),
                Revenue = revenue,
                Attendance = attendanceCount,
                RevenueBarWidth = (double)revenue / (double)maxRevenue * 300,
                AttendanceBarWidth = (double)attendanceCount / maxAttendance * 300
            };
        }).ToList();
    }

    public IReadOnlyList<RevenuePeriodPoint> GetRevenueByMonth(IEnumerable<Payment> payments, int year)
    {
        return Enumerable.Range(1, 12).Select(month =>
        {
            var revenue = payments.Where(payment => payment.Status == "Paid" && payment.PaymentDate.Year == year && payment.PaymentDate.Month == month).Sum(payment => payment.Amount);
            return new RevenuePeriodPoint
            {
                PeriodLabel = CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(month),
                Revenue = revenue
            };
        }).ToList();
    }

    public IReadOnlyList<RevenuePeriodPoint> GetRevenueByYear(IEnumerable<Payment> payments)
    {
        return payments.Where(payment => payment.Status == "Paid")
            .GroupBy(payment => payment.PaymentDate.Year)
            .OrderBy(group => group.Key)
            .Select(group => new RevenuePeriodPoint { PeriodLabel = group.Key.ToString(CultureInfo.InvariantCulture), Revenue = group.Sum(payment => payment.Amount) })
            .ToList();
    }
}

public sealed class RevenuePeriodPoint
{
    public string PeriodLabel { get; init; } = string.Empty;
    public decimal Revenue { get; init; }
    public double BarWidth { get; set; }
}

public sealed class MonthlyKpiPoint
{
    public string MonthLabel { get; init; } = string.Empty;
    public decimal Revenue { get; init; }
    public int Attendance { get; init; }
    public double RevenueBarWidth { get; init; }
    public double AttendanceBarWidth { get; init; }
}

public sealed class ReportSummary
{
    public decimal PaidRevenue { get; init; }
    public int PendingPayments { get; init; }
    public int ExpiringMembers { get; init; }
    public int AttendanceThisMonth { get; init; }
}
