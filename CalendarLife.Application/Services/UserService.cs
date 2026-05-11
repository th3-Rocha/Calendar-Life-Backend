using CalendarLife.Application.DTOs.Users;
using CalendarLife.Application.Interfaces;
using CalendarLife.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CalendarLife.Application.Services;

public sealed class UserService
{
    private readonly ICalendarLifeDbContext _db;

    public UserService(ICalendarLifeDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Gets a user by id. If it doesn't exist, it is created automatically.
    /// This matches the "no-auth" model where the frontend just sends a UUID.
    /// </summary>
    public async Task<UserResponse> GetOrCreateUserAsync(Guid userId, UpsertUserRequest? request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);

        if (user is null)
        {
            user = new User
            {
                Id = userId,
                Name = string.IsNullOrWhiteSpace(request?.Name) ? "User" : request!.Name!.Trim(),
                // if the frontend doesn't know birthdate yet, default to today (date only)
                BirthDate = request?.BirthDate?.Date ?? DateTime.UtcNow.Date
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            // Optional: treat non-null fields as updates.
            // Keeps the endpoint idempotent and easy for the frontend.
            if (!string.IsNullOrWhiteSpace(request?.Name))
                user.Name = request!.Name!.Trim();

            if (request?.BirthDate is not null)
                user.BirthDate = request.BirthDate.Value.Date;

            await _db.SaveChangesAsync(ct);
        }

        return new UserResponse(user.Id, user.Name, user.BirthDate);
    }

    public async Task<UserResponse?> GetUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, ct);

        return user is null ? null : new UserResponse(user.Id, user.Name, user.BirthDate);
    }
}
