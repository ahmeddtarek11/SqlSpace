using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SqlSpace.Application.Abstractions.Users;
using SqlSpace.Application.Abstractions.Users.Dtos;
using SqlSpace.Domain.Common.Results;

namespace SqlSpace.Infrastructure.Identity;

public sealed class UserRepository(UserManager<ApplicationUser> userManager) : IUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<userDto?> GetByIdAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var user = await _userManager.FindByIdAsync(userId);
        return user is null ? null : MapToDto(user);
    }

    public async Task<userDto?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var user = await _userManager.FindByEmailAsync(email);
        return user is null ? null : MapToDto(user);
    }

    public async Task<IReadOnlyList<userDto>> GetByIdsAsync(
        IReadOnlyCollection<string> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds is null || userIds.Count == 0)
        {
            return Array.Empty<userDto>();
        }

        var normalizedIds = userIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (normalizedIds.Length == 0)
        {
            return Array.Empty<userDto>();
        }

        var users = await _userManager.Users
            .AsNoTracking()
            .Where(user => normalizedIds.Contains(user.Id))
            .Select(user => new userDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                LockoutEnabled = user.LockoutEnabled,
                LockoutEnd = user.LockoutEnd,
                AccessFailedCount = user.AccessFailedCount
            })
            .ToListAsync(cancellationToken);

        return users;
    }

    public async Task<Result> UpdateUserAsync(userDto user, CancellationToken cancellationToken)
    {
        if (user is null || string.IsNullOrWhiteSpace(user.Id))
        {
            return Result.Failure(new Error("UserRepository.InvalidUser", "User data is required.", nameof(user)));
        }

        var existingUser = await _userManager.FindByIdAsync(user.Id);
        if (existingUser is null)
        {
            return Result.Failure(new Error("UserRepository.UserNotFound", "User was not found.", nameof(user.Id)));
        }

        existingUser.Email = user.Email;
        existingUser.UserName = user.UserName;
        existingUser.PhoneNumber = user.PhoneNumber;
        existingUser.EmailConfirmed = user.EmailConfirmed;
        existingUser.PhoneNumberConfirmed = user.PhoneNumberConfirmed;
        existingUser.TwoFactorEnabled = user.TwoFactorEnabled;
        existingUser.LockoutEnabled = user.LockoutEnabled;
        existingUser.LockoutEnd = user.LockoutEnd;
        existingUser.AccessFailedCount = user.AccessFailedCount;

        var updateResult = await _userManager.UpdateAsync(existingUser);
        if (updateResult.Succeeded)
        {
            return Result.Success();
        }

        var errors = updateResult.Errors
            .Select(error => new Error("UserRepository.UpdateFailed", error.Description, error.Code))
            .ToList();

        return Result.Failure(errors);
    }

    public async Task<Result> RemoveUserAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result.Failure(new Error("UserRepository.InvalidUserId", "UserId is required.", nameof(userId)));
        }

        var existingUser = await _userManager.FindByIdAsync(userId);
        if (existingUser is null)
        {
            return Result.Failure(new Error("UserRepository.UserNotFound", "User was not found.", nameof(userId)));
        }

        var deleteResult = await _userManager.DeleteAsync(existingUser);
        if (deleteResult.Succeeded)
        {
            return Result.Success();
        }

        var errors = deleteResult.Errors
            .Select(error => new Error("UserRepository.RemoveFailed", error.Description, error.Code))
            .ToList();

        return Result.Failure(errors);
    }

    private static userDto MapToDto(ApplicationUser user)
    {
        return new userDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            UserName = user.UserName ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled,
            LockoutEnabled = user.LockoutEnabled,
            LockoutEnd = user.LockoutEnd,
            AccessFailedCount = user.AccessFailedCount
        };
    }
}
