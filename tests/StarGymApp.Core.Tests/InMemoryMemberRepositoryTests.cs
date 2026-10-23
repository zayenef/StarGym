using StarGymApp.Core.Models;
using StarGymApp.Core.Repositories;
using Xunit;

namespace StarGymApp.Core.Tests;

public class InMemoryMemberRepositoryTests
{
    [Fact]
    public async Task Repository_AddUpdateAndDeleteMember()
    {
        var repository = new InMemoryMemberRepository(Array.Empty<Member>());
        var member = new Member { FullName = "New Member", Status = "Active" };

        await repository.AddAsync(member);
        var added = await repository.GetByIdAsync(member.Id);
        Assert.NotNull(added);
        Assert.Equal("New Member", added!.FullName);

        member.FullName = "Updated Member";
        await repository.UpdateAsync(member);
        var updated = await repository.GetByIdAsync(member.Id);
        Assert.Equal("Updated Member", updated!.FullName);

        await repository.DeleteAsync(member.Id);
        Assert.Null(await repository.GetByIdAsync(member.Id));
    }
}
