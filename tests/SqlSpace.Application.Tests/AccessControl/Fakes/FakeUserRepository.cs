using SqlSpace.Application.Abstractions.Users;
using SqlSpace.Application.Abstractions.Users.Dtos;
using SqlSpace.Domain.Common.Results;

namespace SqlSpace.Application.Tests.AccessControl.Fakes;

public sealed class FakeUserRepository : IUserRepository
{
    private readonly Dictionary<string, userDto> _usersById = new(StringComparer.Ordinal);
    private readonly Dictionary<string, userDto> _usersByEmail = new(StringComparer.OrdinalIgnoreCase);

    public void AddUser(userDto user)
    {
        ArgumentNullException.ThrowIfNull(user);
        _usersById[user.Id] = user;
        _usersByEmail[user.Email] = user;
    }

    public Task<userDto?> GetByIdAsync(string userId, CancellationToken cancellationToken)
    {
        _usersById.TryGetValue(userId, out var user);
        return Task.FromResult(user);
    }

    public Task<userDto?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        _usersByEmail.TryGetValue(email, out var user);
        return Task.FromResult(user);
    }

    public Task<IReadOnlyList<userDto>> GetByIdsAsync(
        IReadOnlyCollection<string> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds is null || userIds.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<userDto>>(Array.Empty<userDto>());
        }

        var users = userIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .Select(id => _usersById.TryGetValue(id, out var user) ? user : null)
            .Where(user => user is not null)
            .Cast<userDto>()
            .ToList();

        return Task.FromResult<IReadOnlyList<userDto>>(users);
    }

    public Task<Result> UpdateUserAsync(userDto user, CancellationToken cancellationToken)
        => Task.FromResult(Result.Success());

    public Task<Result> RemoveUserAsync(string userId, CancellationToken cancellationToken)
        => Task.FromResult(Result.Success());
}
