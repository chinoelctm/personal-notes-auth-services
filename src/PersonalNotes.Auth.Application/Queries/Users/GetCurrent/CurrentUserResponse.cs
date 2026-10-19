using PersonalNotes.Auth.Domain.Entities;

namespace PersonalNotes.Auth.Application.Queries.Users.GetCurrent
{
    public record CurrentUserResponse(
        Guid Id,
        string Username,
        string Name
    )
    {
        public static CurrentUserResponse From(User user) => new(user.Id, user.Username, user.Name);
    }
}
