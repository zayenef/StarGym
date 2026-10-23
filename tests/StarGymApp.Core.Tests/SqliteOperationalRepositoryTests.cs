using StarGymApp.Core.Models;
using StarGymApp.Core.Repositories;
using Xunit;

namespace StarGymApp.Core.Tests;

public class SqliteOperationalRepositoryTests
{
    [Fact]
    public async Task PaymentAndAttendanceRepositories_PersistSeedRecords()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"stargym-ops-{Guid.NewGuid():N}.db");
        try
        {
            var paymentRepository = new SqlitePaymentRepository(databasePath);
            var attendanceRepository = new SqliteAttendanceRepository(databasePath);
            await paymentRepository.InitializeAsync(new[]
            {
                new Payment { Id = 1, MemberId = 3, Amount = 700, Status = "Pending" }
            });
            await attendanceRepository.InitializeAsync(new[]
            {
                new Attendance { Id = 1, MemberId = 3, CheckInDate = DateTime.Today, CheckInTime = TimeSpan.FromHours(8) }
            });

            var payments = await new SqlitePaymentRepository(databasePath).GetAllAsync();
            var attendance = await new SqliteAttendanceRepository(databasePath).GetAllAsync();

            Assert.Single(payments);
            Assert.Equal(700, payments.Single().Amount);
            Assert.Single(attendance);
            Assert.Equal(3, attendance.Single().MemberId);
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task PruneGeneratedTestRecords_RemovesRowsAboveLimitAndKeepsCustomRows()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"stargym-prune-{Guid.NewGuid():N}.db");
        try
        {
            var memberRepository = new SqliteMemberRepository(databasePath);
            var paymentRepository = new SqlitePaymentRepository(databasePath);
            var attendanceRepository = new SqliteAttendanceRepository(databasePath);
            await memberRepository.InitializeAsync(new[]
            {
                new Member { Id = 351, FullName = "Generated Member", Email = "member00351@stargym.test" },
                new Member { Id = 400, FullName = "Custom Member", Email = "owner@example.com" }
            });
            await paymentRepository.InitializeAsync(new[]
            {
                new Payment { Id = 100351, MemberId = 351, InvoiceNumber = "GEN-000351" },
                new Payment { Id = 900000, MemberId = 400, InvoiceNumber = "INV-CUSTOM" }
            });
            await attendanceRepository.InitializeAsync(new[]
            {
                new Attendance { Id = 100351, MemberId = 351, CheckInDate = DateTime.Today },
                new Attendance { Id = 900000, MemberId = 400, CheckInDate = DateTime.Today }
            });

            await memberRepository.RemoveGeneratedTestMembersAfterAsync(350);
            await paymentRepository.RemoveGeneratedTestPaymentsAfterAsync(350);
            await attendanceRepository.RemoveGeneratedTestAttendanceAfterAsync(350);

            Assert.Equal("Custom Member", (await memberRepository.GetByIdAsync(400))!.FullName);
            Assert.Null(await memberRepository.GetByIdAsync(351));
            Assert.Single(await paymentRepository.GetAllAsync(), payment => payment.Id == 900000);
            Assert.DoesNotContain(await paymentRepository.GetAllAsync(), payment => payment.Id == 100351);
            Assert.Single(await attendanceRepository.GetAllAsync(), record => record.Id == 900000);
            Assert.DoesNotContain(await attendanceRepository.GetAllAsync(), record => record.Id == 100351);
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task MembershipLifecycle_SavesRenewalsAndRemovesClientHistory()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"stargym-membership-{Guid.NewGuid():N}.db");
        try
        {
            var members = new SqliteMemberRepository(databasePath);
            var payments = new SqlitePaymentRepository(databasePath);
            var attendance = new SqliteAttendanceRepository(databasePath);
            await members.InitializeAsync();
            await payments.InitializeAsync();
            await attendance.InitializeAsync();

            var photoPath = Path.Combine(Path.GetTempPath(), "StarGym", "member-photo.jpg");
            var member = new Member
            {
                FullName = "Renewal Client",
                PhotoPath = photoPath,
                JoinDate = DateTime.Today,
                MembershipPlanId = 1,
                MembershipDurationMonths = 6,
                MembershipPrice = 1234.50m,
                Status = "Active"
            };
            var firstPayment = new Payment
            {
                Amount = 250,
                PaymentDate = DateTime.Today,
                PaymentMethod = "Cash",
                InvoiceNumber = "INV-NEW",
                Status = "Paid"
            };

            await members.SaveMembershipAsync(member, firstPayment);

            Assert.True(member.Id > 0);
            Assert.Equal(member.Id, firstPayment.MemberId);
            Assert.True(firstPayment.Id > 0);
            var savedMember = await members.GetByIdAsync(member.Id);
            Assert.Equal(member.BadgeId, savedMember!.BadgeId);
            Assert.Equal(photoPath, member.PhotoPath);
            Assert.Equal(photoPath, savedMember.PhotoPath);
            Assert.Equal(6, savedMember.MembershipDurationMonths);
            Assert.Equal(1234.50m, savedMember.MembershipPrice);

            member.JoinDate = DateTime.Today.AddDays(30);
            member.MembershipPlanId = 2;
            member.MembershipDurationMonths = 12;
            member.MembershipPrice = 2400m;
            var renewalPayment = new Payment
            {
                Amount = 450,
                PaymentDate = DateTime.Today,
                PaymentMethod = "Card",
                InvoiceNumber = "INV-RENEW",
                Status = "Paid"
            };
            await members.SaveMembershipAsync(member, renewalPayment);
            await attendance.AddAsync(new Attendance { MemberId = member.Id, CheckInDate = DateTime.Today });

            Assert.Equal(2, (await payments.GetAllAsync()).Count());
            var renewedMember = await members.GetByIdAsync(member.Id);
            Assert.Equal(2, renewedMember!.MembershipPlanId);
            Assert.Equal(12, renewedMember.MembershipDurationMonths);
            Assert.Equal(2400m, renewedMember.MembershipPrice);

            await members.DeleteWithHistoryAsync(member.Id);
            await members.InitializeAsync(new[] { member });

            Assert.Null(await members.GetByIdAsync(member.Id));
            Assert.Empty(await payments.GetAllAsync());
            Assert.Empty(await attendance.GetAllAsync());
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }
}
