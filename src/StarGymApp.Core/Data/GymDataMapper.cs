using StarGymApp.Core.Models;

namespace StarGymApp.Core.Data;

public static class GymDataMapper
{
    public static IReadOnlyList<MembershipPlan> ToPlans(GymDataSet data) =>
        (data.MembershipPlans ?? new()).Select(plan => new MembershipPlan
        {
            Id = plan.Id,
            Name = plan.Name,
            DurationDays = plan.DurationDays,
            Price = plan.Price,
            Description = plan.Description,
            IsActive = plan.IsActive
        }).ToList();

    public static IReadOnlyList<Member> ToMembers(GymDataSet data) =>
        (data.Members ?? new()).Select(member => new Member
        {
            Id = member.Id,
            BadgeId = string.IsNullOrWhiteSpace(member.BadgeId) || member.BadgeId.StartsWith("STAR-", StringComparison.OrdinalIgnoreCase) ? $"{10000000 + member.Id:00000000}" : member.BadgeId,
            PhotoPath = string.IsNullOrWhiteSpace(member.PhotoPath) || member.PhotoPath.Contains("STAR-", StringComparison.OrdinalIgnoreCase) ? $"Images/Members/{10000000 + member.Id:00000000}.jpg" : member.PhotoPath,
            FullName = member.FullName,
            Phone = member.Phone,
            Email = member.Email,
            Gender = member.Gender,
            DateOfBirth = member.DateOfBirth,
            JoinDate = member.JoinDate,
            MembershipPlanId = member.MembershipPlanId,
            Status = member.Status,
            TrainerId = member.TrainerId,
            Notes = member.Notes
        }).ToList();

    public static IReadOnlyList<Payment> ToPayments(GymDataSet data) =>
        (data.Payments ?? new()).Select(payment => new Payment
        {
            Id = payment.Id,
            MemberId = payment.MemberId,
            Amount = payment.Amount,
            PaymentDate = payment.PaymentDate,
            PaymentMethod = payment.PaymentMethod,
            InvoiceNumber = payment.InvoiceNumber,
            Status = payment.Status,
            Notes = payment.Notes
        }).ToList();

    public static IReadOnlyList<Attendance> ToAttendance(GymDataSet data) =>
        (data.Attendance ?? new()).Select(attendance => new Attendance
        {
            Id = attendance.Id,
            MemberId = attendance.MemberId,
            CheckInDate = attendance.CheckInDate,
            CheckInTime = attendance.CheckInTime,
            CheckOutTime = attendance.CheckOutTime,
            Status = attendance.Status
        }).ToList();
}
