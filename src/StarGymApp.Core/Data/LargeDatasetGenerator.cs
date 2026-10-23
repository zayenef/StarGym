using StarGymApp.Core.Models;

namespace StarGymApp.Core.Data;

public static class LargeDatasetGenerator
{
    public const int MaxRecords = 350;

    private static readonly string[] FirstNames = { "Adam", "Aisha", "Amal", "Bilal", "Dalia", "Daniel", "Elias", "Farah", "Hassan", "Huda", "Ibrahim", "Jana", "Karim", "Laila", "Lina", "Mariam", "Maya", "Nadia", "Nasser", "Nora", "Omar", "Rami", "Rana", "Reem", "Sami", "Sara", "Tariq", "Yara", "Yasin", "Zaid" };
    private static readonly string[] LastNames = { "Abbas", "Adel", "Ahmed", "Ali", "Amir", "Bakr", "Fahmi", "Farouk", "Haddad", "Hassan", "Ibrahim", "Jaber", "Khalil", "Mansour", "Nabil", "Nasser", "Omar", "Qasim", "Rashid", "Salem", "Sami", "Sayegh", "Yasin", "Younis" };
    public static void EnsureSize(GymDataSet data, int targetMemberCount = MaxRecords)
    {
        targetMemberCount = Math.Clamp(targetMemberCount, 0, MaxRecords);

        data.Members ??= new List<MemberSeed>();
        data.Payments ??= new List<PaymentSeed>();
        data.Attendance ??= new List<AttendanceSeed>();
        data.Members.RemoveAll(member => member.Id > targetMemberCount);
        data.Payments.RemoveAll(payment => payment.MemberId > targetMemberCount);
        data.Attendance.RemoveAll(record => record.MemberId > targetMemberCount);
        var random = new Random(20260912);
        var nextMemberId = data.Members.Count == 0 ? 1 : data.Members.Max(member => member.Id) + 1;
        for (var id = nextMemberId; id <= targetMemberCount; id++)
        {
            var joinDate = new DateTime(2024 + (id % 3), (id % 12) + 1, (id % 26) + 1);
            var planId = (id % 4) + 1;
            var status = id % 11 == 0 ? "Expired" : id % 17 == 0 ? "Paused" : id % 23 == 0 ? "Inactive" : "Active";
            data.Members.Add(new MemberSeed
            {
                Id = id,
                BadgeId = $"{10000000 + id:00000000}",
                PhotoPath = $"Images/Members/{10000000 + id:00000000}.jpg",
                FullName = $"{FirstNames[(id - 1) % FirstNames.Length]} {LastNames[((id - 1) / FirstNames.Length) % LastNames.Length]}",
                Phone = $"05{random.Next(10000000, 99999999)}",
                Email = $"member{id:00000}@stargym.test",
                Gender = id % 2 == 0 ? "Female" : "Male",
                DateOfBirth = new DateTime(1980 + (id % 23), (id % 12) + 1, (id % 26) + 1),
                JoinDate = joinDate,
                MembershipPlanId = planId,
                Status = status,
                TrainerId = (id % 4) + 1,
                Notes = id % 10 == 0 ? "Generated test record - renewal review" : "Generated test record"
            });
            {
                data.Payments.Add(new PaymentSeed
                {
                    Id = 100000 + id,
                    MemberId = id,
                    Amount = planId switch { 1 => 250, 2 => 450, 3 => 700, _ => 800 },
                    PaymentDate = joinDate.AddDays(1),
                    PaymentMethod = id % 3 == 0 ? "Card" : id % 3 == 1 ? "Cash" : "Transfer",
                    InvoiceNumber = $"GEN-{id:000000}",
                    Status = id % 13 == 0 ? "Pending" : id % 19 == 0 ? "Overdue" : "Paid",
                    Notes = "Generated test payment"
                });
                data.Attendance.Add(new AttendanceSeed
                {
                    Id = 100000 + id,
                    MemberId = id,
                    CheckInDate = joinDate.AddDays(7),
                    CheckInTime = TimeSpan.FromHours(6 + (id % 14)),
                    CheckOutTime = TimeSpan.FromHours(7 + (id % 14)),
                    Status = id % 17 == 0 ? "Late" : "Present"
                });
            }
        }

        foreach (var member in data.Members)
        {
            if (string.IsNullOrWhiteSpace(member.BadgeId) || member.BadgeId.StartsWith("STAR-", StringComparison.OrdinalIgnoreCase)) member.BadgeId = $"{10000000 + member.Id:00000000}";
            if (string.IsNullOrWhiteSpace(member.PhotoPath) || member.PhotoPath.Contains("STAR-", StringComparison.OrdinalIgnoreCase)) member.PhotoPath = $"Images/Members/{10000000 + member.Id:00000000}.jpg";
        }
    }
}
