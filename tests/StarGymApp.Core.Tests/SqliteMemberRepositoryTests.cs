using StarGymApp.Core.Models;
using StarGymApp.Core.Repositories;
using Xunit;

namespace StarGymApp.Core.Tests;

public class SqliteMemberRepositoryTests
{
    [Fact]
    public async Task Repository_PersistsSeedAndChanges()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"stargym-{Guid.NewGuid():N}.db");
        try
        {
            var repository = new SqliteMemberRepository(databasePath);
            await repository.InitializeAsync(new[]
            {
                new Member { Id = 1, FullName = "Seed Member", Status = "Active" }
            });

            var seeded = await repository.GetByIdAsync(1);
            Assert.Equal("Seed Member", seeded!.FullName);

            var newMember = new Member { FullName = "Persisted Member", Status = "Paused" };
            await repository.AddAsync(newMember);
            var reopenedRepository = new SqliteMemberRepository(databasePath);
            await reopenedRepository.InitializeAsync();

            var persisted = await reopenedRepository.GetByIdAsync(newMember.Id);
            Assert.Equal("Persisted Member", persisted!.FullName);
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }
}
