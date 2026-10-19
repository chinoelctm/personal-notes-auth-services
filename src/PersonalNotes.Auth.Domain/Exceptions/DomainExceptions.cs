namespace PersonalNotes.Auth.Domain.Exceptions
{
    public class DomainException(string message) : Exception(message);
    public class NotFoundException(string message) : DomainException(message);
    public class ConflictException(string message) : DomainException(message);
    public class UnauthorizedException(string message) : DomainException(message);
    public class LockedException(string message) : DomainException(message);
    public class InvalidRefreshTokenException(string message = "La sesión expiró. Vuelve a iniciar sesión.") : Exception(message);
}
