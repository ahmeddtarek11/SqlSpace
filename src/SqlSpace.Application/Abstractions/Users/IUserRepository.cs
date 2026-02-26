using SqlSpace.Application.Abstractions.Users.Dtos;
using SqlSpace.Domain.Common.Results;

namespace SqlSpace.Application.Abstractions.Users;

public interface IUserRepository
{
    Task<userDto?> GetByIdAsync(string userId, CancellationToken cancellationToken);
    Task<userDto?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<IReadOnlyList<userDto>> GetByIdsAsync(
        IReadOnlyCollection<string> userIds,
        CancellationToken cancellationToken);
    Task<Result> UpdateUserAsync(userDto user, CancellationToken cancellationToken);
    Task<Result> RemoveUserAsync(string userId, CancellationToken cancellationToken);
}
