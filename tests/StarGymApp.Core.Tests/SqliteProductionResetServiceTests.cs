using StarGymApp.Core.Models;
using StarGymApp.Core.Repositories;
using StarGymApp.Core.Services;
using Xunit;

namespace StarGymApp.Core.Tests;

public class SqliteProductionResetServiceTests
{
    [Fact]
    public async Task ClearExistingDataOnce_RemovesDemoDataButPreservesLaterRealData()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"stargym-reset-{Guid.NewGuid():N}.db");
        try
        {
            var members = new SqliteMemberRepository(databasePath);
            var payments = new SqlitePaymentRepository(databasePath);
            var attendance = new SqliteAttendanceRepository(databasePath);
            await members.InitializeAsync(new[]
            {
                new Member { Id = 1, FullName = "Demo Member", Email = "demo@example.test" }
            });
            await payments.InitializeAsync(new[]
            {
                new Payment { Id = 1, MemberId = 1, InvoiceNumber = "DEMO-1" }
            });
            await attendance.InitializeAsync(new[]
            {
                new Attendance { Id = 1, MemberId = 1, CheckInDate = DateTime.Today }
            });

            var reset = new SqliteProductionResetService();
            var result = await reset.ClearExistingDataOnceAsync(databasePath);

            Assert.True(result.WasApplied);
            Assert.Equal(1, result.MembersRemoved);
            Assert.Equal(1, result.PaymentsRemoved);
            Assert.Equal(1, result.AttendanceRemoved);
            Assert.Empty(await members.GetAllAsync());
            Assert.Empty(await payments.GetAllAsync());
            Assert.Empty(await attendance.GetAllAsync());

            await members.AddAsync(new Member { FullName = "Real Member", Email = "real@example.com" });
            var repeatedReset = await reset.ClearExistingDataOnceAsync(databasePath);

            Assert.False(repeatedReset.WasApplied);
            Assert.Equal("Real Member", Assert.Single(await members.GetAllAsync()).FullName);
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }
}
