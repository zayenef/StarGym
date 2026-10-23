using StarGymApp.Core.Models;

namespace StarGymApp.Core.Repositories;

public sealed class InMemoryMemberRepository : IMemberRepository
{
    private readonly List<Member> members;
    private int nextId;

    public InMemoryMemberRepository(IEnumerable<Member> initialMembers)
    {
        members = initialMembers.Select(Clone).ToList();
        nextId = members.Count == 0 ? 1 : members.Max(member => member.Id) + 1;
    }

    public Task<IEnumerable<Member>> GetAllAsync() =>
        Task.FromResult<IEnumerable<Member>>(members.Select(Clone).ToList());

    public Task<Member?> GetByIdAsync(int id) =>
        Task.FromResult(members.Where(member => member.Id == id).Select(Clone).SingleOrDefault());

    public Task AddAsync(Member member)
    {
        var copy = Clone(member);
        copy.Id = nextId++;
        members.Add(copy);
        member.Id = copy.Id;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Member member)
    {
        var index = members.FindIndex(existing => existing.Id == member.Id);
        if (index < 0)
        {
            throw new KeyNotFoundException($"Member {member.Id} was not found.");
        }

        members[index] = Clone(member);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id)
    {
        members.RemoveAll(member => member.Id == id);
        return Task.CompletedTask;
    }

    private static Member Clone(Member member) => new()
    {
        Id = member.Id,
        BadgeId = member.BadgeId,
        PhotoPath = member.PhotoPath,
        FullName = member.FullName,
        Phone = member.Phone,
        Email = member.Email,
        Gender = member.Gender,
        DateOfBirth = member.DateOfBirth,
        JoinDate = member.JoinDate,
        MembershipPlanId = member.MembershipPlanId,
        MembershipDurationMonths = member.MembershipDurationMonths,
        MembershipPrice = member.MembershipPrice,
        Status = member.Status,
        TrainerId = member.TrainerId,
        Notes = member.Notes
    };
}
