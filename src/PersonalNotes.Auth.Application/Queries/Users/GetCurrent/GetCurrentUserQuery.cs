using MediatR;

namespace PersonalNotes.Auth.Application.Queries.Users.GetCurrent
{
    public record GetCurrentUserQuery(Guid UserId) : IRequest<CurrentUserResponse>;
}
