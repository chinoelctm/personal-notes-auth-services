using MediatR;
using PersonalNotes.Auth.Domain.Exceptions;
using PersonalNotes.Auth.Domain.Repositories;

namespace PersonalNotes.Auth.Application.Queries.Users.GetCurrent
{
    public class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserResponse>
    {
        private readonly IUserRepository _userRepository;

        public GetCurrentUserHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<CurrentUserResponse> Handle(GetCurrentUserQuery query, CancellationToken ct)
        {
            var user = await _userRepository.GetByIdAsync(query.UserId, ct);

            // Token válido de un usuario que ya no existe o fue deshabilitado: se trata como sesión inválida
            if (user is null || !user.IsActive)
                throw new UnauthorizedException("La sesión no es válida.");

            return CurrentUserResponse.From(user);
        }
    }
}
